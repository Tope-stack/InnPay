using InnPay.Application.Common;
using InnPay.Application.DTOs.Request;
using InnPay.Application.DTOs.Response;
using InnPay.Application.Interfaces;
using InnPay.Domain.Entities;
using InnPay.Domain.Enums;
using InnPay.Domain.Interfaces;

namespace InnPay.Application.Services;

public class PaymentGatewayService : IPaymentGatewayService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly IPaystackProvider _paystack;
    private readonly IFlutterwaveProvider _flutterwave;
    private readonly IStripeProvider _stripe;

    public PaymentGatewayService(
        IUnitOfWork uow,
        IPasswordHasher hasher,
        IPaystackProvider paystack,
        IFlutterwaveProvider flutterwave,
        IStripeProvider stripe)
    {
        _uow = uow;
        _hasher = hasher;
        _paystack = paystack;
        _flutterwave = flutterwave;
        _stripe = stripe;
    }

    public async Task<ServiceResult<PaymentInitiatedResponse>> InitiateAsync(
        InitiatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        // Idempotency check
        var existing = await _uow.Payments.GetByIdempotencyKeyAsync(request.IdempotencyKey);
        if (existing is not null)
            return ServiceResult<PaymentInitiatedResponse>.Success(MapToInitiated(existing));

        var account = await _uow.Accounts.GetByIdAsync(request.AccountId);
        if (account is null)
            return ServiceResult<PaymentInitiatedResponse>.Fail("Account not found.", 404);

        var wallet = await _uow.Wallets.GetByIdAsync(request.WalletId);
        if (wallet is null || wallet.AccountId != request.AccountId)
            return ServiceResult<PaymentInitiatedResponse>.Fail("Wallet not found or does not belong to this account.", 404);

        if (!wallet.IsActive || wallet.Status != WalletStatus.Active)
            return ServiceResult<PaymentInitiatedResponse>.Fail("Wallet is not active.");

        var provider = ResolveProvider(request.Currency);
        var reference = GenerateReference("PAY");

        var payment = new Payment
        {
            AccountId = request.AccountId,
            WalletId = request.WalletId,
            Amount = request.Amount,
            Currency = request.Currency,
            Provider = provider,
            Method = request.Method,
            Status = PaymentStatus.Pending,
            IdempotencyKey = request.IdempotencyKey,
            Reference = reference
        };

        ProviderPaymentResult providerResult;

        try
        {
            providerResult = provider switch
            {
                PaymentProvider.Paystack =>
                    await _paystack.InitiateAsync(request.Amount * 100, account.User?.Email ?? "", reference, ""),
                PaymentProvider.Flutterwave =>
                    await _flutterwave.InitiateAsync(request.Amount, request.Currency, account.User?.Email ?? "", reference, ""),
                PaymentProvider.Stripe =>
                    await _stripe.InitiateAsync(request.Amount * 100, request.Currency.ToString().ToLower(), request.IdempotencyKey),
                _ => new ProviderPaymentResult(false, null, null, null, "Unsupported provider.")
            };
        }
        catch (Exception ex)
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = ex.Message;
            await _uow.Payments.AddAsync(payment);
            await _uow.SaveChangesAsync(cancellationToken);
            return ServiceResult<PaymentInitiatedResponse>.Fail("Payment provider unavailable. Please try again.", 503);
        }

        if (!providerResult.IsSuccess)
        {
            payment.Status = PaymentStatus.Failed;
            payment.FailureReason = providerResult.Error;
            await _uow.Payments.AddAsync(payment);
            await _uow.SaveChangesAsync(cancellationToken);
            return ServiceResult<PaymentInitiatedResponse>.Fail(providerResult.Error ?? "Payment initiation failed.");
        }

        payment.Status = PaymentStatus.Processing;
        payment.ProviderReference = providerResult.Reference;
        payment.ClientSecret = providerResult.ClientSecret ?? providerResult.CheckoutUrl;

        await _uow.Payments.AddAsync(payment);
        await _uow.SaveChangesAsync(cancellationToken);

        return ServiceResult<PaymentInitiatedResponse>.Success(MapToInitiated(payment, providerResult.CheckoutUrl), 201);
    }

    public async Task<ServiceResult<PaymentVerifiedResponse>> VerifyAsync(
        VerifyPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var payment = await _uow.Payments.GetByReferenceAsync(request.Reference);
        if (payment is null)
            return ServiceResult<PaymentVerifiedResponse>.Fail("Payment not found.", 404);

        ProviderVerifyResult verifyResult;

        try
        {
            verifyResult = request.Provider switch
            {
                PaymentProvider.Paystack => await _paystack.VerifyAsync(request.Reference),
                PaymentProvider.Flutterwave => await _flutterwave.VerifyAsync(payment.ProviderReference ?? request.Reference),
                PaymentProvider.Stripe => await _stripe.VerifyAsync(payment.ProviderReference ?? request.Reference),
                _ => new ProviderVerifyResult(false, "failed", null, null, "Unsupported provider.")
            };
        }
        catch (Exception ex)
        {
            return ServiceResult<PaymentVerifiedResponse>.Fail($"Verification failed: {ex.Message}", 503);
        }

        // Update payment status based on provider response
        payment.Status = verifyResult.Status.ToLower() switch
        {
            "success" or "successful" or "completed" => PaymentStatus.Completed,
            "failed" or "failure" => PaymentStatus.Failed,
            "reversed" => PaymentStatus.Reversed,
            _ => PaymentStatus.Processing
        };

        if (payment.Status == PaymentStatus.Completed)
        {
            payment.CompletedAt = DateTime.UtcNow;
            payment.ProviderReference = verifyResult.ProviderReference ?? payment.ProviderReference;

            // Credit wallet on successful payment
            var wallet = await _uow.Wallets.GetByIdAsync(payment.WalletId);
            if (wallet is not null)
            {
                wallet.Balance += payment.Amount;
                wallet.AvailableBalance += payment.Amount;
                await _uow.Wallets.UpdateAsync(wallet);
            }
        }

        await _uow.Payments.UpdateAsync(payment);
        await _uow.SaveChangesAsync(cancellationToken);

        return ServiceResult<PaymentVerifiedResponse>.Success(new PaymentVerifiedResponse
        {
            PaymentId = payment.Id,
            Reference = payment.Reference,
            ProviderReference = payment.ProviderReference,
            Status = payment.Status,
            Amount = payment.Amount,
            Currency = payment.Currency,
            CompletedAt = payment.CompletedAt
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static PaymentProvider ResolveProvider(WalletCurrency currency) => currency switch
    {
        WalletCurrency.NGN => PaymentProvider.Paystack,
        WalletCurrency.USD or WalletCurrency.EUR or WalletCurrency.GBP => PaymentProvider.Stripe,
        _ => PaymentProvider.Flutterwave
    };

    private static string GenerateReference(string prefix)
        => $"INN-{prefix}-{Guid.NewGuid().ToString("N")[..12].ToUpper()}";

    private static PaymentInitiatedResponse MapToInitiated(Payment p, string? checkoutUrl = null) => new()
    {
        PaymentId = p.Id,
        Reference = p.Reference,
        Status = p.Status,
        Provider = p.Provider,
        ClientSecret = p.ClientSecret,
        CheckoutUrl = checkoutUrl,
        Amount = p.Amount,
        Currency = p.Currency,
        CreatedAt = p.CreatedAt
    };
}
