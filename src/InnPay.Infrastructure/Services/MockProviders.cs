using InnPay.Application.Interfaces;
using InnPay.Domain.Enums;

namespace InnPay.Infrastructure.Services;

// ─────────────────────────────────────────────────────────────────────────────
// MOCK PROVIDERS — return realistic shaped responses for development & testing.
// Replace each with a real SDK implementation by swapping the registration in
// ServiceExtensions.cs when API credentials are available.
// ─────────────────────────────────────────────────────────────────────────────

// ── 6.1 Payment Providers ────────────────────────────────────────────────────

/// <summary>
/// TODO: Replace with real Paystack SDK (PayStack.net or HttpClient to api.paystack.co)
/// </summary>
public class MockPaystackProvider : IPaystackProvider
{
    public Task<ProviderPaymentResult> InitiateAsync(decimal amountKobo, string email, string reference, string callbackUrl)
    {
        Console.WriteLine($"[MockPaystack] InitiatePayment | ref={reference} | amount={amountKobo}kobo | email={email}");
        return Task.FromResult(new ProviderPaymentResult(
            IsSuccess: true,
            Reference: reference,
            ClientSecret: null,
            CheckoutUrl: $"https://checkout.paystack.com/mock/{reference}",
            Error: null));
    }

    public Task<ProviderVerifyResult> VerifyAsync(string reference)
    {
        Console.WriteLine($"[MockPaystack] VerifyPayment | ref={reference}");
        return Task.FromResult(new ProviderVerifyResult(
            IsSuccess: true,
            Status: "success",
            ProviderReference: $"PSK_{reference}",
            Amount: null,
            Error: null));
    }
}

/// <summary>
/// TODO: Replace with real Flutterwave SDK (FlutterwaveStandard or HttpClient to api.flutterwave.com)
/// </summary>
public class MockFlutterwaveProvider : IFlutterwaveProvider
{
    public Task<ProviderPaymentResult> InitiateAsync(decimal amount, WalletCurrency currency, string email, string reference, string redirectUrl)
    {
        Console.WriteLine($"[MockFlutterwave] Initiate | ref={reference} | amount={amount} {currency}");
        return Task.FromResult(new ProviderPaymentResult(
            IsSuccess: true,
            Reference: reference,
            ClientSecret: null,
            CheckoutUrl: $"https://checkout.flutterwave.com/v3/hosted/pay/mock/{reference}",
            Error: null));
    }

    public Task<ProviderVerifyResult> VerifyAsync(string transactionId)
    {
        Console.WriteLine($"[MockFlutterwave] Verify | txId={transactionId}");
        return Task.FromResult(new ProviderVerifyResult(
            IsSuccess: true,
            Status: "successful",
            ProviderReference: $"FLW_{transactionId}",
            Amount: null,
            Error: null));
    }
}

/// <summary>
/// TODO: Replace with real Stripe SDK (Stripe.net NuGet package)
/// </summary>
public class MockStripeProvider : IStripeProvider
{
    public Task<ProviderPaymentResult> InitiateAsync(decimal amountCents, string currencyCode, string idempotencyKey)
    {
        Console.WriteLine($"[MockStripe] CreatePaymentIntent | amount={amountCents}cents | currency={currencyCode}");
        var piId = $"pi_mock_{Guid.NewGuid().ToString("N")[..16]}";
        return Task.FromResult(new ProviderPaymentResult(
            IsSuccess: true,
            Reference: piId,
            ClientSecret: $"{piId}_secret_mock",
            CheckoutUrl: null,
            Error: null));
    }

    public Task<ProviderVerifyResult> VerifyAsync(string paymentIntentId)
    {
        Console.WriteLine($"[MockStripe] RetrievePaymentIntent | id={paymentIntentId}");
        return Task.FromResult(new ProviderVerifyResult(
            IsSuccess: true,
            Status: "succeeded",
            ProviderReference: paymentIntentId,
            Amount: null,
            Error: null));
    }
}

// ── 6.2 Bills Provider ───────────────────────────────────────────────────────

/// <summary>
/// TODO: Replace with real VTPass or Interswitch Quickteller SDK / HttpClient
/// </summary>
public class MockBillsProvider : IBillsProvider
{
    public Task<IEnumerable<ProviderBillerResult>> GetBillersAsync(BillCategory? category)
    {
        var billers = new List<ProviderBillerResult>
        {
            new("ELEC-EKEDC", "Eko Electricity (EKEDC)", BillCategory.Electricity, null, 500m, 500_000m),
            new("ELEC-IKEDC", "Ikeja Electricity (IKEDC)", BillCategory.Electricity, null, 500m, 500_000m),
            new("AIR-MTN",    "MTN Airtime",               BillCategory.Airtime,     null, 50m,  50_000m),
            new("AIR-AIRTEL", "Airtel Airtime",             BillCategory.Airtime,     null, 50m,  50_000m),
            new("DATA-MTN",   "MTN Data",                   BillCategory.Data,        null, 100m, 50_000m),
            new("CAB-DSTV",   "DStv",                       BillCategory.CableTV,     null, 1000m,50_000m),
            new("CAB-GOTV",   "GoTV",                       BillCategory.CableTV,     null, 1000m,20_000m),
            new("INT-SMILE",  "Smile Internet",             BillCategory.Internet,    null, 1000m,50_000m),
        };

        var filtered = category.HasValue
            ? billers.Where(b => b.Category == category.Value)
            : billers;

        return Task.FromResult<IEnumerable<ProviderBillerResult>>(filtered.ToList());
    }

