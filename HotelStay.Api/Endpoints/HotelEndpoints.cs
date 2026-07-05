using System;
using System.Globalization;
using HotelStay.Api.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using System.IO;
using HotelStay.Api.Services;
using HotelStay.Api.Validation;
using Microsoft.AspNetCore.Mvc;

namespace HotelStay.Api.Endpoints
{
    public static class HotelEndpoints
    {
        // Replace ad-hoc file logging with ILogger via DI in handler implementations.
        // The old file-based logger (endpoint.log) has been removed.

        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private static IResult CreateJsonResult(object value, int statusCode = StatusCodes.Status200OK)
        {
            return new RawJsonResult(value, statusCode, _jsonOptions);
        }

        private static IResult CreateProblemResult(string title, int statusCode)
        {
            return new RawJsonResult(new ProblemDetails { Title = title, Status = statusCode }, statusCode, _jsonOptions, "application/problem+json");
        }

        private sealed class RawJsonResult : IResult
        {
            private readonly object _value;
            private readonly int _statusCode;
            private readonly JsonSerializerOptions _options;
            private readonly string _contentType;

            public RawJsonResult(object value, int statusCode, JsonSerializerOptions options, string contentType = "application/json")
            {
                _value = value ?? new { };
                _statusCode = statusCode;
                _options = options;
                _contentType = contentType;
            }

            public async Task ExecuteAsync(HttpContext httpContext)
            {
                var json = JsonSerializer.Serialize(_value, _options);
                var resp = httpContext.Response;
                resp.ContentType = _contentType;
                resp.StatusCode = _statusCode;
                await resp.WriteAsync(json);
            }
        }

        public static void MapHotelEndpoints(this WebApplication app)
        {
            // Thin endpoint mapping: handlers will be refactored into dedicated classes.
            app.MapGet("/hotels/search", (Func<string?, string?, string?, string?, HotelSearchService, HttpContext, ILoggerFactory, Task<IResult>>) HotelStay.Api.Endpoints.Handlers.SearchHandler.Handle)
                .WithName("SearchHotels")
                .Produces(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest);

            app.MapPost("/hotels/reserve", (Func<HttpRequest, ReservationStore, ILoggerFactory, Task<IResult>>) HotelStay.Api.Endpoints.Handlers.ReserveHandler.Handle)
                .WithName("ReserveHotel")
                .Produces(StatusCodes.Status201Created)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .Produces(StatusCodes.Status422UnprocessableEntity);

            app.MapGet("/hotels/reservation/{reference}", (Func<string, ReservationStore, HttpContext, ILoggerFactory, Task<IResult>>)HotelStay.Api.Endpoints.Handlers.GetReservationHandler.Handle)
                .WithName("GetReservation")
                .Produces(StatusCodes.Status200OK)
                .ProducesProblem(StatusCodes.Status400BadRequest)
                .ProducesProblem(StatusCodes.Status404NotFound);
        }

        private static async Task<IResult> SearchHandler([FromQuery] string? destination, [FromQuery] string? checkIn, [FromQuery] string? checkOut, [FromQuery] string? roomType, [FromServices] HotelSearchService searchService, HttpContext http)
        {
            // Validate required parameters
            if (string.IsNullOrWhiteSpace(destination))
                return CreateProblemResult("'destination' is required", StatusCodes.Status400BadRequest);

            if (string.IsNullOrWhiteSpace(checkIn) || !DateOnly.TryParseExact(checkIn, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var ci))
                return CreateProblemResult("'checkIn' is required in yyyy-MM-dd format", StatusCodes.Status400BadRequest);

            if (string.IsNullOrWhiteSpace(checkOut) || !DateOnly.TryParseExact(checkOut, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var co))
                return CreateProblemResult("'checkOut' is required in yyyy-MM-dd format", StatusCodes.Status400BadRequest);

            if (!(co > ci))
                return CreateProblemResult("checkOut must be after checkIn", StatusCodes.Status400BadRequest);

            RoomType? rt = null;
            if (!string.IsNullOrWhiteSpace(roomType) && Enum.TryParse<RoomType>(roomType, true, out var parsed))
                rt = parsed;

            var results = await searchService.SearchAsync(destination, ci, co, rt, http.RequestAborted);

            return CreateJsonResult(new { results }, StatusCodes.Status200OK);
        }

        // Old handlers removed - replaced by handler classes in Handlers/ folder
        // ReserveHandler, SearchHandler, GetReservationHandler are now in separate files
        // and registered via MapHotelEndpoints with ILoggerFactory dependency injection.
    }
}
