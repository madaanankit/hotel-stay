using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotelStay.Api.Models;

namespace HotelStay.Api.Providers.PremierStays
{
    /// <summary>
    /// In-memory adapter that simulates the PremierStays provider.
    /// Always returns offers as available and maps PascalCase provider DTOs to <see cref="NormalisedRoom"/>.
    /// Deterministic: same inputs produce the same outputs.
    /// </summary>
    public class PremierStaysProvider : IHotelProvider
    {
        private readonly IReadOnlyDictionary<string, List<PremierStaysRoomDto>> _responses;

        /// <summary>
        /// Create a new provider instance with canned responses for supported destinations.
        /// </summary>
        public PremierStaysProvider()
        {
            _responses = CreateCannedResponses();
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<NormalisedRoom>> SearchAsync(string destination, DateOnly checkIn, DateOnly checkOut, RoomType? roomType = null, CancellationToken cancellationToken = default)
        {
            if (destination is null) throw new ArgumentNullException(nameof(destination));

            var key = destination.Trim().ToUpperInvariant();
            if (!_responses.TryGetValue(key, out var list))
            {
                // Unknown destination -> empty list
                return Task.FromResult((IReadOnlyList<NormalisedRoom>)Array.Empty<NormalisedRoom>());
            }

            var nights = Math.Max(0, checkOut.DayNumber - checkIn.DayNumber);

            var filtered = list
                .Where(d => roomType == null || MapRoomType(d.RoomType) == roomType)
                .Select(dto => Map(dto, nights))
                .ToList()
                .AsReadOnly();

            return Task.FromResult((IReadOnlyList<NormalisedRoom>)filtered);
        }

        private NormalisedRoom Map(PremierStaysRoomDto dto, int nights)
        {
            var roomType = MapRoomType(dto.RoomType);

            var perNightList = Enumerable.Range(0, Math.Max(1, nights))
                .Select(_ => new Money(dto.RatePerNight, dto.Currency))
                .ToList();

            var total = new Money(dto.RatePerNight * Math.Max(1, nights), dto.Currency);

            var cancellation = ParseCancellation(dto.CancellationPolicy);

            return new NormalisedRoom
            {
                Id = dto.Id,
                Provider = dto.Provider,
                DestinationCode = dto.DestinationCode,
                RoomType = roomType,
                StarRating = dto.StarRating,
                Amenities = dto.Amenities?.AsReadOnly(),
                PerNightRates = perNightList.AsReadOnly(),
                RatePerNightDisplay = new Money(dto.RatePerNight, dto.Currency),
                TotalPrice = total,
                CancellationPolicy = cancellation,
                Available = true,
                RawProviderPayload = dto.RawJson,
                Metadata = new Dictionary<string,string> { { "source", "PremierStays" } }
            };
        }

        private static RoomType MapRoomType(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return RoomType.Unknown;
            return raw.Trim().ToLowerInvariant() switch
            {
                "standard" => RoomType.Standard,
                "deluxe" => RoomType.Deluxe,
                "suite" => RoomType.Suite,
                _ => RoomType.Unknown
            };
        }

        private static CancellationPolicy ParseCancellation(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new CancellationPolicy { Kind = CancellationPolicy.PolicyKind.Unknown, Description = text };

            var lower = text.Trim().ToLowerInvariant();
            if (lower.Contains("freecancellation") || lower.Contains("free cancellation") || lower.Contains("freecancellable") || lower.Contains("free-cancellation") || lower.Contains("up to 48h"))
            {
                return new CancellationPolicy
                {
                    Kind = CancellationPolicy.PolicyKind.FreeCancellation,
                    FreeCancellationCutoff = TimeSpan.FromHours(48),
                    Description = text
                };
            }

            if (lower.Contains("nonrefundable") || lower.Contains("non-refundable") || lower.Contains("non refundable"))
            {
                return new CancellationPolicy { Kind = CancellationPolicy.PolicyKind.NonRefundable, Description = text };
            }

            return new CancellationPolicy { Kind = CancellationPolicy.PolicyKind.Unknown, Description = text };
        }

        private static IReadOnlyDictionary<string, List<PremierStaysRoomDto>> CreateCannedResponses()
        {
            // Use the cities listed in spec.md
            var nyc = new List<PremierStaysRoomDto>
            {
                new PremierStaysRoomDto("PS-NYC-STD","PremierStays","NYC","Standard",120m, "USD",3, 4, new[] { "Wifi", "Gym" }, "FreeCancellation up to 48h before check-in"),
                new PremierStaysRoomDto("PS-NYC-DLX","PremierStays","NYC","Deluxe",250m, "USD",3, 5, new[] { "Wifi", "Pool", "Gym" }, "FreeCancellation up to 48h before check-in"),
                new PremierStaysRoomDto("PS-NYC-SUI","PremierStays","NYC","Suite",400m, "USD",3, 5, new[] { "Wifi", "Pool", "Spa" }, "NonRefundable")
            };

            var lax = new List<PremierStaysRoomDto>
            {
                new PremierStaysRoomDto("PS-LAX-STD","PremierStays","LAX","Standard",110m, "USD",2, 4, new[] { "Wifi" }, "FreeCancellation up to 48h before check-in"),
                new PremierStaysRoomDto("PS-LAX-DLX","PremierStays","LAX","Deluxe",220m, "USD",2, 4, new[] { "Wifi", "Gym" }, "NonRefundable")
            };

            var lon = new List<PremierStaysRoomDto>
            {
                new PremierStaysRoomDto("PS-LON-STD","PremierStays","LON","Standard",140m, "GBP",3, 4, new[] { "Wifi" }, "FreeCancellation up to 48h before check-in"),
                new PremierStaysRoomDto("PS-LON-SUI","PremierStays","LON","Suite",420m, "GBP",3, 5, new[] { "Wifi", "Breakfast" }, "NonRefundable")
            };

            var tyo = new List<PremierStaysRoomDto>
            {
                new PremierStaysRoomDto("PS-TYO-STD","PremierStays","TYO","Standard",130m, "JPY",2, 4, new[] { "Wifi" }, "FreeCancellation up to 48h before check-in")
            };

            var par = new List<PremierStaysRoomDto>
            {
                new PremierStaysRoomDto("PS-PAR-DLX","PremierStays","PAR","Deluxe",200m, "EUR",2, 5, new[] { "Wifi", "Breakfast" }, "FreeCancellation up to 48h before check-in")
            };

            return new Dictionary<string, List<PremierStaysRoomDto>>(StringComparer.OrdinalIgnoreCase)
            {
                ["NYC"] = nyc,
                ["LAX"] = lax,
                ["LON"] = lon,
                ["TYO"] = tyo,
                ["PAR"] = par
            };
        }

        /// <summary>
        /// Internal DTO that models the PascalCase JSON returned by PremierStays.
        /// </summary>
        private sealed record PremierStaysRoomDto(string Id, string Provider, string DestinationCode, string RoomType, decimal RatePerNight, string Currency, int Nights, int StarRating, string[]? Amenities, string CancellationPolicy)
        {
            public string? RawJson { get; init; } = null;
        }
    }
}
