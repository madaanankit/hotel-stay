using System;

namespace HotelStay.Api.Models
{
    /// <summary>
    /// Supported document types for reservation identity.
    /// </summary>
    public enum DocumentType
    {
        /// <summary>Passport document.</summary>
        Passport,
        /// <summary>National identity card.</summary>
        NationalId,
        /// <summary>Driver's license.</summary>
        DriverLicense,
        /// <summary>Other document types.</summary>
        Other
    }
}
