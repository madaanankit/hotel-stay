using System;

namespace HotelStay.Api.Models
{
    /// <summary>
    /// Search request supplied by API consumers.
    /// </summary>
    public sealed record SearchQuery
    {
        /// <summary>Destination code (required).</summary>
        public required string Destination { get; init; }

        /// <summary>Check-in date (yyyy-MM-dd).</summary>
        public required DateOnly CheckIn { get; init; }

        /// <summary>Check-out date (yyyy-MM-dd).</summary>
        public required DateOnly CheckOut { get; init; }

        /// <summary>Number of guests.</summary>
        public required int Guests { get; init; }

        /// <summary>Optional preferred currency (ISO 4217).</summary>
        public string? Currency { get; init; }
    }
}
