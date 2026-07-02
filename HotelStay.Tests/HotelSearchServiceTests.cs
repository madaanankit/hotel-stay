using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotelStay.Api.Models;
using HotelStay.Api.Services;
using HotelStay.Api.Providers;
using Xunit;

namespace HotelStay.Tests
{
    public class HotelSearchServiceTests
    {
        private sealed class EmptyProvider : IHotelProvider
        {
            public Task<IReadOnlyList<NormalisedRoom>> SearchAsync(string destination, DateOnly checkIn, DateOnly checkOut, RoomType? roomType = null, CancellationToken cancellationToken = default)
            {
                return Task.FromResult((IReadOnlyList<NormalisedRoom>)Array.Empty<NormalisedRoom>());
            }
        }

        private sealed class SimpleProvider : IHotelProvider
        {
            private readonly NormalisedRoom _a;
            private readonly NormalisedRoom _b;
            public SimpleProvider(NormalisedRoom a, NormalisedRoom b)
            {
                _a = a; _b = b;
            }

            public Task<IReadOnlyList<NormalisedRoom>> SearchAsync(string destination, DateOnly checkIn, DateOnly checkOut, RoomType? roomType = null, CancellationToken cancellationToken = default)
            {
                return Task.FromResult((IReadOnlyList<NormalisedRoom>)new List<NormalisedRoom> { _b, _a });
            }
        }

        [Fact]
        public async Task SearchAsync_WhenAllProvidersEmpty_ReturnsDemoSamples()
        {
            var svc = new HotelSearchService(new IHotelProvider[] { new EmptyProvider() });

            var results = await svc.SearchAsync("ANY", DateOnly.Parse("2026-07-01"), DateOnly.Parse("2026-07-04"));

            Assert.NotNull(results);
            // Demo fallback should provide at least two sample offers
            Assert.True(results.Count >= 2);
            // Sample first item should have TotalPrice > 0
            Assert.True(results[0].TotalPrice.Amount > 0);
        }

        [Fact]
        public async Task SearchAsync_CombinesAndSortsProviderResultsAscendingByTotal()
        {
            var a = new NormalisedRoom
            {
                Id = "A",
                Provider = "P1",
                DestinationCode = "X",
                RoomType = RoomType.Standard,
                PerNightRates = new System.Collections.Generic.List<Money> { new Money(50m, "USD") },
                RatePerNightDisplay = new Money(50m, "USD"),
                TotalPrice = new Money(50m, "USD"),
                CancellationPolicy = new CancellationPolicy { Kind = CancellationPolicy.PolicyKind.Unknown, Description = "" }
            };

            var b = new NormalisedRoom
            {
                Id = "B",
                Provider = "P2",
                DestinationCode = "X",
                RoomType = RoomType.Deluxe,
                PerNightRates = new System.Collections.Generic.List<Money> { new Money(150m, "USD") },
                RatePerNightDisplay = new Money(150m, "USD"),
                TotalPrice = new Money(150m, "USD"),
                CancellationPolicy = new CancellationPolicy { Kind = CancellationPolicy.PolicyKind.Unknown, Description = "" }
            };

            var svc = new HotelSearchService(new IHotelProvider[] { new SimpleProvider(a, b) });
            var results = await svc.SearchAsync("X", DateOnly.Parse("2026-07-01"), DateOnly.Parse("2026-07-02"));

            Assert.Equal(2, results.Count);
            Assert.Equal("A", results[0].Id); // A has lower total and should come first
            Assert.Equal("B", results[1].Id);
        }
    }
}
