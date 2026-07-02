# HotelStay — Specification

This document is the single source of truth for the Hotel Availability feature scaffolding. It defines scope, the unified domain model, provider contracts, stub payload shapes, API surface and validation rules. This repository is an offline scaffold: no external provider credentials, no persistence, and no authentication are included in the initial implementation.

Overview & scope
- Offline-only: provider interactions are implemented as in-repo stubs (JSON + adapters). No external network calls will be made in the initial implementation.
- No auth: API surface is unauthenticated for development / testing purposes.
- No persistence: reservations produced by the Reserve endpoint are returned in-memory for the lifetime of the process only. The persistence contract is out-of-scope for this phase.
- Goal: provide a stable, testable normalization layer so the search/reserve flow is provider-agnostic.

1) Domain model (C#-like pseudocode)

// Room category used across all providers
public enum RoomType
{
	Standard,
	Deluxe,
	Suite,
	Unknown
}

// Monetary value
public record Money(decimal Amount, string Currency);

// Cancellation policy normalized
public class CancellationPolicy
{
	public enum PolicyKind { FreeCancellation, NonRefundable, Unknown }
	public PolicyKind Kind { get; init; }
	// If Kind == FreeCancellation, how long before check-in the free cancellation applies
	public TimeSpan? FreeCancellationCutoff { get; init; }
	public string Description { get; init; } // provider text for display
}

// Normalised search result returned by provider adapters
public class NormalisedRoom
{
	public string Id { get; init; } // provider-scoped id
	public string Provider { get; init; }
	public string DestinationCode { get; init; }
	public RoomType RoomType { get; init; }
	public int? StarRating { get; init; }
	public IReadOnlyList<string>? Amenities { get; init; }
	public IReadOnlyList<Money> PerNightRates { get; init; } // one entry per-night
	public Money RatePerNightDisplay { get; init; } // canonical per-night shown in UI
	public Money TotalPrice { get; init; } // sum across nights
	public CancellationPolicy CancellationPolicy { get; init; }
	public bool Available { get; init; } = true;
	public string? RawProviderPayload { get; init; } // JSON for debugging
	public IReadOnlyDictionary<string,string>? Metadata { get; init; } // tokens required for reserve
}

// Reservation request object
public class ReservationRequest
{
	public string Provider { get; init; }
	public string ProviderResultId { get; init; } // maps to NormalisedRoom.Id
	public DateOnly CheckIn { get; init; }
	public DateOnly CheckOut { get; init; }
	public int Guests { get; init; }
	public GuestInfo PrimaryGuest { get; init; }
	public IReadOnlyList<Document> Documents { get; init; }
	public string? Currency { get; init; } // optional
	public string? IdempotencyKey { get; init; }
}

public record GuestInfo(string Name, string Email, string? Phone);

public class Document
{
	public enum DocumentType { Passport, NationalId, DriverLicense, Other }
	public DocumentType Type { get; init; }
	public string Number { get; init; }
	public string CountryOfIssue { get; init; } // ISO2
	public DateOnly? ExpiryDate { get; init; }
}

// Reservation confirmation returned after Reserve
public class ReservationConfirmation
{
	public bool Success { get; init; }
	public string? ReservationId { get; init; } // internal ref
	public string? ProviderReservationId { get; init; }
	public string Status { get; init; } // Pending | Confirmed | Failed
	public Money? TotalPrice { get; init; }
	public DateTimeOffset? CreatedAt { get; init; }
	public IReadOnlyList<ValidationError>? Errors { get; init; }
	public string? RawProviderResponse { get; init; }
}

// Validation error shape (used for 422 Unprocessable Entity responses)
public class ValidationError
{
	public string Code { get; init; } // machine-readable code
	public string? Field { get; init; } // dotted path to field
	public string Message { get; init; }
	public string? Details { get; init; }
}

2) IHotelProvider interface contract

Design goals
- Adapters convert provider-specific payloads into NormalisedRoom and handle reservation calls.
- Keep the contract async, cancelable, and returning DTOs (no provider exceptions for expected validation errors).

