using InnPay.Application.Common;
using InnPay.Application.DTOs.Request;
using InnPay.Application.DTOs.Response;
using InnPay.Application.Interfaces;
using InnPay.Domain.Entities;
using InnPay.Domain.Enums;
using InnPay.Domain.Interfaces;

namespace InnPay.Application.Services;

public class KycService : IKycService
{
    private readonly IUnitOfWork _uow;
    private readonly IFileStorageService _fileStorage;

    public KycService(IUnitOfWork uow, IFileStorageService fileStorage)
    {
        _uow = uow;
        _fileStorage = fileStorage;
    }

    // ─── STATUS ──────────────────────────────────────────────────────────────

    public async Task<ServiceResult<KycStatusResponse>> GetKycStatusAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        var account = await _uow.Accounts.GetByIdAsync(accountId);
        if (account is null)
            return ServiceResult<KycStatusResponse>.Fail("Account not found.", 404);

        var documents = await _uow.KycDocuments.GetByAccountIdAsync(accountId);

        return ServiceResult<KycStatusResponse>.Success(new KycStatusResponse
        {
            AccountId = accountId,
            CurrentTier = account.KycTier,
            OverallStatus = account.KycStatus,
            Documents = documents.Select(MapDocumentResponse).ToList(),
            NextStepMessage = BuildNextStepMessage(account)
        });
    }

    // ─── SINGLE DOCUMENT UPLOAD ───────────────────────────────────────────────

    public async Task<ServiceResult<KycDocumentResponse>> UploadDocumentAsync(UploadKycDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var account = await _uow.Accounts.GetByIdAsync(request.AccountId);
        if (account is null)
            return ServiceResult<KycDocumentResponse>.Fail("Account not found.", 404);

        var fileUrl = await _fileStorage.UploadAsync(
            request.FileBase64,
            request.OriginalFileName,
            request.ContentType,
            $"kyc/{account.AccountType.ToString().ToLower()}/{account.Id}");

        var doc = new KycDocument
        {
            AccountId = request.AccountId,
            DocumentType = request.DocumentType,
            FileUrl = fileUrl,
            OriginalFileName = request.OriginalFileName,
            Status = KycStatus.Pending
        };

        await _uow.KycDocuments.AddAsync(doc);
        await _uow.SaveChangesAsync(cancellationToken);

        return ServiceResult<KycDocumentResponse>.Success(MapDocumentResponse(doc), 201);
    }

    // ─── PERSONAL KYC TIER 1 ─────────────────────────────────────────────────

    public async Task<ServiceResult<KycStatusResponse>> SubmitPersonalKycTier1Async(UploadPersonalKycTier1Request request, CancellationToken cancellationToken = default)
    {
        var account = await _uow.Accounts.GetByIdAsync(request.AccountId);
        if (account is null)
            return ServiceResult<KycStatusResponse>.Fail("Account not found.", 404);

        if (account.AccountType != AccountType.Personal)
            return ServiceResult<KycStatusResponse>.Fail("This KYC tier is for Personal accounts only.");

        if (account.KycTier >= KycTier.Tier1)
            return ServiceResult<KycStatusResponse>.Fail("Tier 1 KYC has already been submitted.");

        // Validate document type is an acceptable ID
        var validIdTypes = new[] { DocumentType.NationalId, DocumentType.InternationalPassport, DocumentType.DriversLicense };
        if (!validIdTypes.Contains(request.IdDocumentType))
            return ServiceResult<KycStatusResponse>.Fail("Invalid ID document type for Tier 1 KYC.");

        var idFileUrl = await _fileStorage.UploadAsync(
            request.IdFileBase64,
            request.IdOriginalFileName,
            request.IdContentType,
            $"kyc/personal/{request.AccountId}/tier1");

        var idDoc = new KycDocument
        {
            AccountId = request.AccountId,
            DocumentType = request.IdDocumentType,
            FileUrl = idFileUrl,
            OriginalFileName = request.IdOriginalFileName,
            Status = KycStatus.Pending
        };

        await _uow.KycDocuments.AddAsync(idDoc);

        // Update account KYC status — Tier 1 is optional (doesn't block activation)
        account.KycTier = KycTier.Tier1;
        account.KycStatus = KycStatus.UnderReview;
        account.UpdatedAt = DateTime.UtcNow;

        // Upgrade NGN wallet daily limit upon Tier 1 submission (activated when approved)
        await _uow.Accounts.UpdateAsync(account);
        await _uow.SaveChangesAsync(cancellationToken);

        return await GetKycStatusAsync(request.AccountId);
    }

    // ─── PERSONAL KYC TIER 2 ─────────────────────────────────────────────────

    public async Task<ServiceResult<KycStatusResponse>> SubmitPersonalKycTier2Async(UploadPersonalKycTier2Request request, CancellationToken cancellationToken = default)
    {
        var account = await _uow.Accounts.GetByIdAsync(request.AccountId);
        if (account is null)
            return ServiceResult<KycStatusResponse>.Fail("Account not found.", 404);

        if (account.AccountType != AccountType.Personal)
            return ServiceResult<KycStatusResponse>.Fail("This KYC tier is for Personal accounts only.");

        if (account.KycTier < KycTier.Tier1)
            return ServiceResult<KycStatusResponse>.Fail("Tier 1 KYC must be approved before submitting Tier 2.");

        if (account.KycTier >= KycTier.Tier2)
            return ServiceResult<KycStatusResponse>.Fail("Tier 2 KYC has already been submitted.");

        // BVN verification would call an external service (e.g. Mono, Okra, or NIBSS).
        // Here we record that BVN was submitted; the external call happens in infrastructure.
        var bvnVerified = await VerifyBvnExternalAsync(request.Bvn, account);
        if (!bvnVerified)
            return ServiceResult<KycStatusResponse>.Fail("BVN verification failed. Please check the number and retry.", 422);

        var folder = $"kyc/personal/{request.AccountId}/tier2";

        var selfieUrl = await _fileStorage.UploadAsync(request.SelfieFileBase64, request.SelfieFileName, request.SelfieContentType, folder);
        var poaUrl    = await _fileStorage.UploadAsync(request.ProofOfAddressFileBase64, request.ProofOfAddressFileName, request.ProofOfAddressContentType, folder);

        await _uow.KycDocuments.AddAsync(new KycDocument
        {
            AccountId = request.AccountId,
            DocumentType = DocumentType.Selfie,
            FileUrl = selfieUrl,
            OriginalFileName = request.SelfieFileName,
            Status = KycStatus.Pending
        });

        await _uow.KycDocuments.AddAsync(new KycDocument
        {
            AccountId = request.AccountId,
            DocumentType = DocumentType.ProofOfAddress,
            FileUrl = poaUrl,
            OriginalFileName = request.ProofOfAddressFileName,
            Status = KycStatus.Pending
        });

        account.KycTier = KycTier.Tier2;
        account.KycStatus = KycStatus.UnderReview;
        account.UpdatedAt = DateTime.UtcNow;

        await _uow.Accounts.UpdateAsync(account);
        await _uow.SaveChangesAsync(cancellationToken);

        return await GetKycStatusAsync(request.AccountId);
    }

    // ─── BUSINESS KYC ────────────────────────────────────────────────────────

    public async Task<ServiceResult<KycStatusResponse>> SubmitBusinessKycAsync(UploadBusinessKycRequest request, CancellationToken cancellationToken = default)
    {
        var account = await _uow.Accounts.GetByIdAsync(request.AccountId);
        if (account is null)
            return ServiceResult<KycStatusResponse>.Fail("Account not found.", 404);

        if (account.AccountType != AccountType.Business)
            return ServiceResult<KycStatusResponse>.Fail("This KYC flow is for Business accounts only.");

        if (account.KycStatus == KycStatus.UnderReview || account.KycStatus == KycStatus.Approved)
            return ServiceResult<KycStatusResponse>.Fail("KYC documents have already been submitted.");

        var folder = $"kyc/business/{request.AccountId}";

        var validDirectorIdTypes = new[] { DocumentType.NationalId, DocumentType.InternationalPassport, DocumentType.DriversLicense };
        if (!validDirectorIdTypes.Contains(request.DirectorIdType))
            return ServiceResult<KycStatusResponse>.Fail("Invalid director ID document type.");

        // Upload all three required documents
        var cacUrl        = await _fileStorage.UploadAsync(request.CacCertificateBase64, request.CacCertificateFileName, request.CacCertificateContentType, folder);
        var utilityUrl    = await _fileStorage.UploadAsync(request.UtilityBillBase64, request.UtilityBillFileName, request.UtilityBillContentType, folder);
        var directorIdUrl = await _fileStorage.UploadAsync(request.DirectorIdBase64, request.DirectorIdFileName, request.DirectorIdContentType, folder);

        await _uow.KycDocuments.AddAsync(new KycDocument { AccountId = request.AccountId, DocumentType = DocumentType.CacCertificate,   FileUrl = cacUrl,        OriginalFileName = request.CacCertificateFileName, Status = KycStatus.Pending });
        await _uow.KycDocuments.AddAsync(new KycDocument { AccountId = request.AccountId, DocumentType = DocumentType.BusinessUtilityBill, FileUrl = utilityUrl,    OriginalFileName = request.UtilityBillFileName,    Status = KycStatus.Pending });
        await _uow.KycDocuments.AddAsync(new KycDocument { AccountId = request.AccountId, DocumentType = request.DirectorIdType,           FileUrl = directorIdUrl, OriginalFileName = request.DirectorIdFileName,      Status = KycStatus.Pending });

        account.KycStatus = KycStatus.UnderReview;
        account.UpdatedAt = DateTime.UtcNow;

        await _uow.Accounts.UpdateAsync(account);
        await _uow.SaveChangesAsync(cancellationToken);

        return await GetKycStatusAsync(request.AccountId);
    }

    // ─── CORPORATE KYC ───────────────────────────────────────────────────────

    public async Task<ServiceResult<KycStatusResponse>> SubmitCorporateKycAsync(UploadCorporateKycRequest request, CancellationToken cancellationToken = default)
    {
        var account = await _uow.Accounts.GetByIdAsync(request.AccountId);
        if (account is null)
            return ServiceResult<KycStatusResponse>.Fail("Account not found.", 404);

        if (account.AccountType != AccountType.Corporate)
            return ServiceResult<KycStatusResponse>.Fail("This KYC flow is for Corporate accounts only.");

        if (account.KycStatus == KycStatus.UnderReview || account.KycStatus == KycStatus.Approved)
            return ServiceResult<KycStatusResponse>.Fail("KYC documents have already been submitted.");

        if (!request.UboDocuments.Any())
            return ServiceResult<KycStatusResponse>.Fail("At least one UBO (Ultimate Beneficial Owner) document is required.");

        var folder = $"kyc/corporate/{request.AccountId}";

        // Upload the four core corporate documents
        var certUrl        = await _fileStorage.UploadAsync(request.CertOfIncorporationBase64, request.CertOfIncorporationFileName, request.CertOfIncorporationContentType, folder);
        var memoUrl        = await _fileStorage.UploadAsync(request.MemorandumBase64,          request.MemorandumFileName,          request.MemorandumContentType,          folder);
        var boardUrl       = await _fileStorage.UploadAsync(request.BoardResolutionBase64,     request.BoardResolutionFileName,     request.BoardResolutionContentType,     folder);
        var beneficialUrl  = await _fileStorage.UploadAsync(request.BeneficialOwnershipBase64, request.BeneficialOwnershipFileName, request.BeneficialOwnershipContentType, folder);

        await _uow.KycDocuments.AddAsync(new KycDocument { AccountId = request.AccountId, DocumentType = DocumentType.CertificateOfIncorporation,     FileUrl = certUrl,       OriginalFileName = request.CertOfIncorporationFileName, Status = KycStatus.Pending });
        await _uow.KycDocuments.AddAsync(new KycDocument { AccountId = request.AccountId, DocumentType = DocumentType.MemorandumAndArticles,           FileUrl = memoUrl,       OriginalFileName = request.MemorandumFileName,          Status = KycStatus.Pending });
        await _uow.KycDocuments.AddAsync(new KycDocument { AccountId = request.AccountId, DocumentType = DocumentType.BoardResolution,                 FileUrl = boardUrl,      OriginalFileName = request.BoardResolutionFileName,     Status = KycStatus.Pending });
        await _uow.KycDocuments.AddAsync(new KycDocument { AccountId = request.AccountId, DocumentType = DocumentType.BeneficialOwnershipDeclaration, FileUrl = beneficialUrl, OriginalFileName = request.BeneficialOwnershipFileName, Status = KycStatus.Pending });

        // Upload each UBO's ID document
        foreach (var (ubo, index) in request.UboDocuments.Select((u, i) => (u, i)))
        {
            var uboUrl = await _fileStorage.UploadAsync(ubo.IdFileBase64, ubo.IdFileName, ubo.IdContentType, $"{folder}/ubo_{index}");
            await _uow.KycDocuments.AddAsync(new KycDocument
            {
                AccountId = request.AccountId,
                DocumentType = DocumentType.UboId,
                FileUrl = uboUrl,
                OriginalFileName = ubo.IdFileName,
                Status = KycStatus.Pending
            });
        }

        account.KycStatus = KycStatus.UnderReview;
        account.UpdatedAt = DateTime.UtcNow;

        await _uow.Accounts.UpdateAsync(account);
        await _uow.SaveChangesAsync(cancellationToken);

        return await GetKycStatusAsync(request.AccountId);
    }

    // ─── ADMIN: REVIEW DOCUMENT ───────────────────────────────────────────────

    public async Task<ServiceResult<KycDocumentResponse>> ReviewDocumentAsync(ReviewKycDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var doc = await _uow.KycDocuments.GetByIdAsync(request.DocumentId);
        if (doc is null)
            return ServiceResult<KycDocumentResponse>.Fail("Document not found.", 404);

        doc.Status = request.IsApproved ? KycStatus.Approved : KycStatus.Rejected;
        doc.RejectionReason = request.IsApproved ? null : request.RejectionReason;
        doc.ReviewedAt = DateTime.UtcNow;
        doc.ReviewedByAdminId = request.ReviewedByAdminId;
        doc.UpdatedAt = DateTime.UtcNow;

        await _uow.KycDocuments.UpdateAsync(doc);

        // Check if all documents for this account are approved → upgrade KYC tier
        await TryUpgradeKycTierAsync(doc.AccountId);

        await _uow.SaveChangesAsync(cancellationToken);

        return ServiceResult<KycDocumentResponse>.Success(MapDocumentResponse(doc));
    }

    // ─── PRIVATE HELPERS ─────────────────────────────────────────────────────

    /// <summary>
    /// After each document review, checks whether all submitted docs are approved
    /// and upgrades the account's KYC tier and wallet limits accordingly.
    /// </summary>
    private async Task TryUpgradeKycTierAsync(Guid accountId)
    {
        var account = await _uow.Accounts.GetByIdAsync(accountId);
        if (account is null) return;

        var docs = (await _uow.KycDocuments.GetByAccountIdAsync(accountId)).ToList();

        // If any document is still pending or rejected, don't upgrade
        if (docs.Any(d => d.Status == KycStatus.Pending || d.Status == KycStatus.Rejected))
        {
            if (docs.Any(d => d.Status == KycStatus.Rejected))
                account.KycStatus = KycStatus.Rejected;

            await _uow.Accounts.UpdateAsync(account);
            return;
        }

        // All documents approved — upgrade based on account type
        account.KycStatus = KycStatus.Approved;

        switch (account.AccountType)
        {
            case AccountType.Personal:
                var tier1Types = new[] { DocumentType.NationalId, DocumentType.InternationalPassport, DocumentType.DriversLicense };
                var hasTier1 = docs.Any(d => tier1Types.Contains(d.DocumentType) && d.Status == KycStatus.Approved);
                var hasTier2 = docs.Any(d => d.DocumentType == DocumentType.Selfie && d.Status == KycStatus.Approved)
                            && docs.Any(d => d.DocumentType == DocumentType.ProofOfAddress && d.Status == KycStatus.Approved);

                if (hasTier2)
                {
                    account.KycTier = KycTier.Tier2;
                    await UpgradePersonalWalletsAsync(accountId, KycTier.Tier2);
                }
                else if (hasTier1)
                {
                    account.KycTier = KycTier.Tier1;
                    await UpgradePersonalWalletsAsync(accountId, KycTier.Tier1);
                }
                break;

            case AccountType.Business:
                account.KycTier = KycTier.BusinessVerified;
                account.Status = AccountStatus.Active;
                await ProvisionBusinessWalletsAsync(accountId);
                break;

            case AccountType.Corporate:
                account.KycTier = KycTier.CorporateVerified;
                account.Status = AccountStatus.Active;
                account.ComplianceReviewComplete = true;
                await ProvisionCorporateWalletsAsync(accountId);
                break;
        }

        account.UpdatedAt = DateTime.UtcNow;
        await _uow.Accounts.UpdateAsync(account);
    }

    private async Task UpgradePersonalWalletsAsync(Guid accountId, KycTier tier)
    {
        var ngnWallet = await _uow.Wallets.GetByAccountAndCurrencyAsync(accountId, WalletCurrency.NGN);
        if (ngnWallet is not null)
        {
            ngnWallet.DailyTransactionLimit = tier == KycTier.Tier2 ? 5_000_000m : 50_000m;
            ngnWallet.UpdatedAt = DateTime.UtcNow;
            await _uow.Wallets.UpdateAsync(ngnWallet);
        }

        if (tier == KycTier.Tier2)
        {
            // Activate foreign currency wallets
            foreach (var currency in new[] { WalletCurrency.USD, WalletCurrency.EUR, WalletCurrency.GBP })
            {
                var existing = await _uow.Wallets.GetByAccountAndCurrencyAsync(accountId, currency);
                if (existing is null)
                {
                    await _uow.Wallets.AddAsync(new Domain.Entities.Wallet
                    {
                        AccountId = accountId,
                        Currency = currency,
                        IsActive = true,
                        DailyTransactionLimit = 0m   // FX limits set separately
                    });
                }
                else
                {
                    existing.IsActive = true;
                    existing.UpdatedAt = DateTime.UtcNow;
                    await _uow.Wallets.UpdateAsync(existing);
                }
            }
        }
    }

    private async Task ProvisionBusinessWalletsAsync(Guid accountId)
    {
        // Activate NGN wallet with full business limit
        var ngnWallet = await _uow.Wallets.GetByAccountAndCurrencyAsync(accountId, WalletCurrency.NGN);
        if (ngnWallet is not null)
        {
            ngnWallet.DailyTransactionLimit = 10_000_000m;
            ngnWallet.IsActive = true;
            ngnWallet.UpdatedAt = DateTime.UtcNow;
            await _uow.Wallets.UpdateAsync(ngnWallet);
        }

        foreach (var currency in new[] { WalletCurrency.USD, WalletCurrency.EUR, WalletCurrency.GBP })
        {
            var existing = await _uow.Wallets.GetByAccountAndCurrencyAsync(accountId, currency);
            if (existing is null)
            {
                await _uow.Wallets.AddAsync(new Domain.Entities.Wallet
                {
                    AccountId = accountId,
                    Currency = currency,
                    IsActive = true,
                    DailyTransactionLimit = 0m
                });
            }
        }
    }

    private async Task ProvisionCorporateWalletsAsync(Guid accountId)
    {
        foreach (var currency in new[] { WalletCurrency.NGN, WalletCurrency.USD, WalletCurrency.EUR, WalletCurrency.GBP })
        {
            var existing = await _uow.Wallets.GetByAccountAndCurrencyAsync(accountId, currency);
            if (existing is null)
            {
                await _uow.Wallets.AddAsync(new Domain.Entities.Wallet
                {
                    AccountId = accountId,
                    Currency = currency,
                    IsActive = true,
                    DailyTransactionLimit = 0m   // custom limits negotiated with compliance
                });
            }
            else
            {
                existing.IsActive = true;
                existing.UpdatedAt = DateTime.UtcNow;
                await _uow.Wallets.UpdateAsync(existing);
            }
        }
    }

    /// <summary>Stub for external BVN verification service (e.g. Mono, Okra, NIBSS).</summary>
    private Task<bool> VerifyBvnExternalAsync(string bvn, Domain.Entities.Account account)
    {
        // TODO: Wire up external BVN provider in Infrastructure layer
        // For now, basic format validation only
        return Task.FromResult(bvn.Length == 11 && bvn.All(char.IsDigit));
    }

    private static KycDocumentResponse MapDocumentResponse(KycDocument doc) => new()
    {
        DocumentId = doc.Id,
        DocumentType = doc.DocumentType,
        Status = doc.Status,
        RejectionReason = doc.RejectionReason,
        UploadedAt = doc.CreatedAt,
        ReviewedAt = doc.ReviewedAt
    };

    private static string BuildNextStepMessage(Domain.Entities.Account account) =>
        (account.AccountType, account.KycTier, account.KycStatus) switch
        {
            (AccountType.Personal, KycTier.None, _)
                => "Upload a government-issued ID to complete Tier 1 KYC and increase your limits.",
            (AccountType.Personal, KycTier.Tier1, KycStatus.UnderReview)
                => "Your Tier 1 documents are under review. Submit BVN and selfie to apply for Tier 2.",
            (AccountType.Personal, KycTier.Tier1, KycStatus.Approved)
                => "Tier 1 approved. Submit BVN, selfie, and proof of address to unlock Tier 2 limits.",
            (AccountType.Personal, KycTier.Tier2, KycStatus.UnderReview)
                => "Your Tier 2 documents are under review.",
            (AccountType.Personal, KycTier.Tier2, KycStatus.Approved)
                => "Tier 2 KYC approved. All features unlocked.",
            (AccountType.Business, _, KycStatus.NotSubmitted)
                => "Submit your CAC certificate, business utility bill, and director's ID to activate your account.",
            (AccountType.Business, _, KycStatus.UnderReview)
                => "Your business KYC documents are under review.",
            (AccountType.Business, _, KycStatus.Approved)
                => "Business account fully verified.",
            (AccountType.Corporate, _, KycStatus.NotSubmitted)
                => "Upload all required corporate documents. Compliance review takes up to 3 business days.",
            (AccountType.Corporate, _, KycStatus.UnderReview)
                => "Your corporate documents are under compliance review (up to 3 business days).",
            (AccountType.Corporate, _, KycStatus.Approved)
                => "Corporate account approved and activated.",
            _ => "Contact support for further assistance."
        };
}
