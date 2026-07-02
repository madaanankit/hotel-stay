using System;
using System.Collections.Generic;

namespace HotelStay.Api.Models
{
    /// <summary>
    /// Reservation result returned by Reserve and GetReservation endpoints.
    /// </summary>
    public sealed record ReservationConfirmation
    {
        /// <summary>True when reservation succeeded.</summary>
        public required bool Success { get; init; }

        /// <summary>Internal reservation reference.</summary>
        public string? ReservationId { get; init; }

        /// <summary>Provider reservation identifier when available.</summary>
        public string? ProviderReservationId { get; init; }

        /// <summary>Status: Pending | Confirmed | Failed</summary>
        public required string Status { get; init; }

        /// <summary>Total price returned by provider/booking flow.</summary>
        public Money? TotalPrice { get; init; }

        /// <summary>Creation timestamp.</summary>
        public DateTimeOffset? CreatedAt { get; init; }

        /// <summary>Validation errors discovered during reservation; used for 422 responses.</summary>
        public IReadOnlyList<ValidationError>? Errors { get; init; }

        /// <summary>Raw provider response payload for troubleshooting.</summary>
        public string? RawProviderResponse { get; init; }
    }
}
