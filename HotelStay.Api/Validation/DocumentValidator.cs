using System;
using System.Collections.Generic;
using HotelStay.Api.Models;

namespace HotelStay.Api.Validation
{
    /// <summary>
    /// Validates whether the supplied document type is acceptable for the destination.
    /// NOTE: For this exercise we use a hard-coded list of domestic/international cities from spec.md.
    /// In a real system this would be a configurable data source (database, configuration, or provider rules).
    /// </summary>
    public sealed class DocumentValidator
    {
        // Hard-coded lists per spec.md. Keep in sync with frontend JS/TS validator.
        private static readonly HashSet<string> DomesticCities = new(StringComparer.OrdinalIgnoreCase)
        {
            "NYC",
            "LAX"
        };

        private static readonly HashSet<string> InternationalCities = new(StringComparer.OrdinalIgnoreCase)
        {
            "LON",
            "TYO",
            "PAR"
        };

        /// <summary>
        /// Result of validation. If IsValid is false, Error contains a ValidationError ready to include in a 422 body.
        /// </summary>
        public sealed record ValidationResult(bool IsValid, Models.ValidationError? Error);

        /// <summary>
        /// Validate the document type for the given destination code.
        /// Returns a ValidationResult with a ValidationError prepared for a 422 response when invalid.
        /// </summary>
        /// <param name="destination">Destination code (e.g. "NYC").</param>
        /// <param name="documentType">Type of document supplied by the client.</param>
        public static ValidationResult Validate(string? destination, DocumentType documentType)
        {
            if (string.IsNullOrWhiteSpace(destination))
            {
                var err = new Models.ValidationError
                {
                    Code = "UnknownDestination",
                    Field = "destination",
                    Message = "Destination is required",
                    Details = "Destination code was not supplied"
                };
                return new ValidationResult(false, err);
            }

            var dest = destination.Trim().ToUpperInvariant();

            // Domestic: accept DriverLicense, NationalId, Passport
            if (DomesticCities.Contains(dest))
            {
                if (documentType == DocumentType.DriverLicense || documentType == DocumentType.NationalId || documentType == DocumentType.Passport)
                    return new ValidationResult(true, null);

                var err = new Models.ValidationError
                {
                    Code = "InvalidDocument",
                    Field = "documentType",
                    Message = "For domestic destinations a DriverLicense, NationalId or Passport is required",
                    Details = $"Provided document type '{documentType}' is not acceptable for domestic destination {dest}"
                };
                return new ValidationResult(false, err);
            }

            // International: require Passport
            if (InternationalCities.Contains(dest))
            {
                if (documentType == DocumentType.Passport)
                    return new ValidationResult(true, null);

                var err = new Models.ValidationError
                {
                    Code = "PassportRequired",
                    Field = "documentType",
                    Message = "A passport is required for international destinations",
                    Details = $"Provided document type '{documentType}' is not acceptable for international destination {dest}"
                };
                return new ValidationResult(false, err);
            }

            // Unknown destination: be conservative and require Passport
            if (documentType == DocumentType.Passport)
                return new ValidationResult(true, null);

            var unknownErr = new Models.ValidationError
            {
                Code = "UnknownDestinationPassportRequired",
                Field = "documentType",
                Message = "Passport is required for this destination",
                Details = $"Destination '{dest}' is not recognised; passport is required by default"
            };
            return new ValidationResult(false, unknownErr);
        }

        /// <summary>
        /// Instance wrapper to allow DI usage where preferred. Delegates to static Validate.
        /// </summary>
        public ValidationResult ValidateInstance(string? destination, DocumentType documentType)
            => Validate(destination, documentType);
    }
}
