using InnPay.Application.DTOs.Request;
using InnPay.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InnPay.API.Controllers;

/// <summary>Flight search, booking, and e-ticket delivery via Amadeus Travel API.</summary>
[Authorize]
public class FlightsController : BaseController
{
    private readonly IFlightService _flightService;
    public FlightsController(IFlightService flightService) => _flightService = flightService;

    /// <summary>GET /api/v1/flights/search</summary>
    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromQuery] FlightSearchRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return FromResult(await _flightService.SearchAsync(request, ct));
    }

    /// <summary>POST /api/v1/flights/book</summary>
    [HttpPost("book")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Book([FromBody] BookFlightRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return FromResult(await _flightService.BookAsync(request, ct));
    }

    /// <summary>POST /api/v1/flights/ticket/send — send e-ticket to user email.</summary>
    [HttpPost("ticket/send")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendTicket([FromBody] SendTicketRequest request, CancellationToken ct)
        => FromResult(await _flightService.SendTicketAsync(request, ct));

    /// <summary>GET /api/v1/flights/bookings/{accountId}</summary>
    [HttpGet("bookings/{accountId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Bookings(Guid accountId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => FromResult(await _flightService.GetBookingsAsync(accountId, page, pageSize, ct));
}
