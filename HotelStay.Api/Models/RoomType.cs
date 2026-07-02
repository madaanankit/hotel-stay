using System;

namespace HotelStay.Api.Models
{
    /// <summary>
    /// Canonical room categories used across providers.
    /// </summary>
    public enum RoomType
    {
        /// <summary>Standard room.</summary>
        Standard,
        /// <summary>Deluxe room.</summary>
        Deluxe,
        /// <summary>Suite.</summary>
        Suite,
        /// <summary>Unknown or unmapped room type.</summary>
        Unknown
    }
}
