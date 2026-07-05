# Work Completion Summary & Remaining Tasks

## Overview
This document summarizes work completed in this session and clearly lists remaining tasks to reach a ≥4.0 overall score.

---

## Completed Work (9 Commits)

### Phase 0: Quick Triage & Safety — ✅ COMPLETE

| # | Commit Hash | Task | Status |
|---|---|---|---|
| 1 | `3e1ecc8` | Restrict CORS to dev-only via appsettings.Development.json | ✅ Done |
| 2 | `d225d15` | Remove file-based logging; prepare endpoints for handler refactor | ✅ Done |
| 3 | `eec4d38` | Accept numeric RoomType in ReserveRequestBody | ✅ Done |
| 4 | `bfbdb39` | Refactor endpoints into handler classes; replace fragile reservation ID with GUID-based short ID | ✅ Done |
| 5 | `540ef40` | Minor cleanup: ReservationStore touch | ✅ Done |
| 6 | `a6f7e01` | Add coverlet.collector package for test coverage | ✅ Done |
| 7 | `1497181` | Document test/coverage instructions in README | ✅ Done |
| 8 | `1a49c2a` | Explicitly name GitHub Copilot in reflection.md & prompts.md | ✅ Done |
| 9 | `d1e5e70` | Remove all Console.Error.WriteLine; replace with ILogger | ✅ Done |
| 10 | `398dbf3` | Update PHASED_IMPROVEMENT_PLAN.md with status & next actions | ✅ Done |

### What Was Fixed

#### CORS Security
- Program.cs now reads `Cors:AllowedOrigins` from configuration (appsettings.Development.json)
- LocalDevCors policy applied **only in Development** environment
- Non-dev environments have no CORS policy by default
- Addresses evaluation finding: "Wide-open CORS (AllowAnyOrigin)"

#### Logging & Code Quality
- Removed all `Console.Error.WriteLine` statements (6+ instances)
- Replaced with structured `ILoggerFactory` in middleware and handlers
- Removed hardcoded `endpoint.log` file logging
- Addresses evaluation finding: "Debug Console.Error statements, file-system logging baked in"

#### Endpoint Architecture
- Extracted `SearchHandler`, `ReserveHandler`, `GetReservationHandler` into separate handler classes
- Each handler uses `ILoggerFactory` for structured logging
- Removed god-class complexity from `HotelEndpoints.cs`
- Addresses evaluation finding: "Massive God-class endpoint, 300+ lines"

#### Reservation ID Generation
- Replaced fragile Base64-ticks approach with GUID-based short ID: `HS-{Guid:N}[..12]`
- No longer susceptible to collisions under concurrent requests in same tick
- Addresses evaluation finding: "Weak reservation ID generation; collides under concurrent requests"

#### JSON Deserialization
- Changed `ReserveRequestBody.RoomType` from string to `int?` to accept numeric enum values
- Prevents JSON deserialization errors when UI sends numeric room types
- Addresses evaluation finding: "Enum-vs-string serialization bug"

#### Test Coverage
- Added `coverlet.collector` (v6.0.0) to project for code coverage collection
- Updated README with instructions: `dotnet test --collect:"XPlat Code Coverage"`
- Enables downstream CI/CD coverage threshold enforcement
- Prepares for Phase 3 coverage goals

#### AI Transparency
- Updated `prompts.md` to explicitly name "GitHub Copilot (GitHub Copilot Chat)"
- Updated `reflection.md` to detail AI tool usage, design-phase prompts, and areas requiring manual correction
- Addresses evaluation finding: "AI tool not named; reflection is somewhat generic"

---

## Remaining High-Priority Tasks (for ≥4.0 score)

### Phase 1: Critical Functional Bugs — ⏳ IN PROGRESS (Est. 8–12 hours)

**Why this is highest priority:** The evaluation identified these as breaking the abstraction and preventing real stub providers from being exercised through the UI.

#### 1.1 Create CityRegistry POCO & IOptions wiring (Est. 3–4 hours)
**Problem:** UI sends display names (`"New York"`, `"London"`); API expects codes (`"NYC"`, `"LON"`). Mismatch causes all searches to fall back to demo data.

