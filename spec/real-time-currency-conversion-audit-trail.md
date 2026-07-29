# Spec

<!-- Business layer + Known Constraints — carried through exactly as given, not written by you -->

**Title:** Real-Time Currency Conversion & Audit Trail

**Business Idea:** Treasury operations teams at our enterprise customers
currently convert currency using manual lookups against a third-party
portal — slow, and it leaves no record for compliance. We need instant,
self-serve conversion inside our own app, and because these are regulated
transactions, every conversion must be reconstructable for an auditor on
demand, not pieced together afterward from emails or spreadsheets.

**Persona:** Treasury operations analysts at enterprise customers, who
process multiple cross-border settlements a day and are personally
accountable if a conversion can't be justified during an audit.

**User Need:** See a trustworthy converted amount the moment I enter it
— and be able to pull up any past conversion, with its rate and timestamp,
the moment an auditor asks for it.

**Known Constraints:**
- Frontend must read `VITE_API_URL` dynamically at container runtime using
  a generic entrypoint script placeholder replacement to allow native
  browser fetch.
- Backend must read database configurations exclusively from runtime
  environment variables.
- CI/CD pipeline must inject placeholders safely at deployment time without
  exposing sensitive data in container layers.
- Restricted to specific stack versions: React (Vite/Node 24.*) and C#
  (.NET 10).
- Must adhere to a strict folder layout (`src/frontend` and `src/backend`).
- Deploy as a single Azure Container App using the sidecar pattern to host
  both frontend and backend.
- Authenticate to Cosmos DB using the pre-assigned User-Assigned Managed
  Identity via `Azure.Identity`.
- Utilize `Microsoft.Azure.Cosmos` and `Azure.Identity` packages for
  secure, token-based database interactions.
- Include `Azure.ResourceManager` and `Azure.ResourceManager.CosmosDB`
  packages to handle programmatic database and container provisioning
  (Required RBAC roles are already in place).
- Implement minimal scaffolding of frontend and backend projects along with any changes required to github workflows and related files, then commit to `main` branch of the repository.
- Create and checkout feature branch for the repository. Full feature requirements should be implemented in the feature branch.
- Utilize environment variables listed in `docs\CONTAINER_ENVIRONMENT_VARIABLES.md` as needed while developing services.

<!-- Technical layer — this is what you add -->

**Current Context:** Assumption: to satisfy the required scaffolding and feature work without leaving the mandated layout, implementation should create or update `src/backend/Program.cs`, `src/backend/backend.csproj`, `src/backend/Endpoints/ConversionsEndpoints.cs`, `src/backend/Models/ConversionAuditRecord.cs`, `src/backend/Options/CosmosOptions.cs`, `src/backend/Services/ExchangeRateClient.cs`, `src/backend/Repositories/ConversionAuditRepository.cs`, `src/frontend/package.json`, `src/frontend/src/main.jsx`, `src/frontend/src/App.jsx`, `src/frontend/src/api/conversions.js`, `src/frontend/src/components/ConversionForm.jsx`, `src/frontend/src/components/ConversionHistory.jsx`, and reference `docs\CONTAINER_ENVIRONMENT_VARIABLES.md`; if workflow changes are required, update the exact existing file(s) under `.github/workflows/` rather than creating an alternate layout.

**In Scope:**
- Add a backend HTTP endpoint at `POST /api/conversions/quote` that accepts an amount, source currency, and target currency, calls one external exchange-rate provider, returns the quoted conversion, and writes an audit record for every successful quote.
- Add backend retrieval support for prior audit records so an auditor-facing lookup can fetch a specific conversion by its persisted identifier at `GET /api/conversions/{id}`.
- Persist immutable audit data in Cosmos DB for each successful conversion, including the original amount, source currency, target currency, exchange rate used, converted amount, provider identifier, and UTC timestamp used for the quote.
- Provision the required Cosmos database and container programmatically if they are absent, using the packages mandated in Known Constraints.
- Add a frontend screen in `src/frontend` that lets the user enter an amount and currency pair, displays the returned converted amount immediately, and then retrieve a prior conversion by audit identifier.
- Wire the frontend to call the backend through the runtime-resolved `VITE_API_URL` value rather than a build-time hard-coded origin.
- Add the minimum workflow and related-file updates needed so the scaffolded frontend and backend projects build consistently with the required stack versions and folder layout.

