using InnPay.Domain.Enums;

namespace InnPay.Application.Interfaces;

// ── Payment Providers (6.1) ───────────────────────────────────────────────────

public interface IPaystackProvider
{
    /// <summary>Creates a payment intent and returns a checkout reference / authorization URL. Amount in kobo (NGN smallest unit).</summary>
    Task<ProviderPaymentResult> InitiateAsync(decimal amountKobo, string email, string reference, string callbackUrl);

    /// <summary>Verifies a transaction with Paystack and returns its current status.</summary>
    Task<ProviderVerifyResult> VerifyAsync(string reference);
}

public interface IFlutterwaveProvider
{
    Task<ProviderPaymentResult> InitiateAsync(decimal amount, WalletCurrency currency, string email, string reference, string redirectUrl);
    Task<ProviderVerifyResult> VerifyAsync(string transactionId);
}

public interface IStripeProvider
{
    /// <summary>Creates a Stripe PaymentIntent and returns the client secret for the Flutter SDK.</summary>
    Task<ProviderPaymentResult> InitiateAsync(decimal amountCents, string currencyCode, string idempotencyKey);
    Task<ProviderVerifyResult> VerifyAsync(string paymentIntentId);
}

// ── Bills Provider (6.2) ─────────────────────────────────────────────────────

public interface IBillsProvider
{
    Task<IEnumerable<ProviderBillerResult>> GetBillersAsync(BillCategory? category);
    Task<ProviderCustomerValidationResult> ValidateCustomerAsync(string billerCode, string customerReference);
    Task<ProviderBillPaymentResult> PayBillAsync(string billerCode, string customerReference, decimal amount, string reference);
}

// ── Virtual Account Provider (6.5) ───────────────────────────────────────────

public interface IVirtualAccountProvider
{
    Task<ProviderVirtualAccountResult> GenerateAsync(string accountName, string reference);
}

// ── Card Issuer Provider (6.7) ───────────────────────────────────────────────

public interface ICardIssuerProvider
{
    Task<ProviderCardResult> CreateCardAsync(string accountName, string reference);
    Task<bool> FreezeCardAsync(string providerCardId);
    Task<bool> UnfreezeCardAsync(string providerCardId);
    Task<bool> DeleteCardAsync(string providerCardId);
    Task<bool> SetSpendingLimitAsync(string providerCardId, decimal perTransaction, decimal monthly);
}

// ── Gift Card Provider (6.8) ─────────────────────────────────────────────────

public interface IGiftCardProvider
{
    Task<IEnumerable<ProviderGiftCardCatalogueItem>> GetCatalogueAsync(string? countryCode);
    Task<ProviderGiftCardOrderResult> PurchaseAsync(string brandName, string countryCode, decimal denominationUsd, string reference);
}

// ── Flight Provider (6.9) ─────────────────────────────────────────────────────

public interface IFlightProvider
{
    Task<IEnumerable<ProviderFlightOffer>> SearchFlightsAsync(string origin, string destination, DateTime departureDate, DateTime? returnDate, int passengers, string cabinClass, string tripType);
    Task<ProviderFlightBookingResult> BookAsync(string offerId, IEnumerable<ProviderPassengerDetail> passengers, string reference);
    Task<string?> GetETicketUrlAsync(string providerOrderId);
}

// ── Betting Provider (6.10) ───────────────────────────────────────────────────

public interface IBettingProvider
{
    Task<ProviderBettingFundResult> FundAsync(BettingPlatform platform, string bettingUserId, decimal amount, string reference);
}

// ── Provider Result Models ─────────────────────────────────────────────────────

public record ProviderPaymentResult(bool IsSuccess, string? Reference, string? ClientSecret, string? CheckoutUrl, string? Error);
public record ProviderVerifyResult(bool IsSuccess, string Status, string? ProviderReference, decimal? Amount, string? Error);
public record ProviderBillerResult(string BillerCode, string BillerName, BillCategory Category, string? LogoUrl, decimal MinAmount, decimal MaxAmount);
public record ProviderCustomerValidationResult(bool IsValid, string? CustomerName, decimal? OutstandingBalance, string? Error);
public record ProviderBillPaymentResult(bool IsSuccess, string? BillerReference, string? ElectricityToken, string? Error);
public record ProviderVirtualAccountResult(bool IsSuccess, string? AccountNumber, string? BankName, string? BankCode, string? AccountName, string? Error);
public record ProviderCardResult(bool IsSuccess, string? ProviderCardId, string? PanEncrypted, string? Last4, string? Expiry, string? CvvEncrypted, string? Error);
public record ProviderGiftCardCatalogueItem(string BrandName, string CountryCode, string? LogoUrl, IEnumerable<decimal> Denominations);
public record ProviderGiftCardOrderResult(bool IsSuccess, string? OrderId, string? RedemptionCodeEncrypted, string? RedemptionPin, string? Error);
public record ProviderFlightOffer(string OfferId, string Airline, string Origin, string Destination, DateTime DepartureAt, DateTime? ArrivalAt, int Stops, string CabinClass, decimal PriceNgn, decimal PriceUsd, string Duration);
public record ProviderPassengerDetail(string FirstName, string LastName, DateTime DateOfBirth, string PassportNumber, string Nationality);
public record ProviderFlightBookingResult(bool IsSuccess, string? ProviderOrderId, string? BookingReference, string? ETicketUrl, string? Error);
public record ProviderBettingFundResult(bool IsSuccess, string? ProviderReference, string? Error);