    public Task<ProviderCustomerValidationResult> ValidateCustomerAsync(string billerCode, string customerReference)
    {
        Console.WriteLine($"[MockBills] ValidateCustomer | biller={billerCode} | ref={customerReference}");
        return Task.FromResult(new ProviderCustomerValidationResult(
            IsValid: true,
            CustomerName: "John Doe",
            OutstandingBalance: 5000m,
            Error: null));
    }

    public Task<ProviderBillPaymentResult> PayBillAsync(string billerCode, string customerReference, decimal amount, string reference)
    {
        Console.WriteLine($"[MockBills] PayBill | biller={billerCode} | ref={reference} | amount={amount}");
        var token = billerCode.StartsWith("ELEC") ? $"4532-{Random.Shared.Next(1000, 9999)}-{Random.Shared.Next(1000, 9999)}-0001" : null;
        return Task.FromResult(new ProviderBillPaymentResult(
            IsSuccess: true,
            BillerReference: $"VTPASS_{reference}",
            ElectricityToken: token,
            Error: null));
    }
}

// ── 6.5 Virtual Account Provider ─────────────────────────────────────────────

/// <summary>
/// TODO: Replace with real Providus Bank or Wema Bank API HttpClient
/// </summary>
public class MockVirtualAccountProvider : IVirtualAccountProvider
{
    public Task<ProviderVirtualAccountResult> GenerateAsync(string accountName, string reference)
    {
        Console.WriteLine($"[MockVirtualAccount] Generate | name={accountName} | ref={reference}");
        var accountNumber = $"9{Random.Shared.Next(100_000_000, 999_999_999)}";
        return Task.FromResult(new ProviderVirtualAccountResult(
            IsSuccess: true,
            AccountNumber: accountNumber,
            BankName: "Providus Bank",
            BankCode: "101",
            AccountName: accountName,
            Error: null));
    }
}

// ── 6.7 Card Issuer Provider ──────────────────────────────────────────────────

/// <summary>
/// TODO: Replace with real Sudo Africa or Union54 SDK / HttpClient
/// Card details are mock-encrypted — use IEncryptionService.Encrypt in production
/// </summary>
public class MockCardIssuerProvider : ICardIssuerProvider
{
    public Task<ProviderCardResult> CreateCardAsync(string accountName, string reference)
    {
        Console.WriteLine($"[MockCardIssuer] CreateCard | name={accountName}");
        var pan = $"4{Random.Shared.NextInt64(100_000_000_000_000L, 999_999_999_999_999L)}";
        var last4 = pan[^4..];
        var expiry = $"{DateTime.UtcNow.AddYears(3).Month:D2}/{DateTime.UtcNow.AddYears(3):yy}";
        var cvv = Random.Shared.Next(100, 999).ToString();
        return Task.FromResult(new ProviderCardResult(
            IsSuccess: true,
            ProviderCardId: $"SUDO_{Guid.NewGuid().ToString("N")[..16]}",
            PanEncrypted: pan,          // TODO: IEncryptionService.Encrypt(pan)
            Last4: last4,
            Expiry: expiry,
            CvvEncrypted: cvv,          // TODO: IEncryptionService.Encrypt(cvv)
            Error: null));
    }

    public Task<bool> FreezeCardAsync(string providerCardId)
    {
        Console.WriteLine($"[MockCardIssuer] FreezeCard | id={providerCardId}");
        return Task.FromResult(true);
    }

    public Task<bool> UnfreezeCardAsync(string providerCardId)
    {
        Console.WriteLine($"[MockCardIssuer] UnfreezeCard | id={providerCardId}");
        return Task.FromResult(true);
    }

    public Task<bool> DeleteCardAsync(string providerCardId)
    {
        Console.WriteLine($"[MockCardIssuer] DeleteCard | id={providerCardId}");
        return Task.FromResult(true);
    }

    public Task<bool> SetSpendingLimitAsync(string providerCardId, decimal perTransaction, decimal monthly)
    {
        Console.WriteLine($"[MockCardIssuer] SetLimit | id={providerCardId} | per-txn={perTransaction} | monthly={monthly}");
        return Task.FromResult(true);
    }
}

