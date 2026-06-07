using InnPay.Application.Common;
using InnPay.Application.DTOs.Request;
using InnPay.Application.DTOs.Response;
using InnPay.Application.Interfaces;
using InnPay.Domain.Entities;
using InnPay.Domain.Enums;
using InnPay.Domain.Interfaces;

namespace InnPay.Application.Services;

public class BillsService : IBillsService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly IBillsProvider _billsProvider;

    public BillsService(IUnitOfWork uow, IPasswordHasher hasher, IBillsProvider billsProvider)
    {
        _uow = uow;
        _hasher = hasher;
        _billsProvider = billsProvider;
    }

    public async Task<ServiceResult<IEnumerable<BillerResponse>>> GetBillersAsync(
        BillCategory? category, CancellationToken cancellationToken = default)
    {
        var billers = await _billsProvider.GetBillersAsync(category);
        return ServiceResult<IEnumerable<BillerResponse>>.Success(
            billers.Select(b => new BillerResponse
            {
                BillerCode = b.BillerCode,
                BillerName = b.BillerName,
                Category = b.Category,
                LogoUrl = b.LogoUrl,
                MinAmount = b.MinAmount,
                MaxAmount = b.MaxAmount
            }));
    }

    public async Task<ServiceResult<BillCustomerValidationResponse>> ValidateCustomerAsync(
        ValidateBillCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _billsProvider.ValidateCustomerAsync(request.BillerCode, request.CustomerReference);
        if (!result.IsValid)
            return ServiceResult<BillCustomerValidationResponse>.Fail(result.Error ?? "Customer validation failed.", 422);

        return ServiceResult<BillCustomerValidationResponse>.Success(new BillCustomerValidationResponse
        {
            CustomerReference = request.CustomerReference,
            CustomerName = result.CustomerName,
            BillerName = request.BillerCode,
            OutstandingBalance = result.OutstandingBalance
        });
    }

    public async Task<ServiceResult<BillPaymentResponse>> PayBillAsync(
        PayBillRequest request, CancellationToken cancellationToken = default)
    {
        var account = await _uow.Accounts.GetByIdAsync(request.AccountId);
        if (account is null)
            return ServiceResult<BillPaymentResponse>.Fail("Account not found.", 404);

        if (!_hasher.Verify(request.TransactionPin, account.User?.TransactionPinHash ?? ""))
            return ServiceResult<BillPaymentResponse>.Fail("Invalid transaction PIN.", 401);

        var wallet = await _uow.Wallets.GetByIdAsync(request.WalletId);
        if (wallet is null || wallet.AccountId != request.AccountId)
            return ServiceResult<BillPaymentResponse>.Fail("Wallet not found.", 404);

        if (!wallet.IsActive)
            return ServiceResult<BillPaymentResponse>.Fail("Wallet is not active.");

        if (wallet.AvailableBalance < request.Amount)
            return ServiceResult<BillPaymentResponse>.Fail("Insufficient balance.");

        // Validate customer first
        var validation = await _billsProvider.ValidateCustomerAsync(request.BillerCode, request.CustomerReference);
        if (!validation.IsValid)
            return ServiceResult<BillPaymentResponse>.Fail("Customer validation failed. Please check the reference number.", 422);

        var reference = $"INN-BILL-{Guid.NewGuid().ToString("N")[..10].ToUpper()}";

        var billPayment = new BillPayment
        {
            AccountId = request.AccountId,
            WalletId = request.WalletId,
            Category = ResolveBillerCategory(request.BillerCode),
            BillerCode = request.BillerCode,
            BillerName = request.BillerCode,   // provider returns actual name
            CustomerReference = request.CustomerReference,
            CustomerName = validation.CustomerName,
            Amount = request.Amount,
            Status = PaymentStatus.Processing,
            Reference = reference
        };

        // Debit wallet
        wallet.Balance -= request.Amount;
        wallet.AvailableBalance -= request.Amount;

        await _uow.BillPayments.AddAsync(billPayment);
        await _uow.Wallets.UpdateAsync(wallet);

        // Call provider
        var payResult = await _billsProvider.PayBillAsync(
            request.BillerCode, request.CustomerReference, request.Amount, reference);

        if (payResult.IsSuccess)
        {
            billPayment.Status = PaymentStatus.Completed;
            billPayment.BillerReference = payResult.BillerReference;
            billPayment.ElectricityToken = payResult.ElectricityToken;
        }
        else
        {
            // Refund on failure
            billPayment.Status = PaymentStatus.Failed;
            billPayment.FailureReason = payResult.Error;
            wallet.Balance += request.Amount;
            wallet.AvailableBalance += request.Amount;
            await _uow.Wallets.UpdateAsync(wallet);
        }

        await _uow.BillPayments.UpdateAsync(billPayment);
        await _uow.SaveChangesAsync(cancellationToken);

        if (!payResult.IsSuccess)
            return ServiceResult<BillPaymentResponse>.Fail(payResult.Error ?? "Bill payment failed.", 422);

        return ServiceResult<BillPaymentResponse>.Success(new BillPaymentResponse
        {
            PaymentId = billPayment.Id,
            Reference = billPayment.Reference,
            BillerName = billPayment.BillerName,
            CustomerReference = billPayment.CustomerReference,
            Amount = billPayment.Amount,
            Status = billPayment.Status,
            ElectricityToken = billPayment.ElectricityToken,
            BillerReference = billPayment.BillerReference,
            CreatedAt = billPayment.CreatedAt
        }, 201);
    }

    private static BillCategory ResolveBillerCategory(string billerCode)
    {
        // Heuristic prefix resolution — replaced by provider metadata in production
        if (billerCode.StartsWith("ELEC")) return BillCategory.Electricity;
        if (billerCode.StartsWith("AIR")) return BillCategory.Airtime;
        if (billerCode.StartsWith("DATA")) return BillCategory.Data;
        if (billerCode.StartsWith("CAB")) return BillCategory.CableTV;
        if (billerCode.StartsWith("WAT")) return BillCategory.Water;
        return BillCategory.Internet;
    }
}
