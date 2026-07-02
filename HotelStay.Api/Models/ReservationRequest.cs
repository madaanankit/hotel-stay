using System;
using System.Collections.Generic;

namespace HotelStay.Api.Models
{
    /// <summary>
    /// Reservation request sent to Reserve endpoint and provider adapters.
    /// </summary>
    public sealed record ReservationRequest
    {
        /// <summary>Target provider (e.g. "PremierStays").</summary>
        public required string Provider { get; init; }

        /// <summary>Provider-scoped result id (from NormalisedRoom.Id).</summary>
        public required string ProviderResultId { get; init; }

        /// <summary>Check-in date.</summary>
        public required DateOnly CheckIn { get; init; }

        /// <summary>Check-out date.</summary>
        public required DateOnly CheckOut { get; init; }

        /// <summary>Number of guests.</summary>
        public required int Guests { get; init; }

        /// <summary>Primary guest details.</summary>
        public required GuestInfo PrimaryGuest { get; init; }

        /// <summary>Identity documents required for reservation.</summary>
        public required IReadOnlyList<Document> Documents { get; init; }

        /// <summary>Optional currency to request from provider.</summary>
        public string? Currency { get; init; }

        /// <summary>Optional idempotency key to deduplicate reservation attempts.</summary>
        public string? IdempotencyKey { get; init; }
    }
}
