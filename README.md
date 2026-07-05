# HotelStay (API + Static UI)

This repository contains a minimal HotelStay API (ASP.NET Core) and a tiny framework-free static UI (HTML/JS/CSS) that talks to the API. The UI is located in the `hotelstay-ui/` folder and is intentionally build-free so it can be opened in a browser or served via a trivial static server.

Prerequisites
-------------
- .NET 10 SDK (install from https://dotnet.microsoft.com)
- Optional: a static file server for the UI. You can open the UI directly in a browser, but using a simple HTTP server avoids file:// CORS/browser restrictions. Examples:
  - Python 3: `python -m http.server 8000`
  - Node (http-server): `npx http-server -p 8000`

Restore, build, and run the API
-------------------------------
1. From a clean clone, open a terminal in the repository root.
2. Restore and run the API:

   dotnet restore HotelStay.Api
   dotnet run --project HotelStay.Api

   The API listens on http://localhost:5000 by default in this project scaffold. If the process reports a different URL, use that when pointing the UI.

Run the tests
-------------
Run unit tests for the API project:

   dotnet test HotelStay.Tests

Run tests with coverage collection (requires coverlet collector package):

   dotnet test HotelStay.Tests --collect:"XPlat Code Coverage"

After running the above command, the coverage report will be available under the test results folder (use coverage tools or CI to publish a badge).

Run the UI (hotelstay-ui)
-------------------------
The static UI files are located in `hotelstay-ui/`:

   hotelstay-ui/index.html
   hotelstay-ui/app.js
   hotelstay-ui/styles.css

You can open `hotelstay-ui/index.html` directly in a browser, but some browsers block cross-origin requests when the page is served from file://. Recommended: serve the folder with a simple static server and open http://localhost:8000/index.html.

Example (PowerShell, Python):

   cd hotelstay-ui
   python -m http.server 8000
   # Open http://localhost:8000 in your browser

Make sure the API is running at http://localhost:5000. The UI's API base URL is set in `hotelstay-ui/app.js` as:

   const API_BASE = 'http://localhost:5000';

Change that value if your API listens on a different address.

How to demo (quick walkthrough)
--------------------------------
1. Start the API (dotnet run) so it listens on http://localhost:5000.
2. Serve the UI directory and open it in a browser.
3. Sample search
   - Destination: `Tokyo` (or any city)
   - Check-in: choose a date (yyyy-mm-dd)
   - Check-out: choose a later date
   - Room type: Any / Standard / Deluxe / Suite
   - Click Search — results will show provider badge, room type, per-night rate and total price.

4. Sample reservation
   - From a search result click Reserve
   - Fill guest name, choose document type (Passport or National ID) and document number
   - Confirm reservation. On success you'll see a confirmation with a reference/reservation id and total price.

5. Sample validation-failure (client-side)
   - Submit the search form without destination or with check-out on-or-before check-in — client shows validation error before calling API.

6. Sample validation-failure (server-side)
   - If you bypass client checks and POST an invalid body to the API reserve endpoint you will receive a 400 (bad request) or 422 (document validation) JSON response.

API endpoints and curl examples
------------------------------
- Search hotels (GET):

  curl "http://localhost:5000/hotels/search?destination=Paris&checkIn=2026-07-01&checkOut=2026-07-03"

- Reserve (POST):

  curl -X POST http://localhost:5000/hotels/reserve \\
	-H "Content-Type: application/json" \\
	-d '{"guestName":"Alice","destination":"Paris","documentType":"Passport","documentNumber":"X123","provider":"Demo","roomType":"Deluxe","checkIn":"2026-07-01","checkOut":"2026-07-03","ratePerNight":100}'

- Get reservation (GET):

  curl http://localhost:5000/hotels/reservation/{reference}

Swagger / OpenAPI
------------------
This scaffold disables Swagger by default. You can enable it locally by adding Swashbuckle and wiring UseSwagger / UseSwaggerUI in Program.cs if desired.

Assumptions and scope
---------------------
- No persistent storage: reservations are stored in-memory in a simple ReservationStore for the lifetime of the process.
- No authentication or authorization.
- Providers (PremierStays, BudgetNests) are stubbed, in-memory implementations used for demo/testing.
- The UI is intentionally minimal and framework-free; it's designed for local development and demos only.

If you need a tighter CORS policy (restrict origins) or environment-specific settings, update `Program.cs` in `HotelStay.Api` accordingly.

License / attribution
---------------------
This repository is a learning/demo scaffold.
