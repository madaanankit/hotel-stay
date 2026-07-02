using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace HotelStay.Api.Models
{
    /// <summary>
    /// ProblemDetails-friendly validation error used for 422 Unprocessable Entity responses.
    /// </summary>
    public sealed record ValidationError
    {
        /// <summary>Machine-readable error code.</summary>
        public required string Code { get; init; }

        /// <summary>Optional dotted path to the affected field.</summary>
        public string? Field { get; init; }

        /// <summary>Human-readable error message.</summary>
        public required string Message { get; init; }

        /// <summary>Optional developer-facing details.</summary>
        public string? Details { get; init; }
    }
}
