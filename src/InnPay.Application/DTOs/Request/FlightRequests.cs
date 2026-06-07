using InnPay.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace InnPay.Application.DTOs.Request;

public class FlightSearchRequest
{
    [Required, StringLength(3, MinimumLength = 3)]
    public string Origin { get; set; } = string.Empty;         // IATA code e.g. LOS

    [Required, StringLength(3, MinimumLength = 3)]
    public string Destination { get; set; } = string.Empty;    // IATA code e.g. LHR

    [Required] public DateTime DepartureDate { get; set; }
    public DateTime? ReturnDate { get; set; }

    [Required, Range(1, 9)] public int PassengerCount { get; set; } = 1;
    [Required] public TripType TripType { get; set; } = TripType.OneWay;

    [StringLength(20)] public string CabinClass { get; set; } = "Economy";
}

public class BookFlightRequest
{
    [Required] public Guid AccountId { get; set; }
    [Required] public Guid WalletId { get; set; }

    /// <summary>Amadeus offer ID from the search results.</summary>
    [Required] public string OfferId { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public List<PassengerDetailRequest> Passengers { get; set; } = new();

    [Required, StringLength(6, MinimumLength = 6)]
    public string TransactionPin { get; set; } = string.Empty;
}

public class PassengerDetailRequest
{
    [Required, StringLength(100)] public string FirstName { get; set; } = string.Empty;
    [Required, StringLength(100)] public string LastName { get; set; } = string.Empty;
    [Required] public DateTime DateOfBirth { get; set; }

    [Required, StringLength(20)] public string PassportNumber { get; set; } = string.Empty;

    [Required, StringLength(3, MinimumLength = 2)]
    public string Nationality { get; set; } = string.Empty;
}

public class SendTicketRequest
{
    [Required] public Guid BookingId { get; set; }
}
