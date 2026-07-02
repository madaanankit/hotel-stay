using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotelStay.Api.Models;

namespace HotelStay.Api.Providers
{
    /// <summary>
    /// Abstraction for hotel provider adapters.
    /// Implementations translate provider-specific responses into <see cref="NormalisedRoom"/> instances
    /// and are responsible for filtering out any provider-side unavailable offers before returning results.
    /// </summary>
    public interface IHotelProvider
    {
        /// <summary>
        /// Search availability for the given destination and date range.
        /// Implementations must return a collection of <see cref="NormalisedRoom"/> mapped from
        /// the provider's raw payload. Implementations should filter out entries that are not available
        /// (for example, where the provider returns an explicit "available": false flag) before returning.
        /// </summary>
        /// <param name="destination">Destination code supplied by the caller.</param>
        /// <param name="checkIn">Check-in date.</param>
        /// <param name="checkOut">Check-out date.</param>
        /// <param name="roomType">Optional preferred room category to filter by.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that resolves to a read-only list of normalised rooms.</returns>
        Task<IReadOnlyList<NormalisedRoom>> SearchAsync(
            string destination,
            DateOnly checkIn,
            DateOnly checkOut,
            RoomType? roomType = null,
            CancellationToken cancellationToken = default);
    }
}
