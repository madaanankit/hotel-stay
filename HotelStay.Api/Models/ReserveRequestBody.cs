using System;

namespace HotelStay.Api.Models
{
    /// <summary>
    /// Request body for POST /hotels/reserve in the stubbed implementation.
    /// Fields are intentionally simple for the scaffold: ratePerNight is used to compute totals.
    /// </summary>
    public sealed record ReserveRequestBody
    {
        public required string GuestName { get; init; }
        public required string Destination { get; init; }
        public required string DocumentType { get; init; }
        public required string DocumentNumber { get; init; }
        public required string Provider { get; init; }
        public required string RoomType { get; init; }
        public required string CheckIn { get; init; } // yyyy-MM-dd
        public required string CheckOut { get; init; } // yyyy-MM-dd
        public decimal? RatePerNight { get; init; }
        public string? Currency { get; init; }
    }
}
