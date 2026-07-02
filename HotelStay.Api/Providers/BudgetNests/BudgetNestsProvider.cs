using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using HotelStay.Api.Models;

namespace HotelStay.Api.Providers.BudgetNests
{
    /// <summary>
    /// In-memory adapter that simulates the BudgetNests provider.
    /// Returns snake_case DTOs and filters out unavailable offers before returning.
    /// Deterministic: same inputs produce the same outputs.
    /// </summary>
    public class BudgetNestsProvider : IHotelProvider
    {
        private readonly IReadOnlyDictionary<string, List<BudgetNestsRoomDto>> _responses;

        public BudgetNestsProvider()
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
                return Task.FromResult((IReadOnlyList<NormalisedRoom>)Array.Empty<NormalisedRoom>());
            }

            var nights = Math.Max(0, checkOut.DayNumber - checkIn.DayNumber);

            var available = list
                .Where(d => d.Available)
                .Where(d => roomType == null || MapRoomType(d.RoomType) == roomType)
                .Select(dto => Map(dto, nights))
                .ToList()
                .AsReadOnly();

            return Task.FromResult((IReadOnlyList<NormalisedRoom>)available);
        }

        private NormalisedRoom Map(BudgetNestsRoomDto dto, int nights)
        {
            var roomType = MapRoomType(dto.RoomType);

            var perNightCount = Math.Max(1, nights);
            var perNightList = Enumerable.Range(0, perNightCount)
                .Select(_ => new Money(dto.PerNightRate, dto.Currency))
                .ToList();

            var total = new Money(dto.PerNightRate * perNightCount, dto.Currency);

            var cancellation = ParseCancellation(dto.CancellationPolicy);

            return new NormalisedRoom
            {
                Id = dto.Id,
                Provider = dto.Provider,
                DestinationCode = dto.DestinationCode,
                RoomType = roomType,
                StarRating = null,
                Amenities = null,
                PerNightRates = perNightList.AsReadOnly(),
                RatePerNightDisplay = new Money(dto.PerNightRate, dto.Currency),
                TotalPrice = total,
                CancellationPolicy = cancellation,
                Available = true,
                RawProviderPayload = dto.RawJson,
                Metadata = new Dictionary<string,string> { { "source", "BudgetNests" } }
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
            if (lower.Contains("flexible") || lower.Contains("up to 24h") || lower.Contains("24h"))
            {
                return new CancellationPolicy
                {
                    Kind = CancellationPolicy.PolicyKind.FreeCancellation,
                    FreeCancellationCutoff = TimeSpan.FromHours(24),
                    Description = text
                };
            }

            if (lower.Contains("nonrefundable") || lower.Contains("non-refundable") || lower.Contains("non refundable"))
            {
                return new CancellationPolicy { Kind = CancellationPolicy.PolicyKind.NonRefundable, Description = text };
            }

            return new CancellationPolicy { Kind = CancellationPolicy.PolicyKind.Unknown, Description = text };
        }

        private static IReadOnlyDictionary<string, List<BudgetNestsRoomDto>> CreateCannedResponses()
        {
            // Create canned responses. Each destination includes at least one unavailable room to test filtering.
            var nyc = new List<BudgetNestsRoomDto>
            {
                new BudgetNestsRoomDto("BN-NYC-STD","BudgetNests","NYC","standard",75m,"USD","Flexible up to 24h before check-in", true),
                new BudgetNestsRoomDto("BN-NYC-DLX","BudgetNests","NYC","deluxe",160m,"USD","NonRefundable", false),
                new BudgetNestsRoomDto("BN-NYC-SUI","BudgetNests","NYC","suite",350m,"USD","Flexible up to 24h before check-in", true)
            };

            var lax = new List<BudgetNestsRoomDto>
            {
                new BudgetNestsRoomDto("BN-LAX-STD","BudgetNests","LAX","standard",70m,"USD","Flexible up to 24h before check-in", true),
                new BudgetNestsRoomDto("BN-LAX-DLX","BudgetNests","LAX","deluxe",180m,"USD","NonRefundable", false)
            };

            var lon = new List<BudgetNestsRoomDto>
            {
                new BudgetNestsRoomDto("BN-LON-STD","BudgetNests","LON","standard",120m,"GBP","Flexible up to 24h before check-in", true),
                new BudgetNestsRoomDto("BN-LON-DLX","BudgetNests","LON","deluxe",0m,"GBP","NonRefundable", false)
            };

            var tyo = new List<BudgetNestsRoomDto>
            {
                new BudgetNestsRoomDto("BN-TYO-STD","BudgetNests","TYO","standard",9000m,"JPY","Flexible up to 24h before check-in", true),
                new BudgetNestsRoomDto("BN-TYO-DLX","BudgetNests","TYO","deluxe",0m,"JPY","NonRefundable", false)
            };

            var par = new List<BudgetNestsRoomDto>
            {
                new BudgetNestsRoomDto("BN-PAR-STD","BudgetNests","PAR","standard",95m,"EUR","Flexible up to 24h before check-in", true),
                new BudgetNestsRoomDto("BN-PAR-DLX","BudgetNests","PAR","deluxe",190m,"EUR","NonRefundable", false)
            };

            return new Dictionary<string, List<BudgetNestsRoomDto>>(StringComparer.OrdinalIgnoreCase)
            {
                ["NYC"] = nyc,
                ["LAX"] = lax,
                ["LON"] = lon,
                ["TYO"] = tyo,
                ["PAR"] = par
            };
        }

        private sealed record BudgetNestsRoomDto(
            [property: JsonPropertyName("id")] string Id,
            [property: JsonPropertyName("provider")] string Provider,
            [property: JsonPropertyName("destination_code")] string DestinationCode,
            [property: JsonPropertyName("room_type")] string RoomType,
            [property: JsonPropertyName("per_night_rate")] decimal PerNightRate,
            [property: JsonPropertyName("currency")] string Currency,
            [property: JsonPropertyName("cancellation_policy")] string CancellationPolicy,
            [property: JsonPropertyName("available")] bool Available)
        {
            public string? RawJson { get; init; } = null;
        }
    }
}
