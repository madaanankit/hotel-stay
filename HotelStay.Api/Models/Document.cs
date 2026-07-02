using System;

namespace HotelStay.Api.Models
{
    /// <summary>
    /// Identity document used for reservation validation.
    /// </summary>
    public sealed record Document
    {
        /// <summary>Type of document.</summary>
        public required DocumentType Type { get; init; }

        /// <summary>Document number.</summary>
        public required string Number { get; init; }

        /// <summary>Country of issue (ISO2).</summary>
        public required string CountryOfIssue { get; init; }

        /// <summary>Optional expiry date. Must be in the future when present.</summary>
        public DateOnly? ExpiryDate { get; init; }
    }
}
