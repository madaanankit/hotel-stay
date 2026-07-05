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

        private static async Task<IResult> ReserveHandler(HttpRequest httpRequest, [FromServices] ReservationStore store)
        {
            try
            {
                Console.Error.WriteLine("POST /hotels/reserve invoked");
                using var reader = new System.IO.StreamReader(httpRequest.Body);
                var bodyText = await reader.ReadToEndAsync();
                Console.Error.WriteLine("Request body: " + bodyText);
                // Deserialize into ReserveRequestBody using case-insensitive property matching so the client may send PascalCase or camelCase
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var bodyObj = System.Text.Json.JsonSerializer.Deserialize<ReserveRequestBody>(bodyText, options);
                if (bodyObj == null)
                    return CreateProblemResult("Request body is required", StatusCodes.Status400BadRequest);

                var guestName = bodyObj.GuestName;
                var destination = bodyObj.Destination;
                var documentTypeRaw = bodyObj.DocumentType;
                var documentNumber = bodyObj.DocumentNumber;
                var provider = bodyObj.Provider;
                var roomType = bodyObj.RoomType;
                var checkInRaw = bodyObj.CheckIn;
                var checkOutRaw = bodyObj.CheckOut;

                if (string.IsNullOrWhiteSpace(guestName) || string.IsNullOrWhiteSpace(destination) || string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(documentNumber) || string.IsNullOrWhiteSpace(documentTypeRaw))
                    return CreateProblemResult("guestName, destination, provider, documentNumber and documentType are required", StatusCodes.Status400BadRequest);

                if (!Enum.TryParse<DocumentType>(documentTypeRaw, true, out var docType))
                    return CreateProblemResult("documentType is invalid", StatusCodes.Status400BadRequest);

                if (string.IsNullOrWhiteSpace(checkInRaw) || string.IsNullOrWhiteSpace(checkOutRaw) || !DateOnly.TryParseExact(checkInRaw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var ci) || !DateOnly.TryParseExact(checkOutRaw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var co))
                    return CreateProblemResult("checkIn/checkOut must be provided in yyyy-MM-dd format", StatusCodes.Status400BadRequest);

                if (!(co > ci))
                    return CreateProblemResult("checkOut must be after checkIn", StatusCodes.Status400BadRequest);

                // Document validation
                var dv = DocumentValidator.Validate(destination, docType);
                if (!dv.IsValid)
                    return CreateJsonResult(new { errors = new[] { dv.Error } }, StatusCodes.Status422UnprocessableEntity);

                decimal? ratePerNight = bodyObj.RatePerNight;
                if (ratePerNight == null)
                    return CreateProblemResult("ratePerNight is required in this stubbed implementation", StatusCodes.Status400BadRequest);

                var nights = Math.Max(1, co.DayNumber - ci.DayNumber);
                var currency = string.IsNullOrWhiteSpace(bodyObj.Currency) ? "USD" : bodyObj.Currency;
                var total = new Money(ratePerNight.Value * nights, currency ?? "USD");

                var id = "HS-" + Convert.ToBase64String(BitConverter.GetBytes(DateTimeOffset.UtcNow.Ticks)).TrimEnd('=', '\n');

                var confirmation = new ReservationConfirmation
                {
                    Success = true,
                    ReservationId = id,
                    ProviderReservationId = id,
                    Status = "Confirmed",
                    TotalPrice = total,
                    CreatedAt = DateTimeOffset.UtcNow,
                    Errors = null,
                    RawProviderResponse = null
                };

                store.TryAdd(id, confirmation);

                var response = new
                {
                    reservationId = id,
                    provider,
                    roomType,
                    total = confirmation.TotalPrice,
                    cancellationPolicy = "See provider policy",
                    guestName
                };

                return CreateJsonResult(response, StatusCodes.Status201Created);
            }
            catch (System.Text.Json.JsonException jex)
            {
                Console.Error.WriteLine("JsonException: " + jex.ToString());
                return CreateProblemResult("Invalid JSON body: " + jex.Message, StatusCodes.Status400BadRequest);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Unhandled exception: " + ex.ToString());
                return CreateProblemResult("Internal server error: " + ex.Message, StatusCodes.Status500InternalServerError);
            }
        }

        private static Task<IResult> GetReservationHandler(string reference, [FromServices] ReservationStore store, HttpContext http)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(reference))
                    return Task.FromResult(CreateProblemResult("'reference' path parameter is required", StatusCodes.Status400BadRequest));

                if (!store.TryGet(reference, out var confirmation))
                    return Task.FromResult(CreateProblemResult($"Reservation '{reference}' not found", StatusCodes.Status404NotFound));

                return Task.FromResult(CreateJsonResult((object)confirmation!, StatusCodes.Status200OK));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("GetReservation unhandled: " + ex.ToString());
                return Task.FromResult(CreateProblemResult("Internal server error: " + ex.Message, StatusCodes.Status500InternalServerError));
            }
        }
        // LogAsync is implemented above and writes to endpoint.log. No-op fallback removed.
        // Added a no-op LogAsync to ensure build consistency after prior removal.
        private static Task LogAsync(string message)
        {
            // Intentionally does nothing. Kept for compatibility with prior callers.
            return Task.CompletedTask;
        }
    }
}