interface IHotelProvider
{
	// Search for availability. Implementations must return NormalisedRoom list. Implementations may mark Available=false
	// for results but the core search flow will filter those out by default.
	Task<IReadOnlyCollection<NormalisedRoom>> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default);

	// Attempt to reserve. ReservationRequest includes Provider and ProviderResultId (from NormalisedRoom.Id).
	// The implementation must validate documents and return a ReservationConfirmation containing validation errors when applicable.
	Task<ReservationConfirmation> ReserveAsync(ReservationRequest request, CancellationToken cancellationToken = default);

	// Optional: get a reservation status from provider (by provider reservation id)
	Task<ReservationConfirmation?> GetReservationAsync(string providerReservationId, CancellationToken cancellationToken = default);
}

public class SearchRequest
{
	public string DestinationCode { get; init; }
	public DateOnly CheckIn { get; init; }
	public DateOnly CheckOut { get; init; }
	public int Guests { get; init; }
	public string? Currency { get; init; }
}

3) Stub provider payload examples

PremierStays (PascalCase JSON)
```json
{
  "Id": "PS-1234",
  "Provider": "PremierStays",
  "DestinationCode": "NYC",
  "RoomType": "Deluxe",
  "StarRating": 5,
  "Amenities": ["Wifi", "Pool", "Gym"],
  "PerNightRate": 150.00,
  "Currency": "USD",
  "Nights": 3,
  "CancellationPolicy": "FreeCancellation up to 48h before check-in",
  "Raw": { /* any extra */ }
}
```

BudgetNests (snake_case JSON)
```json
{
  "id": "BN-abc",
  "provider": "BudgetNests",
  "destination_code": "NYC",
  "room_type": "Standard",
  "per_night_rate": 75.00,
  "currency": "USD",
  "nights": 3,
  "policy": "Flexible up to 24h before check-in",
  "available": true
}
```

Notes
- PremierStays returns richer detail (amenities, star rating) and uses PascalCase JSON.
- BudgetNests returns minimal data and uses snake_case JSON; it may include available=false in some responses — these must be filtered by the normaliser.

4) Mapping table (provider -> NormalisedRoom)

| Normalised field | PremierStays (raw) | BudgetNests (raw) |
|---|---:|---|
| Id | Id | id |
| Provider | Provider | provider |
| DestinationCode | DestinationCode | destination_code |
| RoomType | RoomType (Standard/Deluxe/Suite) | room_type (Standard/Deluxe/Suite) |
| StarRating | StarRating | (absent) -> null |
| Amenities | Amenities | (absent) -> null |
| PerNightRates | PerNightRate + nights -> list of Money | per_night_rate + nights -> list of Money |
| RatePerNightDisplay | PerNightRate | per_night_rate |
| TotalPrice | PerNightRate * Nights | per_night_rate * nights |
| CancellationPolicy | CancellationPolicy text -> parsed into PolicyKind + cutoff (48h) | policy text -> parsed into PolicyKind + cutoff (24h) |
| Available | (assume true) | available (if present) -> use value |
| RawProviderPayload | full JSON as string | full JSON as string |

Mapping rules
- RoomType values map directly. Unknown or missing values map to RoomType.Unknown and should produce a validation warning when encountered.
- CancellationPolicy textual values are parsed into CancellationPolicy.Kind and FreeCancellationCutoff: PremierStays "FreeCancellation up to 48h" -> FreeCancellation + 48h; BudgetNests "Flexible up to 24h" -> FreeCancellation + 24h. Any non-matching text -> NonRefundable or Unknown depending on the presence of "NonRefundable" token.

5) Document validation rules

Cities and rules
- Domestic (examples): New York (NYC), Los Angeles (LAX)
- International (examples): London (LON), Tokyo (TYO), Paris (PAR)

Document acceptance rules
- Domestic city reservations accept: DriverLicense OR NationalId OR Passport.
- International city reservations require Passport. NationalId and DriverLicense may be accepted only when explicitly allowed by provider rules (none of the current stubs allow this — they require Passport).

