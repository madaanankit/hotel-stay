using System;

namespace HotelStay.Api.Models
{
    /// <summary>
    /// Simple money value object used for prices.
    /// </summary>
    public sealed record Money(decimal Amount, string Currency)
    {
        /// <summary>Return a new Money representing multiplication by a factor.</summary>
        public Money Multiply(int factor) => new(Amount * factor, Currency);
    }
}
