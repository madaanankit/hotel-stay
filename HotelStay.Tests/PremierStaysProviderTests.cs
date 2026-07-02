using System;
using System.Linq;
using System.Threading.Tasks;
using HotelStay.Api.Providers.PremierStays;
using Xunit;

namespace HotelStay.Tests
{
    public class PremierStaysProviderTests
    {
        [Fact]
        public async Task SearchAsync_KnownDestination_ReturnsExpectedCountAndShape()
        {
            var provider = new PremierStaysProvider();

            var results = await provider.SearchAsync("NYC", DateOnly.Parse("2026-07-10"), DateOnly.Parse("2026-07-13"));

            Assert.NotNull(results);
            Assert.True(results.Count >= 2); // NYC has multiple types in canned responses

            var deluxe = results.FirstOrDefault(r => r.RoomType == HotelStay.Api.Models.RoomType.Deluxe);
            Assert.NotNull(deluxe);
            Assert.Equal("PremierStays", deluxe.Provider);
            Assert.NotNull(deluxe.CancellationPolicy);
        }

        [Fact]
        public async Task SearchAsync_CancellationPolicy_ParsesFreeAndNonRefundable()
        {
            var provider = new PremierStaysProvider();

            var nyc = await provider.SearchAsync("NYC", DateOnly.Parse("2026-07-10"), DateOnly.Parse("2026-07-11"));
            var suite = nyc.FirstOrDefault(r => r.RoomType == HotelStay.Api.Models.RoomType.Suite);
            Assert.NotNull(suite);
            Assert.Equal(HotelStay.Api.Models.CancellationPolicy.PolicyKind.NonRefundable, suite.CancellationPolicy.Kind);

            var deluxe = nyc.FirstOrDefault(r => r.RoomType == HotelStay.Api.Models.RoomType.Deluxe);
            Assert.NotNull(deluxe);
            Assert.Equal(HotelStay.Api.Models.CancellationPolicy.PolicyKind.FreeCancellation, deluxe.CancellationPolicy.Kind);
        }

        [Fact]
        public async Task SearchAsync_UnknownDestination_ReturnsEmpty()
        {
            var provider = new PremierStaysProvider();

            var results = await provider.SearchAsync("UNKNOWN", DateOnly.Parse("2026-07-10"), DateOnly.Parse("2026-07-11"));

            Assert.NotNull(results);
            Assert.Empty(results);
        }

        [Fact]
        public async Task SearchAsync_Deterministic_SameInputsReturnEqualResults()
        {
            var provider = new PremierStaysProvider();

            var a = await provider.SearchAsync("NYC", DateOnly.Parse("2026-07-10"), DateOnly.Parse("2026-07-13"));
            var b = await provider.SearchAsync("NYC", DateOnly.Parse("2026-07-10"), DateOnly.Parse("2026-07-13"));

            Assert.Equal(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.Equal(a[i].Id, b[i].Id);
                Assert.Equal(a[i].TotalPrice.Amount, b[i].TotalPrice.Amount);
            }
        }
    }
}
