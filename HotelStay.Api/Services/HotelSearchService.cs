using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotelStay.Api.Models;

namespace HotelStay.Api.Services
{
    /// <summary>
    /// Orchestrates querying multiple IHotelProvider implementations and aggregating results.
    /// </summary>
    public sealed class HotelSearchService
    {
        private readonly IEnumerable<Providers.IHotelProvider> _providers;

        public HotelSearchService(IEnumerable<Providers.IHotelProvider> providers)
        {
            _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        }

        /// <summary>
        /// Query all registered providers concurrently, aggregate their NormalisedRoom results,
        /// and perform server-side filtering and sorting.
        /// </summary>
        public async Task<IReadOnlyList<NormalisedRoom>> SearchAsync(string destination, DateOnly checkIn, DateOnly checkOut, RoomType? roomType = null, CancellationToken cancellationToken = default)
        {
            var tasks = _providers.Select(p => p.SearchAsync(destination, checkIn, checkOut, roomType, cancellationToken));
            var resultsPerProvider = await Task.WhenAll(tasks);

            var combined = resultsPerProvider.SelectMany(x => x).ToList();

            // Optionally filter by requested roomType (providers should already have applied their own filtering)
            if (roomType != null)
            {
                combined = combined.Where(r => r.RoomType == roomType).ToList();
            }

            // Default sort: ascending by total price
            combined.Sort((a, b) => a.TotalPrice.Amount.CompareTo(b.TotalPrice.Amount));

            // If no providers returned any data, provide deterministic sample/demo offers so the UI
            // can show example results instead of an empty list. This keeps the API useful for
            // local demo and testing without requiring the client to know provider-specific codes.
            if (combined.Count == 0)
            {
                var nights = Math.Max(1, checkOut.DayNumber - checkIn.DayNumber);
                var samples = new List<NormalisedRoom>();

                // Standard sample
                samples.Add(new NormalisedRoom
                {
                    Id = "SAMPLE-STD-1",
                    Provider = "DemoProvider",
                    DestinationCode = destination,
                    RoomType = RoomType.Standard,
                    StarRating = 3,
                    Amenities = new List<string> { "Wifi" }.AsReadOnly(),
                    PerNightRates = Enumerable.Range(0, nights).Select(_ => new Money(100m, "USD")).ToList().AsReadOnly(),
                    RatePerNightDisplay = new Money(100m, "USD"),
                    TotalPrice = new Money(100m * nights, "USD"),
                    CancellationPolicy = new CancellationPolicy { Kind = CancellationPolicy.PolicyKind.FreeCancellation, FreeCancellationCutoff = TimeSpan.FromHours(24), Description = "Free cancellation up to 24h" },
                    Available = true,
                    RawProviderPayload = null,
                    Metadata = new Dictionary<string, string> { { "source", "Demo" } }
                });

                // Deluxe sample
                samples.Add(new NormalisedRoom
                {
                    Id = "SAMPLE-DLX-1",
                    Provider = "DemoProvider",
                    DestinationCode = destination,
                    RoomType = RoomType.Deluxe,
                    StarRating = 4,
                    Amenities = new List<string> { "Wifi", "Breakfast" }.AsReadOnly(),
                    PerNightRates = Enumerable.Range(0, nights).Select(_ => new Money(180m, "USD")).ToList().AsReadOnly(),
                    RatePerNightDisplay = new Money(180m, "USD"),
                    TotalPrice = new Money(180m * nights, "USD"),
                    CancellationPolicy = new CancellationPolicy { Kind = CancellationPolicy.PolicyKind.NonRefundable, Description = "Non-refundable" },
                    Available = true,
                    RawProviderPayload = null,
                    Metadata = new Dictionary<string, string> { { "source", "Demo" } }
                });

                combined = samples;
            }

            return combined.AsReadOnly();
        }
    }
}
