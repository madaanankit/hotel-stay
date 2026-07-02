using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using HotelStay.Api.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace HotelStay.Tests
{
    public class ReservationEndpointTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public ReservationEndpointTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Reserve_Succeeds_Returns201AndReference()
        {
            var client = _factory.CreateClient();

            var body = new ReserveRequestBody
            {
                GuestName = "Alice",
                Destination = "NYC",
                DocumentType = "Passport",
                DocumentNumber = "P123456",
                Provider = "PremierStays",
                RoomType = "Deluxe",
                CheckIn = "2026-07-10",
                CheckOut = "2026-07-12",
                RatePerNight = 100m,
                Currency = "USD"
            };

            var resp = await client.PostAsJsonAsync("/hotels/reserve", body);

            if (resp.StatusCode != HttpStatusCode.Created)
            {
                var text = await resp.Content.ReadAsStringAsync();
                // Preserve response body to aid debugging in test failures.
                Assert.True(false, $"Expected 201 Created but got {(int)resp.StatusCode}: {text}");
            }
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(json.TryGetProperty("reservationId", out var id));
        }

        [Fact]
        public async Task Reserve_Fails_When_Document_Not_Accepted_Returns422()
        {
            var client = _factory.CreateClient();

            var body = new ReserveRequestBody
            {
                GuestName = "Bob",
                Destination = "LON",
                DocumentType = "NationalId",
                DocumentNumber = "NID123",
                Provider = "BudgetNests",
                RoomType = "Standard",
                CheckIn = "2026-07-10",
                CheckOut = "2026-07-12",
                RatePerNight = 120m,
                Currency = "GBP"
            };

            var resp = await client.PostAsJsonAsync("/hotels/reserve", body);

            if (resp.StatusCode != (HttpStatusCode)422)
            {
                var text = await resp.Content.ReadAsStringAsync();
                Assert.True(false, $"Expected 422 UnprocessableEntity but got {(int)resp.StatusCode}: {text}");
            }
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(json.TryGetProperty("errors", out _));
        }

        [Fact]
        public async Task GetReservation_Returns200_For_Existing_Reference()
        {
            var client = _factory.CreateClient();

            var body = new ReserveRequestBody
            {
                GuestName = "Carol",
                Destination = "NYC",
                DocumentType = "Passport",
                DocumentNumber = "P999",
                Provider = "PremierStays",
                RoomType = "Standard",
                CheckIn = "2026-07-10",
                CheckOut = "2026-07-11",
                RatePerNight = 80m,
                Currency = "USD"
            };

            var resp = await client.PostAsJsonAsync("/hotels/reserve", body);
            if (resp.StatusCode != HttpStatusCode.Created)
            {
                var text = await resp.Content.ReadAsStringAsync();
                Assert.True(false, $"Expected 201 Created but got {(int)resp.StatusCode}: {text}");
            }
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(json.TryGetProperty("reservationId", out var id));
            var reference = id.GetString();

            var get = await client.GetAsync($"/hotels/reservation/{reference}");
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            var conf = await get.Content.ReadFromJsonAsync<ReservationConfirmation>();
            Assert.Equal(reference, conf.ReservationId);
        }

        [Fact]
        public async Task GetReservation_Returns404_For_UnknownReference()
        {
            var client = _factory.CreateClient();
            var get = await client.GetAsync($"/hotels/reservation/NO-SUCH-REF");
            if (get.StatusCode != HttpStatusCode.NotFound)
            {
                var text = await get.Content.ReadAsStringAsync();
                Assert.True(false, $"Expected 404 NotFound but got {(int)get.StatusCode}: {text}");
            }
        }
    }
}
