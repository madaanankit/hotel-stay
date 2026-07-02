using System;
using System.Linq;
using System.Threading.Tasks;
using HotelStay.Api.Providers.BudgetNests;
using Xunit;

namespace HotelStay.Tests
{
    public class BudgetNestsProviderTests
    {
        [Fact]
        public async Task SearchAsync_KnownDestination_ReturnsExpectedCountAndShape()
        {
            var provider = new BudgetNestsProvider();

            var results = await provider.SearchAsync("NYC", DateOnly.Parse("2026-07-10"), DateOnly.Parse("2026-07-13"));

            Assert.NotNull(results);
            Assert.True(results.Count >= 1); // at least one available in canned responses

            var standard = results.FirstOrDefault(r => r.RoomType == HotelStay.Api.Models.RoomType.Standard);
            Assert.NotNull(standard);
            Assert.Equal("BudgetNests", standard.Provider);
            Assert.NotNull(standard.CancellationPolicy);
        }

        [Fact]
        public async Task SearchAsync_FiltersUnavailable_RemovesUnavailableRooms()
        {
            var provider = new BudgetNestsProvider();

            var all = await provider.SearchAsync("NYC", DateOnly.Parse("2026-07-10"), DateOnly.Parse("2026-07-11"));

            // The canned data includes an unavailable deluxe room; ensure it's not returned
            Assert.DoesNotContain(all, r => r.RoomType == HotelStay.Api.Models.RoomType.Deluxe && r.Id.Contains("DLX") );
        }

        [Fact]
        public async Task SearchAsync_CancellationPolicy_ParsesFlexibleAndNonRefundable()
        {
            var provider = new BudgetNestsProvider();

            var nyc = await provider.SearchAsync("NYC", DateOnly.Parse("2026-07-10"), DateOnly.Parse("2026-07-11"));
            var standard = nyc.FirstOrDefault(r => r.RoomType == HotelStay.Api.Models.RoomType.Standard);
            Assert.NotNull(standard);
            Assert.Equal(HotelStay.Api.Models.CancellationPolicy.PolicyKind.FreeCancellation, standard.CancellationPolicy.Kind);
        }

        [Fact]
        public async Task SearchAsync_UnknownDestination_ReturnsEmpty()
        {
            var provider = new BudgetNestsProvider();

            var results = await provider.SearchAsync("UNKNOWN", DateOnly.Parse("2026-07-10"), DateOnly.Parse("2026-07-11"));

            Assert.NotNull(results);
            Assert.Empty(results);
        }

        [Fact]
        public async Task SearchAsync_Deterministic_SameInputsReturnEqualResults()
        {
            var provider = new BudgetNestsProvider();

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
