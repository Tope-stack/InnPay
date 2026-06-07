using InnPay.Application.Common;
using InnPay.Application.DTOs.Request;
using InnPay.Application.DTOs.Response;
using InnPay.Domain.Enums;

namespace InnPay.Application.Interfaces;

// ── 6.1 Universal Payment Gateway ────────────────────────────────────────────

public interface IPaymentGatewayService
{
    Task<ServiceResult<PaymentInitiatedResponse>> InitiateAsync(InitiatePaymentRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<PaymentVerifiedResponse>> VerifyAsync(VerifyPaymentRequest request, CancellationToken cancellationToken = default);
}

// ── 6.2 Utility Bills ────────────────────────────────────────────────────────

public interface IBillsService
{
    Task<ServiceResult<IEnumerable<BillerResponse>>> GetBillersAsync(BillCategory? category, CancellationToken cancellationToken = default);
    Task<ServiceResult<BillCustomerValidationResponse>> ValidateCustomerAsync(ValidateBillCustomerRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<BillPaymentResponse>> PayBillAsync(PayBillRequest request, CancellationToken cancellationToken = default);
}

// ── 6.3 Internal Transfer ─────────────────────────────────────────────────────

public interface IInternalTransferService
{
    Task<ServiceResult<RecipientLookupResponse>> LookupRecipientAsync(string identifier, CancellationToken cancellationToken = default);
    Task<ServiceResult<InternalTransferResponse>> InitiateAsync(InternalTransferRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<TransferScheduleResponse>> ScheduleAsync(ScheduleTransferRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> CancelScheduleAsync(CancelScheduleRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<IEnumerable<InternalTransferResponse>>> GetHistoryAsync(Guid accountId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
}

// ── 6.4 External Transfer ─────────────────────────────────────────────────────

public interface IExternalTransferService
{
    Task<ServiceResult<IEnumerable<BankResponse>>> GetBankListAsync(WalletCurrency currency, CancellationToken cancellationToken = default);
    Task<ServiceResult<BankAccountValidationResponse>> ValidateBankAccountAsync(ValidateBankAccountRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<ExternalTransferResponse>> InitiateAsync(ExternalTransferRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<IEnumerable<ExternalTransferResponse>>> GetHistoryAsync(Guid accountId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
}

// ── 6.5 Virtual Accounts ─────────────────────────────────────────────────────

public interface IVirtualAccountService
{
    Task<ServiceResult<VirtualAccountResponse>> GenerateAsync(GenerateVirtualAccountRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<IEnumerable<VirtualAccountResponse>>> GetByAccountAsync(Guid accountId, CancellationToken cancellationToken = default);
}

// ── 6.6 Withdrawal ────────────────────────────────────────────────────────────

public interface IWithdrawalService
{
    Task<ServiceResult<WithdrawalResponse>> InitiateAsync(InitiateWithdrawalRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<IEnumerable<WithdrawalResponse>>> GetHistoryAsync(Guid accountId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
}

// ── 6.7 Virtual Card ──────────────────────────────────────────────────────────

public interface IVirtualCardService
{
    Task<ServiceResult<VirtualCardResponse>> CreateAsync(CreateVirtualCardRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<VirtualCardDetailsResponse>> RevealDetailsAsync(RevealCardDetailsRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<VirtualCardResponse>> FreezeAsync(CardActionRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<VirtualCardResponse>> UnfreezeAsync(CardActionRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteAsync(CardActionRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<VirtualCardResponse>> SetSpendingLimitAsync(SetSpendingLimitRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<IEnumerable<CardTransactionResponse>>> GetTransactionHistoryAsync(Guid cardId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
    Task<ServiceResult> HandleWebhookAsync(string providerCardId, CardTransactionWebhookPayload payload, CancellationToken cancellationToken = default);
}

// ── 6.8 Gift Cards ────────────────────────────────────────────────────────────

public interface IGiftCardService
{
    Task<ServiceResult<IEnumerable<GiftCardCatalogueItemResponse>>> GetCatalogueAsync(string? countryCode, CancellationToken cancellationToken = default);
    Task<ServiceResult<GiftCardPurchaseResponse>> PurchaseAsync(PurchaseGiftCardRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<GiftCardRevealResponse>> RevealAsync(RevealGiftCardRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<IEnumerable<GiftCardPurchaseResponse>>> GetPurchasesAsync(Guid accountId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
}

// ── 6.9 Flights ───────────────────────────────────────────────────────────────

public interface IFlightService
{
    Task<ServiceResult<IEnumerable<FlightOfferResponse>>> SearchAsync(FlightSearchRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<FlightBookingResponse>> BookAsync(BookFlightRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> SendTicketAsync(SendTicketRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<IEnumerable<FlightBookingResponse>>> GetBookingsAsync(Guid accountId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
}

// ── 6.10 Bet Funding ──────────────────────────────────────────────────────────

public interface IBetFundingService
{
    Task<ServiceResult<IEnumerable<BettingPlatformResponse>>> GetPlatformsAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult<BetFundingResponse>> FundAsync(FundBettingAccountRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult<IEnumerable<BetFundingResponse>>> GetHistoryAsync(Guid accountId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default);
}

// ── Webhook payload (card issuer → InnPay) ────────────────────────────────────

public class CardTransactionWebhookPayload
{
    public string MerchantName { get; set; } = string.Empty;
    public string MerchantCategory { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string ProviderTransactionId { get; set; } = string.Empty;
    public bool IsDeclined { get; set; }
    public string? DeclineReason { get; set; }
    public DateTime TransactedAt { get; set; }
}
