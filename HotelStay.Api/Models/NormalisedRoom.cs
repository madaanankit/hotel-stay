using System;
using System.Collections.Generic;

namespace HotelStay.Api.Models
{
    /// <summary>
    /// Normalised room search result returned by provider adapters.
    /// </summary>
    public sealed record NormalisedRoom
    {
        /// <summary>Provider-scoped identifier for this offer.</summary>
        public required string Id { get; init; }

        /// <summary>Name of the provider that produced this result.</summary>
        public required string Provider { get; init; }

        /// <summary>Destination code echoed from the search request.</summary>
        public required string DestinationCode { get; init; }

        /// <summary>Canonical room category.</summary>
        public required RoomType RoomType { get; init; }

        /// <summary>Optional star rating (when provided by the source).</summary>
        public int? StarRating { get; init; }

        /// <summary>Optional amenity list (provider-specific).</summary>
        public IReadOnlyList<string>? Amenities { get; init; }

        /// <summary>Per-night rates, one entry per night of the stay.</summary>
        public required IReadOnlyList<Money> PerNightRates { get; init; }

        /// <summary>Canonical per-night rate to display in UI (e.g. first-night or averaged).</summary>
        public required Money RatePerNightDisplay { get; init; }

        /// <summary>Total price for the stay (sum of per-night rates including provider fees when available).</summary>
        public required Money TotalPrice { get; init; }

        /// <summary>Cancellation policy for the rate.</summary>
        public required CancellationPolicy CancellationPolicy { get; init; }

        /// <summary>Whether the provider indicates this offer is available.</summary>
        public bool Available { get; init; } = true;

        /// <summary>Raw provider JSON payload as string for troubleshooting.</summary>
        public string? RawProviderPayload { get; init; }

        /// <summary>Provider-specific metadata (tokens, rate keys) required for reservation.</summary>
        public IReadOnlyDictionary<string,string>? Metadata { get; init; }
    }
}
