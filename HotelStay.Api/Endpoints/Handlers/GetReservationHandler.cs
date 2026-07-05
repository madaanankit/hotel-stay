using HotelStay.Api.Services;
using Microsoft.Extensions.Logging;

namespace HotelStay.Api.Endpoints.Handlers
{
    public static class GetReservationHandler
    {
        public static Task<IResult> Handle(string reference, ReservationStore store, HttpContext http, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("GetReservationHandler");
            try
            {
                if (string.IsNullOrWhiteSpace(reference))
                    return Task.FromResult(Results.Problem("'reference' path parameter is required", statusCode: StatusCodes.Status400BadRequest) as IResult);

                if (!store.TryGet(reference, out var confirmation))
                    return Task.FromResult(Results.Problem($"Reservation '{reference}' not found", statusCode: StatusCodes.Status404NotFound) as IResult);

                return Task.FromResult(Results.Json((object)confirmation!, statusCode: StatusCodes.Status200OK) as IResult);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "GetReservation unhandled");
                return Task.FromResult(Results.Problem("Internal server error: " + ex.Message, statusCode: StatusCodes.Status500InternalServerError) as IResult);
            }
        }
    }
}
