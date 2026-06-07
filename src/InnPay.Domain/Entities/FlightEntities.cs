using InnPay.Domain.Enums;

namespace InnPay.Domain.Entities;

/// <summary>
/// Flight booking via Amadeus Travel API.
/// </summary>
public class FlightBooking : BaseEntity
{
    public Guid AccountId { get; set; }
    public Guid WalletId { get; set; }

    public string BookingReference { get; set; } = string.Empty;
    public string Airline { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;         // IATA code e.g. LOS
    public string Destination { get; set; } = string.Empty;    // IATA code e.g. LHR
    public DateTime DepartureAt { get; set; }
    public DateTime? ReturnAt { get; set; }                    // null for one-way

    public int PassengerCount { get; set; }
    public string CabinClass { get; set; } = "Economy";
    public TripType TripType { get; set; }

    public decimal AmountCharged { get; set; }
    public WalletCurrency Currency { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public string? ETicketUrl { get; set; }
    public string? AmadeusOrderId { get; set; }
    public string? FailureReason { get; set; }

    // Navigation
    public Account Account { get; set; } = null!;
    public Wallet Wallet { get; set; } = null!;
    public ICollection<FlightPassenger> Passengers { get; set; } = new List<FlightPassenger>();
}

/// <summary>
/// Passenger detail for a flight booking.
/// </summary>
public class FlightPassenger : BaseEntity
{
    public Guid FlightBookingId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string PassportNumber { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;

    // Navigation
    public FlightBooking FlightBooking { get; set; } = null!;
}