// ── 6.8 Gift Card Provider ───────────────────────────────────────────────────

/// <summary>
/// TODO: Replace with real Reloadly API HttpClient (api.reloadly.com)
/// </summary>
public class MockGiftCardProvider : IGiftCardProvider
{
    public Task<IEnumerable<ProviderGiftCardCatalogueItem>> GetCatalogueAsync(string? countryCode)
    {
        var items = new List<ProviderGiftCardCatalogueItem>
        {
            new("Amazon", "US", null, new[] { 10m, 25m, 50m, 100m }),
            new("Apple",  "US", null, new[] { 15m, 25m, 50m }),
            new("Netflix","US", null, new[] { 15m, 25m, 30m }),
            new("Steam",  "US", null, new[] { 5m, 10m, 20m, 50m }),
            new("Google Play", "US", null, new[] { 5m, 10m, 25m, 50m })
        };

        var result = countryCode is null
            ? items
            : items.Where(i => i.CountryCode.Equals(countryCode, StringComparison.OrdinalIgnoreCase)).ToList();

        return Task.FromResult<IEnumerable<ProviderGiftCardCatalogueItem>>(result);
    }

    public Task<ProviderGiftCardOrderResult> PurchaseAsync(string brandName, string countryCode, decimal denominationUsd, string reference)
    {
        Console.WriteLine($"[MockGiftCard] Purchase | brand={brandName} | denom=${denominationUsd} | ref={reference}");
        var code = $"MOCK-{brandName.ToUpper()[..Math.Min(4, brandName.Length)]}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        return Task.FromResult(new ProviderGiftCardOrderResult(
            IsSuccess: true,
            OrderId: $"RELOADLY_{reference}",
            RedemptionCodeEncrypted: code,   // TODO: IEncryptionService.Encrypt(code)
            RedemptionPin: Random.Shared.Next(1000, 9999).ToString(),
            Error: null));
    }
}

// ── 6.9 Flight Provider ───────────────────────────────────────────────────────

/// <summary>
/// TODO: Replace with real Amadeus Travel API (api.amadeus.com)
/// </summary>
public class MockFlightProvider : IFlightProvider
{
    public Task<IEnumerable<ProviderFlightOffer>> SearchFlightsAsync(
        string origin, string destination, DateTime departureDate, DateTime? returnDate,
        int passengers, string cabinClass, string tripType)
    {
        Console.WriteLine($"[MockFlight] Search | {origin}→{destination} | {departureDate:yyyy-MM-dd}");
        var offers = new List<ProviderFlightOffer>
        {
            new($"OFFER_{Guid.NewGuid():N}", "Air Peace", origin, destination,
                departureDate.AddHours(8), departureDate.AddHours(11), 0, cabinClass,
                150_000m, 95m, "3h 00m"),
            new($"OFFER_{Guid.NewGuid():N}", "Ethiopian Airlines", origin, destination,
                departureDate.AddHours(14), departureDate.AddHours(19), 1, cabinClass,
                120_000m, 75m, "5h 00m")
        };
        return Task.FromResult<IEnumerable<ProviderFlightOffer>>(offers);
    }

    public Task<ProviderFlightBookingResult> BookAsync(string offerId, IEnumerable<ProviderPassengerDetail> passengers, string reference)
    {
        Console.WriteLine($"[MockFlight] Book | offerId={offerId} | ref={reference}");
        return Task.FromResult(new ProviderFlightBookingResult(
            IsSuccess: true,
            ProviderOrderId: $"AMD_{Guid.NewGuid().ToString("N")[..12]}",
            BookingReference: $"INN{Random.Shared.Next(100000, 999999)}",
            ETicketUrl: $"https://tickets.innpay.com/mock/{reference}.pdf",
            Error: null));
    }

    public Task<string?> GetETicketUrlAsync(string providerOrderId)
        => Task.FromResult<string?>($"https://tickets.innpay.com/mock/{providerOrderId}.pdf");
}

// ── 6.10 Betting Provider ─────────────────────────────────────────────────────

/// <summary>
/// TODO: Replace with real Bet9ja / Sportybet / 1xBet API clients when credentials available
/// </summary>
public class MockBettingProvider : IBettingProvider
{
    public Task<ProviderBettingFundResult> FundAsync(BettingPlatform platform, string bettingUserId, decimal amount, string reference)
    {
        Console.WriteLine($"[MockBetting] Fund | platform={platform} | userId={bettingUserId} | amount={amount} | ref={reference}");
        return Task.FromResult(new ProviderBettingFundResult(
            IsSuccess: true,
            ProviderReference: $"{platform.ToString().ToUpper()}_{reference}",
            Error: null));
    }
}