Validation must occur both client-side (UI) and server-side (API). Client-side validation improves UX; server-side validation is authoritative.

Server-side validation checks include:
- Document presence: at least one document provided when required by provider/destination.
- Expiry: ExpiryDate must be in the future.
- Country-of-issue: For international destinations, CountryOfIssue must be present and consistent with Passport rules (no country mismatch rule enforced unless provider requires it).
- Document format: basic length/character checks (enough to detect obviously malformed numbers).

6) API contract

All endpoints return application/json. Dates in ISO-8601; check-in/check-out use yyyy-MM-dd.

GET /hotels/search
Query parameters:
- destination (string, required)
- checkIn (date, required, yyyy-MM-dd)
- checkOut (date, required, yyyy-MM-dd)
- guests (int, required)
- currency (string, optional)

Success (200) response:
Content-Type: application/json
Body: { "results": NormalisedRoom[] }

400 Bad Request
- Missing or invalid parameters (e.g., checkOut <= checkIn) -> return 400 with a ValidationError[] describing parameter errors.

422 Unprocessable Entity
- Returned when all providers returned no results for the destination, or request semantically invalid for the providers (e.g., unsupported currency). Body: { "errors": ValidationError[] }

POST /hotels/reserve
Request body: ReservationRequest as JSON

Responses
- 201 Created
  - Body: ReservationConfirmation (Success=true)
- 400 Bad Request
  - Malformed JSON or missing required fields -> return 400 with ValidationError[] for request schema errors
- 422 Unprocessable Entity
  - Validation errors (document expired, missing document, invalid dates, room unavailable, price mismatch). Response body: { "errors": [ ValidationError ] }
- 409 Conflict
  - Idempotency conflict if IdempotencyKey indicates a completed different reservation (implementation choice; initially may not be used).

GET /hotels/reservation/{reference}
- Path param: reference (internal ReservationId)

Responses
- 200 OK -> ReservationConfirmation
- 404 Not Found -> { "errors": [ { code: "NotFound", message: "Reservation not found" } ] }

ValidationError (422) example
```json
{
  "errors": [
	{ "code": "DocumentExpired", "field": "Documents[0].ExpiryDate", "message": "Passport expired on 2023-01-01" },
	{ "code": "InvalidDates", "field": "CheckOut", "message": "Check-out must be after check-in" }
  ]
}
```

7) Extensibility note — adding a third provider (example: CityStay)

Files/areas to change
- Add a new adapter class implementing IHotelProvider: e.g., CityStayProvider : IHotelProvider. This class is responsible for parsing CityStay's raw payloads and mapping to NormalisedRoom and ReservationConfirmation.
- Add CityStay stub JSON files under /stubs/CityStay/ (search + reserve responses) and unit tests exercising mapping and boundary cases.
- Register the new provider in DI (Startup / Program.cs) with a provider key if a provider factory is used.

Files/areas NOT to change
- Core search/reserve controllers and DTOs (SearchRequest, NormalisedRoom, ReservationRequest, ReservationConfirmation) should not need modification.
- Existing provider adapters (PremierStays, BudgetNests) remain untouched.

Exactly what changes are required to add CityStay
1. Create CityStayProvider : IHotelProvider and implement SearchAsync / ReserveAsync parsing logic.
2. Add stub JSON payloads and unit/integration tests for CityStay.
3. Register CityStayProvider with DI and provide configuration (baseUrl or stub path) via options.

This separation ensures the normalization surface is stable: adding providers is limited to adapter code + registration.

Appendix: test stubs & edge cases
- Include stub files for:
  - PremierStays: normal, NonRefundable, FreeCancellation boundary at 48h, varying per-night rates, missing RoomType
  - BudgetNests: normal available=true, available=false (filtering), Flexible (24h), NonRefundable, varying per-night rates
  - Price-drift scenario: provider returns a different total on reserve than the search response

End of specification.
