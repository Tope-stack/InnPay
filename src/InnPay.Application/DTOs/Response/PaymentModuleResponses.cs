using InnPay.Domain.Enums;

namespace InnPay.Application.DTOs.Response;

// ── 6.1 Payment Gateway ────────────────────────────────────────────────────────

public class PaymentInitiatedResponse
{
    public Guid PaymentId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public PaymentProvider Provider { get; set; }

    /// <summary>Returned to Flutter for SDK handoff (Paystack client secret, Stripe client secret, etc.).</summary>
    public string? ClientSecret { get; set; }

    /// <summary>Checkout URL for redirect-based flows (Flutterwave, etc.).</summary>
    public string? CheckoutUrl { get; set; }

    public decimal Amount { get; set; }
    public WalletCurrency Currency { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PaymentVerifiedResponse
{
    public Guid PaymentId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string? ProviderReference { get; set; }
    public PaymentStatus Status { get; set; }
    public decimal Amount { get; set; }
    public WalletCurrency Currency { get; set; }
    public DateTime? CompletedAt { get; set; }
}

// ── 6.3 Internal Transfer ─────────────────────────────────────────────────────

public class RecipientLookupResponse
{
    public Guid AccountId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public AccountType AccountType { get; set; }
}

public class InternalTransferResponse
{
    public Guid TransferId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid SenderAccountId { get; set; }
    public Guid ReceiverAccountId { get; set; }
    public string ReceiverName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public WalletCurrency Currency { get; set; }
    public TransferStatus Status { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TransferScheduleResponse
{
    public Guid ScheduleId { get; set; }
    public Guid ReceiverAccountId { get; set; }
    public string ReceiverName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public WalletCurrency Currency { get; set; }
    public TransferFrequency Frequency { get; set; }
    public DateTime NextRunAt { get; set; }
    public bool IsActive { get; set; }
}

// ── 6.4 External Transfer ─────────────────────────────────────────────────────

public class BankResponse
{
    public string BankCode { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public bool SupportsInstant { get; set; }
    public WalletCurrency Currency { get; set; }
}

public class BankAccountValidationResponse
{
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
}

public class ExternalTransferResponse
{
    public Guid TransferId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string DestinationBankName { get; set; } = string.Empty;
    public string DestinationAccountNumber { get; set; } = string.Empty;
    public string DestinationAccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public WalletCurrency Currency { get; set; }
    public ExternalTransferStatus Status { get; set; }
    public string? NibssSessionId { get; set; }
    public string? SwiftReference { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ── 6.2 Bills ─────────────────────────────────────────────────────────────────

public class BillerResponse
{
    public string BillerCode { get; set; } = string.Empty;
    public string BillerName { get; set; } = string.Empty;
    public BillCategory Category { get; set; }
    public string? LogoUrl { get; set; }
    public decimal MinAmount { get; set; }
    public decimal MaxAmount { get; set; }
}

public class BillCustomerValidationResponse
{
    public string CustomerReference { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string BillerName { get; set; } = string.Empty;
    public decimal? OutstandingBalance { get; set; }
}

public class BillPaymentResponse
{
    public Guid PaymentId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string BillerName { get; set; } = string.Empty;
    public string CustomerReference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public string? ElectricityToken { get; set; }
    public string? BillerReference { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ── 6.5 Virtual Accounts ──────────────────────────────────────────────────────

public class VirtualAccountResponse
{
    public Guid VirtualAccountId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string? Label { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ── 6.6 Withdrawal ────────────────────────────────────────────────────────────

public class WithdrawalResponse
{
    public Guid WithdrawalId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string DestinationBankName { get; set; } = string.Empty;
    public string DestinationAccountNumber { get; set; } = string.Empty;
    public string DestinationAccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public WalletCurrency Currency { get; set; }
    public WithdrawalStatus Status { get; set; }
    public string EstimatedDelivery { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// ── 6.7 Virtual Card ──────────────────────────────────────────────────────────

public class VirtualCardResponse
{
    public Guid CardId { get; set; }
    public string Last4 { get; set; } = string.Empty;
    public string Expiry { get; set; } = string.Empty;
    public VirtualCardStatus Status { get; set; }
    public decimal SpendingLimitPerTransaction { get; set; }
    public decimal SpendingLimitMonthly { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class VirtualCardDetailsResponse : VirtualCardResponse
{
    /// <summary>Full PAN — only returned when user explicitly requests with PIN.</summary>
    public string Pan { get; set; } = string.Empty;
    public string Cvv { get; set; } = string.Empty;
}

public class CardTransactionResponse
{
    public Guid TransactionId { get; set; }
    public string MerchantName { get; set; } = string.Empty;
    public string MerchantCategory { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public WalletCurrency Currency { get; set; }
    public bool IsDeclined { get; set; }
    public string? DeclineReason { get; set; }
    public DateTime TransactedAt { get; set; }
}

// ── 6.8 Gift Cards ────────────────────────────────────────────────────────────

public class GiftCardCatalogueItemResponse
{
    public string BrandName { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public List<decimal> AvailableDenominationsUsd { get; set; } = new();
}

public class GiftCardPurchaseResponse
{
    public Guid PurchaseId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string BrandName { get; set; } = string.Empty;
    public decimal DenominationUsd { get; set; }
    public decimal AmountCharged { get; set; }
    public WalletCurrency WalletCurrency { get; set; }
    public GiftCardStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GiftCardRevealResponse : GiftCardPurchaseResponse
{
    public string RedemptionCode { get; set; } = string.Empty;
    public string? RedemptionPin { get; set; }
}

// ── 6.9 Flights ───────────────────────────────────────────────────────────────

public class FlightOfferResponse
{
    public string OfferId { get; set; } = string.Empty;
    public string Airline { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateTime DepartureAt { get; set; }
    public DateTime? ArrivalAt { get; set; }
    public int Stops { get; set; }
    public string CabinClass { get; set; } = string.Empty;
    public decimal PriceNgn { get; set; }
    public decimal PriceUsd { get; set; }
    public string Duration { get; set; } = string.Empty;
}

public class FlightBookingResponse
{
    public Guid BookingId { get; set; }
    public string BookingReference { get; set; } = string.Empty;
    public string Airline { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateTime DepartureAt { get; set; }
    public int PassengerCount { get; set; }
    public decimal AmountCharged { get; set; }
    public WalletCurrency Currency { get; set; }
    public BookingStatus Status { get; set; }
    public string? ETicketUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ── 6.10 Bet Funding ──────────────────────────────────────────────────────────

public class BettingPlatformResponse
{
    public BettingPlatform Platform { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public decimal MinAmount { get; set; }
    public decimal MaxAmount { get; set; }
}

public class BetFundingResponse
{
    public Guid FundingId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public BettingPlatform Platform { get; set; }
    public string BettingUserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public string? ProviderReference { get; set; }
    public DateTime CreatedAt { get; set; }
}
