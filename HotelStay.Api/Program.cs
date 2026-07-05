using System.Globalization;
using HotelStay.Api.Models;
using HotelStay.Api.Providers;
using HotelStay.Api.Providers.PremierStays;
using HotelStay.Api.Providers.BudgetNests;
using HotelStay.Api.Services;
using HotelStay.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// Register provider implementations.
// These providers are stateless, in-memory stubs. Singleton lifetime is appropriate because:
// - There is no per-request state stored on the provider instances.
// - Reusing a single instance reduces allocations and simplifies deterministic behavior in tests.
// If a real provider had scoped resources (e.g. per-request tokens), consider Scoped or Transient.
builder.Services.AddSingleton<IHotelProvider, PremierStaysProvider>();
builder.Services.AddSingleton<IHotelProvider, BudgetNestsProvider>();

builder.Services.AddEndpointsApiExplorer();

// Enable CORS so the plain static UI (served from file:// or a simple static server)
// can call the API during development. This policy allows any origin, method, and header.
// It's intentionally permissive for local development; tighten in production as needed.
// Configure CORS from configuration. In Development we enable a permissive policy
// controlled via appsettings.Development.json. In non-development environments no CORS policy is applied.
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:8000" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDevCors", policy => policy.WithOrigins(corsOrigins).AllowAnyMethod().AllowAnyHeader());
});

// Register HotelSearchService which orchestrates querying providers and aggregation.
builder.Services.AddSingleton<HotelSearchService>();
builder.Services.AddSingleton<ReservationStore>();

var app = builder.Build();

// Global exception logging middleware to ensure test host captures exceptions.
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        // Log the exception for test output but re-throw so the host's exception handling can decide how to render it.
        Console.Error.WriteLine("Unhandled middleware exception: " + ex.ToString());
        throw;
    }
});

// Swagger is intentionally disabled in this scaffold to avoid runtime reflection issues
// when running under test hosts. Enable in local development by adding Swashbuckle and
// calling UseSwagger/UseSwaggerUI here if desired.

// Map endpoints defined in a dedicated file to keep Program.cs thin.
// Ensure routing is established before applying CORS and mapping endpoints
app.UseRouting();

// Apply CORS policy only in Development to avoid wide-open CORS in non-dev environments
if (app.Environment.IsDevelopment())
{
    app.UseCors("LocalDevCors");
}

app.MapHotelEndpoints();

app.Run();

// Expose Program type for functional tests (WebApplicationFactory)
public partial class Program { }