**Solution:**
```csharp
// Create Models/CityRegistry.cs
public sealed record CityInfo
{
	public required string Code { get; init; }
	public required string DisplayName { get; init; }
	public required bool IsDomestic { get; init; }
}

public sealed record CityRegistry
{
	public required IReadOnlyList<CityInfo> Cities { get; init; }
}
```

Add to `appsettings.json`:
```json
{
  "CityRegistry": {
	"Cities": [
	  { "Code": "NYC", "DisplayName": "New York", "IsDomestic": true },
	  { "Code": "LAX", "DisplayName": "Los Angeles", "IsDomestic": true },
	  { "Code": "LON", "DisplayName": "London", "IsDomestic": false },
	  { "Code": "PAR", "DisplayName": "Paris", "IsDomestic": false },
	  { "Code": "TYO", "DisplayName": "Tokyo", "IsDomestic": false },
	  { "Code": "SYD", "DisplayName": "Sydney", "IsDomestic": false }
	]
  }
}
```

Wire in `Program.cs`:
```csharp
builder.Services.Configure<CityRegistry>(builder.Configuration.GetSection("CityRegistry"));
```

**Files to modify/create:**
- Create: `HotelStay.Api/Models/CityRegistry.cs`
- Modify: `appsettings.json`
- Modify: `appsettings.Development.json`
- Modify: `Program.cs`

**Test:** Search with UI city names; verify API receives codes and provider stubs return results.

---

#### 1.2 Update UI to use city codes (Est. 1–2 hours)
**Problem:** Frontend `knownCities` array uses display names; should use codes.

**Solution:**
- Change `hotelstay-ui/app.js` `knownCities` from display names to codes
- OR: Create a mapping object in JS and display names while sending codes to API

**Files to modify:**
- `hotelstay-ui/app.js`

**Test:** UI dropdown still shows friendly names; API searches use codes.

---

#### 1.3 Create IDocumentValidator interface & wire to CityRegistry (Est. 2 hours)
**Problem:** Static `DocumentValidator.Validate()` doesn't use CityRegistry; always treats cities as "unknown" and forces Passport.

**Solution:**
```csharp
// Create Services/IDocumentValidator.cs
public interface IDocumentValidator
{
	ValidationResult Validate(string destinationCode, DocumentType documentType);
}

// Modify Validation/DocumentValidator.cs
public class DocumentValidator : IDocumentValidator
{
	private readonly IOptions<CityRegistry> _cityRegistry;

	public DocumentValidator(IOptions<CityRegistry> cityRegistry)
	{
		_cityRegistry = cityRegistry;
	}

	public ValidationResult Validate(string destinationCode, DocumentType documentType)
	{
		var city = _cityRegistry.Value.Cities.FirstOrDefault(c => c.Code == destinationCode);
		if (city == null)
			return new ValidationResult { IsValid = false, Error = "Unknown destination code" };

		var requiresPassport = !city.IsDomestic;
		if (requiresPassport && documentType != DocumentType.Passport)
			return new ValidationResult { IsValid = false, Error = "International destinations require Passport" };

		return new ValidationResult { IsValid = true };
	}
}
```

Wire in `Program.cs`:
```csharp
builder.Services.AddScoped<IDocumentValidator, DocumentValidator>();
```

Update handlers to inject and use `IDocumentValidator` instead of static calls.

**Files to modify/create:**
- Create: `HotelStay.Api/Services/IDocumentValidator.cs`
- Modify: `HotelStay.Api/Validation/DocumentValidator.cs`
- Modify: `Program.cs`
- Modify: `HotelStay.Api/Endpoints/Handlers/ReserveHandler.cs` to inject `IDocumentValidator`

**Test:** Verify document validation now uses city codes and domestic/international rules work correctly.

---

#### 1.4 Add ReserveAsync to IHotelProvider (Est. 2–3 hours)
**Problem:** `IHotelProvider` only has `SearchAsync`; reservation logic is hardcoded in the endpoint. Adding a new provider requires endpoint changes (violates OCP).

