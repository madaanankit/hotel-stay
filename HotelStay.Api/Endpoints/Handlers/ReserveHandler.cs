using System.Globalization;
using HotelStay.Api.Models;
using HotelStay.Api.Services;
using HotelStay.Api.Validation;
using Microsoft.Extensions.Logging;

namespace HotelStay.Api.Endpoints.Handlers
{
    public static class ReserveHandler
    {
        public static async Task<IResult> Handle(HttpRequest httpRequest, ReservationStore store, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("ReserveHandler");
            try
            {
                using var reader = new System.IO.StreamReader(httpRequest.Body);
                var bodyText = await reader.ReadToEndAsync();
                logger.LogDebug("Reserve request body: {Body}", bodyText);

                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var bodyObj = System.Text.Json.JsonSerializer.Deserialize<ReserveRequestBody>(bodyText, options);
                if (bodyObj == null)
                    return Results.Problem("Request body is required", statusCode: StatusCodes.Status400BadRequest);

                var guestName = bodyObj.GuestName;
                var destination = bodyObj.Destination;
                var documentTypeRaw = bodyObj.DocumentType;
                var documentNumber = bodyObj.DocumentNumber;
                var provider = bodyObj.Provider;
                var roomType = bodyObj.RoomType;
                var checkInRaw = bodyObj.CheckIn;
                var checkOutRaw = bodyObj.CheckOut;

                if (string.IsNullOrWhiteSpace(guestName) || string.IsNullOrWhiteSpace(destination) || string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(documentNumber) || string.IsNullOrWhiteSpace(documentTypeRaw))
                    return Results.Problem("guestName, destination, provider, documentNumber and documentType are required", statusCode: StatusCodes.Status400BadRequest);

                if (!Enum.TryParse<DocumentType>(documentTypeRaw, true, out var docType))
                    return Results.Problem("documentType is invalid", statusCode: StatusCodes.Status400BadRequest);

                if (string.IsNullOrWhiteSpace(checkInRaw) || string.IsNullOrWhiteSpace(checkOutRaw) || !DateOnly.TryParseExact(checkInRaw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var ci) || !DateOnly.TryParseExact(checkOutRaw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var co))
                    return Results.Problem("checkIn/checkOut must be provided in yyyy-MM-dd format", statusCode: StatusCodes.Status400BadRequest);

                if (!(co > ci))
                    return Results.Problem("checkOut must be after checkIn", statusCode: StatusCodes.Status400BadRequest);

                // Document validation
                var dv = DocumentValidator.Validate(destination, docType);
                if (!dv.IsValid)
                    return Results.Json(new { errors = new[] { dv.Error } }, statusCode: StatusCodes.Status422UnprocessableEntity);

                decimal? ratePerNight = bodyObj.RatePerNight;
                if (ratePerNight == null)
                    return Results.Problem("ratePerNight is required in this stubbed implementation", statusCode: StatusCodes.Status400BadRequest);

                var nights = Math.Max(1, co.DayNumber - ci.DayNumber);
                var currency = string.IsNullOrWhiteSpace(bodyObj.Currency) ? "USD" : bodyObj.Currency;
                var total = new Money(ratePerNight.Value * nights, currency ?? "USD");

                // Use GUID-based short id to avoid collision
                var guid = Guid.NewGuid().ToString("N").ToUpperInvariant();
                var id = "HS-" + guid[..12];

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

                return Results.Json(response, statusCode: StatusCodes.Status201Created);
            }
            catch (System.Text.Json.JsonException jex)
            {
                logger.LogWarning(jex, "Invalid JSON body for reservation");
                return Results.Problem("Invalid JSON body: " + jex.Message, statusCode: StatusCodes.Status400BadRequest);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled reservation exception");
                return Results.Problem("Internal server error: " + ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}
