using System;

namespace HotelStay.Api.Models
{
    /// <summary>
    /// Primary guest contact information.
    /// </summary>
    public sealed record GuestInfo(string Name, string Email, string? Phone);
}