**Out of Scope:**
- User authentication, user-role management, or per-user authorization rules.
- Bulk conversion uploads, CSV import/export, or batch settlement processing.
- Historical re-pricing of past conversions using newly fetched rates.
- Multi-provider routing, provider failover orchestration, or rate caching beyond the single live quote request.
- Dashboard analytics, reporting exports, or compliance workflow features beyond lookup of stored conversions.
- Creating new Azure infrastructure outside the already provisioned Cosmos DB instance and Azure Container App usage described in Known Constraints.

**Feature Constraints:**
- Use `IHttpClientFactory` for the external exchange-rate provider integration so outbound HTTP behavior is centrally configured and testable.
- Treat each successful conversion as an immutable audit event: once written, the stored rate, converted amount, and timestamp must never be recalculated or updated in place.
- Store money and rate values using backend `decimal` handling and return currency amounts rounded to two fractional digits in API responses.
- Normalize currency codes to uppercase ISO-style three-letter strings before validation, provider calls, persistence, and response serialization.
- Return RFC 7807 `ProblemDetails` responses for invalid requests and upstream provider failures; do not return raw exception messages to the browser.
- Do not write an audit record when the provider quote fails or validation rejects the request.
- Include a provider response identifier field in the persisted audit document when the upstream provider supplies one; if the provider does not supply one, persist `null` explicitly and document that assumption in code comments.

**Expected Outcome:** A user can submit a quote request from the frontend, receive a trustworthy converted amount immediately, and later retrieve the exact same persisted quote by audit ID without recomputation. For example, if `POST /api/conversions/quote` receives `{ "amount": 100.00, "sourceCurrency": "usd", "targetCurrency": "eur" }` and the provider returns a rate of `0.92`, the API responds with HTTP `200` and a body containing an audit `id`, normalized currencies `USD` and `EUR`, `exchangeRate: 0.92`, `convertedAmount: 92.00`, and a UTC `quotedAtUtc` timestamp; the backend also persists those same values in Cosmos DB. A subsequent `GET /api/conversions/{id}` returns the stored record with the same rate and converted amount rather than a newly calculated value. If the provider call fails or times out, `POST /api/conversions/quote` returns a `ProblemDetails` response with HTTP `503`, and no audit document is created for that failed attempt.

**Expected Agent Output:** OpenCode should produce the minimal frontend and backend scaffolding required under `src/frontend` and `src/backend`, implement the quote and audit-lookup flow described above, add the Cosmos provisioning and persistence code, update only the necessary existing workflow/related files to keep the repository buildable, and report which files changed plus the verification steps it ran. The implementation should result in one backend quote endpoint, one backend lookup endpoint, one persisted audit record model/repository flow, and one frontend UI flow that performs quote submission and audit retrieval.

**Suggested Intent For OpenCode:** Implement the plan in `spec/real-time-currency-conversion-audit-trail.md`, and respect every Known Constraint verbatim while doing so — not just the feature-specific rules. That includes the exact `src/frontend` and `src/backend` layout, React/Vite on Node 24.*, C# on .NET 10, runtime `VITE_API_URL` placeholder replacement, runtime-only backend environment-variable configuration, the single Azure Container App sidecar deployment model, Cosmos authentication through the pre-assigned User-Assigned Managed Identity using `Azure.Identity`, use of `Microsoft.Azure.Cosmos` plus `Azure.ResourceManager` and `Azure.ResourceManager.CosmosDB`, required workflow/related-file updates, the branch/commit sequence stated in Known Constraints, and the environment variables documented in `docs\CONTAINER_ENVIRONMENT_VARIABLES.md`. Build the smallest complete implementation that adds a live conversion quote API, immutable Cosmos-backed audit persistence, audit lookup by ID, and a frontend UI for quote submission and retrieval, with concrete error handling and verification.