**Solution:**
```csharp
// Modify IHotelProvider
public interface IHotelProvider
{
	Task<IReadOnlyList<NormalisedRoom>> SearchAsync(
		string destination,
		DateOnly checkIn,
		DateOnly checkOut,
		RoomType? roomType = null,
		CancellationToken cancellationToken = default);

	// NEW:
	Task<ReservationConfirmation> ReserveAsync(
		ReservationRequest request,
		CancellationToken cancellationToken = default);
}

// In PremierStaysProvider & BudgetNestsProvider, implement:
public async Task<ReservationConfirmation> ReserveAsync(ReservationRequest request, CancellationToken cancellationToken)
{
	// Validate document per provider rules (or use injected IDocumentValidator)
	// Return confirmation with provider-specific ID or status
	return new ReservationConfirmation
	{
		Success = true,
		ProviderReservationId = $"{ProviderName}-{Guid.NewGuid():N}[..8]",
		Status = "Confirmed",
		CreatedAt = DateTimeOffset.UtcNow,
		// ... populate other fields
	};
}
```

**Files to modify:**
- `HotelStay.Api/Providers/IHotelProvider.cs` — add ReserveAsync signature
- `HotelStay.Api/Providers/PremierStays/PremierStaysProvider.cs` — implement ReserveAsync
- `HotelStay.Api/Providers/BudgetNests/BudgetNestsProvider.cs` — implement ReserveAsync

**Test:** Unit test each provider's ReserveAsync; verify signatures match.

---

#### 1.5 Create IReservationService & wire provider ReserveAsync (Est. 2–3 hours)
**Problem:** Endpoint directly creates and stores reservations. Should delegate to service that calls providers.

**Solution:**
```csharp
// Create Services/IReservationService.cs
public interface IReservationService
{
	Task<ReservationConfirmation> ReserveAsync(
		string provider,
		ReservationRequest request,
		CancellationToken cancellationToken = default);
}

// Create Services/ReservationService.cs
public class ReservationService : IReservationService
{
	private readonly IEnumerable<IHotelProvider> _providers;
	private readonly ReservationStore _store;

	public ReservationService(IEnumerable<IHotelProvider> providers, ReservationStore store)
	{
		_providers = providers;
		_store = store;
	}

	public async Task<ReservationConfirmation> ReserveAsync(
		string provider,
		ReservationRequest request,
		CancellationToken cancellationToken = default)
	{
		var matchingProvider = _providers.FirstOrDefault(p => p.GetType().Name.Contains(provider));
		if (matchingProvider == null)
			throw new InvalidOperationException($"Provider '{provider}' not found");

		var confirmation = await matchingProvider.ReserveAsync(request, cancellationToken);

		// Generate our own reference ID
		confirmation.ReservationId = "HS-" + Guid.NewGuid().ToString("N")[..12];

		// Store and return
		_store.TryAdd(confirmation.ReservationId, confirmation);
		return confirmation;
	}
}
```

Wire in `Program.cs`:
```csharp
builder.Services.AddScoped<IReservationService, ReservationService>();
```

Update `ReserveHandler` to inject and use `IReservationService`.

**Files to modify/create:**
- Create: `HotelStay.Api/Services/IReservationService.cs`
- Create: `HotelStay.Api/Services/ReservationService.cs`
- Modify: `Program.cs` — register IReservationService
- Modify: `HotelStay.Api/Endpoints/Handlers/ReserveHandler.cs` — inject IReservationService, call instead of inline logic

**Test:** Integration test that verifies ReserveHandler calls IReservationService and returns provider confirmation.

---

#### 1.6 Remove demo fallback from HotelSearchService (Est. 0.5 hours)
**Problem:** `HotelSearchService.SearchAsync()` returns demo data when providers return empty. Masks the city-code bug.

**Solution:**
Remove or comment out the demo fallback logic. Let empty result be empty.

**Files to modify:**
- `HotelStay.Api/Services/HotelSearchService.cs`

**Test:** Search with valid city code and valid dates; if no rooms match, verify empty list (not demo data) is returned.

---

### Phase 3: Testing & CI — ⏳ TODO (Est. 6–10 hours)

Once Phase 1 is complete, add tests and CI.

#### 3.1 Add unit tests for validation edge cases (Est. 3–4 hours)
- `checkOut <= checkIn` validation
- Missing required fields (destination, checkIn, checkOut)
- Invalid RoomType enum value
- Document type validation per destination (passport vs national ID)
- Available:false rooms filtered from results
- Concurrent reservation ID uniqueness

