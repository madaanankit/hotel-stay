using System;
using System.Text.Json.Serialization;

namespace HotelStay.Api.Models
{
    /// <summary>
    /// Normalised cancellation policy for a room rate.
    /// </summary>
    public sealed record CancellationPolicy
    {
        /// <summary>
        /// Kind of cancellation policy.
        /// </summary>
        public enum PolicyKind { FreeCancellation, NonRefundable, Unknown }

        /// <summary>
        /// Policy kind (FreeCancellation, NonRefundable, Unknown).
        /// </summary>
        public PolicyKind Kind { get; init; }

        /// <summary>
        /// When Kind == FreeCancellation, how long before check-in free cancellation applies.
        /// </summary>
        public TimeSpan? FreeCancellationCutoff { get; init; }

        /// <summary>
        /// Original provider text suitable for display.
        /// </summary>
        public string? Description { get; init; }
    }
}
