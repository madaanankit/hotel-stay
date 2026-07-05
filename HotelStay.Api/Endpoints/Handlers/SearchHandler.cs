using System.Globalization;
using HotelStay.Api.Models;
using HotelStay.Api.Services;
using Microsoft.Extensions.Logging;

namespace HotelStay.Api.Endpoints.Handlers
{
    public static class SearchHandler
    {
        public static async Task<IResult> Handle(string? destination, string? checkIn, string? checkOut, string? roomType, HotelSearchService searchService, HttpContext http, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("SearchHandler");
            // Reuse existing logic moved from HotelEndpoints.SearchHandler
            if (string.IsNullOrWhiteSpace(destination))
            {
                return Results.Problem("'destination' is required", statusCode: StatusCodes.Status400BadRequest);
            }

            if (string.IsNullOrWhiteSpace(checkIn) || !DateOnly.TryParseExact(checkIn, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var ci))
                return Results.Problem("'checkIn' is required in yyyy-MM-dd format", statusCode: StatusCodes.Status400BadRequest);

            if (string.IsNullOrWhiteSpace(checkOut) || !DateOnly.TryParseExact(checkOut, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var co))
                return Results.Problem("'checkOut' is required in yyyy-MM-dd format", statusCode: StatusCodes.Status400BadRequest);

            if (!(co > ci))
                return Results.Problem("checkOut must be after checkIn", statusCode: StatusCodes.Status400BadRequest);

            RoomType? rt = null;
            if (!string.IsNullOrWhiteSpace(roomType) && Enum.TryParse<RoomType>(roomType, true, out var parsed))
                rt = parsed;

            var results = await searchService.SearchAsync(destination, ci, co, rt, http.RequestAborted);

            return Results.Json(new { results });
        }
    }
}