**Files to create/modify:**
- `HotelStay.Tests/SearchEndpointTests.cs` — add edge case tests
- `HotelStay.Tests/ReservationEndpointTests.cs` — add validation tests
- `HotelStay.Tests/DocumentValidatorTests.cs` — add city-based validation tests

---

#### 3.2 Add integration tests for city-code flow (Est. 2–3 hours)
- Search with UI city name (display name) → verify API receives code → verify provider stubs invoked
- Reservation with provider → verify ReserveAsync called → verify confirmation stored with correct reference ID

**Files to create/modify:**
- `HotelStay.Tests/SearchEndpointTests.cs` — add city-code flow test
- `HotelStay.Tests/ReservationEndpointTests.cs` — add ReserveAsync invocation test

---

#### 3.3 Add GitHub Actions workflow for CI/CD (Est. 2 hours)
Create `.github/workflows/dotnet.yml`:
```yaml
name: .NET Build & Test

on: [push, pull_request]

jobs:
  build-and-test:
	runs-on: ubuntu-latest
	steps:
	  - uses: actions/checkout@v3
	  - uses: actions/setup-dotnet@v3
		with:
		  dotnet-version: '10.0.x'
	  - run: dotnet restore
	  - run: dotnet build --no-restore
	  - run: dotnet test --no-build --collect:"XPlat Code Coverage" --logger "trx;LogFileName=test-results.trx"
	  - name: Check Coverage Threshold
		run: |
		  # Parse coverage report and enforce 70% threshold
		  # (implementation depends on coverage format)
```

**Files to create:**
- `.github/workflows/dotnet.yml`

---

### Phase 4: Production Readiness — ⏳ TODO (Est. 4–6 hours)

#### 4.1 Add Health Checks (Est. 1–2 hours)
Wire `Microsoft.AspNetCore.Diagnostics.HealthChecks`:
```csharp
builder.Services.AddHealthChecks()
	.AddCheck("live", () => HealthCheckResult.Healthy(), tags: new[] { "live" });

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
	Predicate = reg => reg.Tags.Contains("live")
});
```

**Files to modify:**
- `Program.cs`

---

#### 4.2 Add Structured Logging with Correlation IDs (Est. 1–2 hours)
Add middleware to inject correlation ID into all logs.

**Files to modify:**
- `Program.cs` — add middleware

---

#### 4.3 Parameterize Configuration (Est. 1 hour)
Move all hard-coded values to `appsettings.json` (already partially done for CORS and CityRegistry).

**Files to modify:**
- `appsettings.json`, `appsettings.Development.json`

---

### Phase 5: Documentation — ⏳ TODO (Est. 2–3 hours)

#### 5.1 Create CONTRIBUTING.md (Est. 1 hour)
Document branch strategy, commit guidelines, and PR process.

**Files to create:**
- `CONTRIBUTING.md`

---

#### 5.2 Create ARCHITECTURE.md (Est. 1–1.5 hours)
Document provider pattern, DI structure, handler flow, design trade-offs.

**Files to create:**
- `ARCHITECTURE.md`

---

#### 5.3 Polish README (Est. 0.5 hour)
Add overview section and link to ARCHITECTURE.md.

**Files to modify:**
- `README.md`

---

## Summary: Effort & Impact

| Phase | Status | Est. Hours | High-Impact | Score Impact |
|---|---|---|---|---|
| **Phase 0** | ✅ Done | 0 | - | +0.5 |
| **Phase 1** | ⏳ TODO | 10–12 | ⭐⭐⭐ Critical | +0.8–1.0 |
| **Phase 3** | ⏳ TODO | 6–10 | ⭐⭐ Important | +0.4–0.6 |
| **Phase 4** | ⏳ TODO | 4–6 | ⭐⭐ Important | +0.3–0.5 |
| **Phase 5** | ⏳ TODO | 2–3 | ⭐ Nice-to-have | +0.2–0.3 |
| **Phase 6** | 📋 Optional | 4–12 | ⭐ Bonus | +0.5–1.0 |
| **TOTAL** | - | 26–41 hours | - | **+2.2–3.4** (goal: 4.0+) |

---

## Next Immediate Step

**Recommended:** Start Phase 1.1 (CityRegistry) immediately, as it unblocks all other Phase 1 tasks and is the root cause of the biggest functional bug.

Would you like me to proceed with Phase 1.1 now, or should you review the plan first?
