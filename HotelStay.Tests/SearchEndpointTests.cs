using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace HotelStay.Tests
{
    public class SearchEndpointTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public SearchEndpointTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Search_MissingParameters_Returns400()
        {
            var client = _factory.CreateClient();

            var resp = await client.GetAsync("/hotels/search");

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(json.TryGetProperty("title", out _));
        }

        [Fact]
        public async Task Search_ValidParameters_Returns200AndResultsArray()
        {
            var client = _factory.CreateClient();
            var url = "/hotels/search?destination=NYC&checkIn=2026-07-01&checkOut=2026-07-03";

            var resp = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
            // Response shape should contain 'results' array
            Assert.True(json.TryGetProperty("results", out var results));
            Assert.True(results.ValueKind == JsonValueKind.Array);
        }
    }
}
