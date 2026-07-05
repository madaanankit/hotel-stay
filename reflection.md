Reflection on the HotelStay case study

This file documents what I would change or improve given more time, and where I relied on AI assistance.

- Project summary
  - Delivered a runnable API and a minimal framework-free static UI. The system is suitable for local demos and tests but is not production-ready.

- Where I would invest more time
  1. Move the UI to a modern front-end framework (React / Angular):
	 - Rationale: component-based frameworks improve maintainability, make complex UI state easier to manage, and provide mature ecosystems for testing, accessibility, and internationalization.
	 - Impact: easier development of form validation, rich client-side routing, and automated E2E testing with Playwright or Cypress.

  2. Add OpenAPI/Swagger documentation for the API:
	 - Rationale: a machine-readable contract simplifies client development, enables interactive testing, and supports automatic client SDK generation.
	 - Impact: faster integration testing, fewer contract mismatches between UI and API, and clearer API surface for reviewers.

  3. Add a persistent database for reservations:
	 - Rationale: the current in-memory ReservationStore is ephemeral and unsuitable for production.
	 - Impact: use a lightweight SQL store (SQLite for demos, PostgreSQL for production) with migrations, ensuring reservations survive process restarts and enabling real integration tests.

- AI assistance and verification
  - I used GitHub Copilot (GitHub Copilot Chat) integrated in Visual Studio / Cursor to scaffold the static UI and help with iterative fixes. AI accelerated development but introduced a few assumptions (payload shapes, enum vs string) that required manual correction and additional tests.
  - I validated AI outputs using unit and integration tests and by running the app end-to-end. Areas where AI required manual correction (JSON serialization, ID generation, CORS policy) are documented in commit messages and code comments.
  - Design-phase prompts were used to inform architecture decisions and identify gaps in test coverage and production readiness.

- Final notes
  - The project is a functional prototype and a good foundation. With the three investments above (modern UI framework, OpenAPI, persistent storage) plus improved testing and observability, the solution would be ready for early staging and wider evaluation.
