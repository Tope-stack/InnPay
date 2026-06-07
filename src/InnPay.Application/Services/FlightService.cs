using InnPay.Application.Common;
using InnPay.Application.DTOs.Request;
using InnPay.Application.DTOs.Response;
using InnPay.Application.Interfaces;
using InnPay.Domain.Entities;
using InnPay.Domain.Enums;
using InnPay.Domain.Interfaces;

namespace InnPay.Application.Services;

public class FlightService : IFlightService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordHasher _hasher;
    private readonly IFlightProvider _flightProvider;
    private readonly IEmailService _email;

    public FlightService(IUnitOfWork uow, IPasswordHasher hasher, IFlightProvider flightProvider, IEmailService email)
    {
        _uow = uow;
        _hasher = hasher;
        _flightProvider = flightProvider;
        _email = email;
    }

    public async Task<ServiceResult<IEnumerable<FlightOfferResponse>>> SearchAsync(
        FlightSearchRequest request, CancellationToken cancellationToken = default)
    {
        var offers = await _flightProvider.SearchFlightsAsync(
            request.Origin, request.Destination, request.DepartureDate,
            request.ReturnDate, request.PassengerCount, request.CabinClass,
            request.TripType.ToString());

        return ServiceResult<IEnumerable<FlightOfferResponse>>.Success(
            offers.Select(o => new FlightOfferResponse
            {
                OfferId = o.OfferId,
                Airline = o.Airline,
                Origin = o.Origin,
                Destination = o.Destination,
                DepartureAt = o.DepartureAt,
                ArrivalAt = o.ArrivalAt,
                Stops = o.Stops,
                CabinClass = o.CabinClass,
                PriceNgn = o.PriceNgn,
                PriceUsd = o.PriceUsd,
                Duration = o.Duration
            }));
    }

    public async Task<ServiceResult<FlightBookingResponse>> BookAsync(
        BookFlightRequest request, CancellationToken cancellationToken = default)
    {
        var account = await _uow.Accounts.GetByIdAsync(request.AccountId);
        if (account is null)
            return ServiceResult<FlightBookingResponse>.Fail("Account not found.", 404);

        if (!_hasher.Verify(request.TransactionPin, account.User?.TransactionPinHash ?? ""))
            return ServiceResult<FlightBookingResponse>.Fail("Invalid transaction PIN.", 401);

        var wallet = await _uow.Wallets.GetByIdAsync(request.WalletId);
        if (wallet is null || wallet.AccountId != request.AccountId)
            return ServiceResult<FlightBookingResponse>.Fail("Wallet not found.", 404);

        var passengers = request.Passengers.Select(p => new ProviderPassengerDetail(
            p.FirstName, p.LastName, p.DateOfBirth, p.PassportNumber, p.Nationality));

        var reference = $"INN-FLT-{Guid.NewGuid().ToString("N")[..10].ToUpper()}";
        var providerResult = await _flightProvider.BookAsync(request.OfferId, passengers, reference);

        if (!providerResult.IsSuccess)
            return ServiceResult<FlightBookingResponse>.Fail(providerResult.Error ?? "Booking failed.", 422);

        // TODO: compute actual price from offer — using placeholder here
        var amount = 100_000m;
        if (wallet.AvailableBalance < amount)
            return ServiceResult<FlightBookingResponse>.Fail("Insufficient balance.");

        wallet.Balance -= amount;
        wallet.AvailableBalance -= amount;

        var booking = new FlightBooking
        {
            AccountId = request.AccountId,
            WalletId = request.WalletId,
            BookingReference = providerResult.BookingReference!,
            Airline = "Airline",       // from offer data
            Origin = "LOS",            // from offer data
            Destination = "LHR",       // from offer data
            DepartureAt = DateTime.UtcNow.AddDays(7),
            PassengerCount = request.Passengers.Count,
            CabinClass = "Economy",
            TripType = TripType.OneWay,
            AmountCharged = amount,
            Currency = wallet.Currency,
            Status = BookingStatus.Confirmed,
            ETicketUrl = providerResult.ETicketUrl,
            AmadeusOrderId = providerResult.ProviderOrderId
        };

        booking.Passengers = request.Passengers.Select(p => new FlightPassenger
        {
            FirstName = p.FirstName,
            LastName = p.LastName,
            DateOfBirth = p.DateOfBirth,
            PassportNumber = p.PassportNumber,
            Nationality = p.Nationality
        }).ToList();

        await _uow.FlightBookings.AddAsync(booking);
        await _uow.Wallets.UpdateAsync(wallet);
        await _uow.SaveChangesAsync(cancellationToken);

        return ServiceResult<FlightBookingResponse>.Success(MapToResponse(booking), 201);
    }

    public async Task<ServiceResult> SendTicketAsync(
        SendTicketRequest request, CancellationToken cancellationToken = default)
    {
        var booking = await _uow.FlightBookings.GetByIdAsync(request.BookingId);
        if (booking is null)
            return ServiceResult.Fail("Booking not found.", 404);

        var account = await _uow.Accounts.GetByIdAsync(booking.AccountId);
        if (account?.User is not null)
        {
            await _email.SendAsync(
                account.User.Email,
                $"Your InnPay Flight Ticket — {booking.BookingReference}",
                $"Your e-ticket for flight {booking.Origin} → {booking.Destination} on {booking.DepartureAt:dd MMM yyyy}.\n\nBooking Reference: {booking.BookingReference}\n\nTicket: {booking.ETicketUrl}");
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult<IEnumerable<FlightBookingResponse>>> GetBookingsAsync(
        Guid accountId, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var bookings = await _uow.FlightBookings.GetByAccountIdAsync(accountId, page, pageSize);
        return ServiceResult<IEnumerable<FlightBookingResponse>>.Success(bookings.Select(MapToResponse));
    }

    private static FlightBookingResponse MapToResponse(FlightBooking b) => new()
    {
        BookingId = b.Id,
        BookingReference = b.BookingReference,
        Airline = b.Airline,
        Origin = b.Origin,
        Destination = b.Destination,
        DepartureAt = b.DepartureAt,
        PassengerCount = b.PassengerCount,
        AmountCharged = b.AmountCharged,
        Currency = b.Currency,
        Status = b.Status,
        ETicketUrl = b.ETicketUrl,
        CreatedAt = b.CreatedAt
    };
}
