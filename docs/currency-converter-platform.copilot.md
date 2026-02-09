# Currency Converter Platform — Agent Instructions

Purpose
-------
This document is an actionable agent spec for implementing a full-stack currency conversion platform (ASP.NET Core backend + React frontend). It defines responsibilities, prioritized tasks, API surface, architectural constraints, test expectations, deploy guidance, and acceptance criteria so an engineer or an AI agent can execute reliably.

Responsibilities
----------------
- Implement a secure, observable, testable backend API using C# / ASP.NET Core.
- Implement a TypeScript React frontend that consumes the API and provides the required UX flows.
- Use AI-assisted development for productivity (code generation, unit tests, documentation), but ensure human review for security-sensitive code (auth, secrets).

Prioritized Workplan (high level)
--------------------------------
1. Create this agent doc and project skeletons (backend + frontend).
2. Implement core backend services: provider factory, Frankfurter provider, caching, retry, circuit breaker.
3. Implement API endpoints (v1): latest rates, convert, historical (paginated) with validation rules.
4. Add security: JWT auth, RBAC, client ID extraction, rate limiting.
5. Add observability: Serilog structured logs, correlation IDs, metrics endpoint.
6. Write unit and integration tests (>=90% backend coverage); add frontend tests for key flows.
7. Prepare CI/CD documentation and Docker artifacts for Dev/Test/Prod.

Functional API Specification
----------------------------
Base path: `/api/v1` (versioned)

Endpoints
- GET `/api/v1/rates/latest?base={currency}`
	- Description: Return latest exchange rates for base currency.
	- Query: `base` (required, 3-letter ISO currency)
	- Response: 200 JSON { base: string, date: YYYY-MM-DD, rates: { [currency]: decimal } }

- GET `/api/v1/convert?from={src}&to={dst}&amount={value}`
	- Description: Convert `amount` from `src` to `dst` using latest rates.
	- Query: `from`, `to`, `amount` (decimal)
	- Validation: If either currency is TRY, PLN, THB, or MXN → return 400 with JSON { error: "Excluded currency: {code}" }
	- Response: 200 JSON { from, to, amount, rate, result }

- GET `/api/v1/rates/historical?base={currency}&start={YYYY-MM-DD}&end={YYYY-MM-DD}&page={n}&pageSize={m}`
	- Description: Return paginated historical rates for a date range.
	- Pagination: `page` (1-based), `pageSize` (default 50, max 200)
	- Response: 200 JSON { base, start, end, totalItems, page, pageSize, items: [ { date, rates } ] }

Error handling
- Use appropriate status codes and consistent JSON error schema: { errorCode, message, details? }

Excluded currencies
- Requests using TRY, PLN, THB, MXN (either as source or target) must return 400 and a clear message.

Backend Architecture & Implementation Notes
------------------------------------------
- Provider abstraction: `IExchangeRateProvider` with implementations (Frankfurter, future providers). Use a factory `IExchangeRateProviderFactory` to pick provider by config.
- HTTP resilience: Use Polly for retries with exponential backoff, timeout, and a circuit breaker when calling external APIs.
- Caching: Use `IDistributedCache` with a memory-backed implementation in Dev and Redis for Prod. Cache latest rates per base for configurable TTL (e.g., 5 minutes).
- Correlation: Accept `X-Correlation-ID` header; generate if missing; propagate to Frankfurter requests and include in logs.
- Logging: Use Serilog structured logging. Log for every request: client IP, client ID (from JWT), HTTP method & path, status code, response time, correlation id.
- Metrics: expose Prometheus-style metrics endpoint (`/metrics`) and add counters for external API calls, cache hits/misses.
- Security: JWT bearer authentication; roles `User` and `Admin`. Use middleware to extract `client_id` claim. Enforce RBAC on endpoints if needed.
- Rate limiting: Implement token-bucket or leaky-bucket per `client_id` (e.g., 100 req/min default) using middleware or ASP.NET rate-limit packages.
- API versioning: Add route/versioning header support to allow future v2.

Observability details
- Correlate internal API requests with calls to Frankfurter using correlation id and logs.
- Include structured fields: `CorrelationId`, `ClientId`, `ClientIp`, `HttpMethod`, `Route`, `StatusCode`, `ElapsedMs`, `ProviderName`, `ExternalCallLatencyMs`.

Security & Secrets
- Store secrets (JWT signing key, Redis connection string) in environment variables / secret store. Do not commit secrets.

Testing Strategy
----------------
- Unit tests: xUnit + FluentAssertions; use Microsoft.Extensions.Hosting test host for DI. Target >=90% coverage for backend core logic.
- Integration tests: run a containerized Redis (if used) and either mock Frankfurter via WireMock or use recorded responses (VCR-style). Test rate limit behavior, excluded-currency validation, paging.
- Coverage: use `coverlet` to produce coverage reports (Cobertura/HTML) and upload via CI.
- Frontend tests: React Testing Library + Vitest/Jest for component flows: conversion form, latest rates, historical pagination, error states.

Frontend Implementation Notes
----------------------------
- Tech: React + TypeScript, use React Query (TanStack Query) for data fetching and caching.
- Structure: pages/components/services/hooks. Keep API client in `src/services/api.ts`.
- UX: Clear validation (show excluded-currency errors), loading states, accessible form controls.
- Tests: Focus on conversions, exclusion validation, pagination and error displays.

Repository Layout (recommended)
------------------------------
- /currency-converter-api
	- /src
		- /CurrencyConverter.Api (ASP.NET Core Web API)
		- /CurrencyConverter.Core (domain models, interfaces)
		- /CurrencyConverter.Services (providers, caching, resilience)
	- /tests
		- /CurrencyConverter.UnitTests
		- /CurrencyConverter.IntegrationTests

- /currency-converter-web
	- /src (React app)
	- /tests

Developer Quick Start (local)
----------------------------
Prereqs: .NET SDK 7+, Node 18+, Docker (optional for Redis)

Backend

1) Create solution and projects (example):

```bash
dotnet new sln -n CurrencyConverter
dotnet new webapi -o currency-converter-api/src/CurrencyConverter.Api
dotnet new classlib -o currency-converter-api/src/CurrencyConverter.Core
dotnet new classlib -o currency-converter-api/src/CurrencyConverter.Services
dotnet sln add currency-converter-api/src/**/*.csproj
```

2) Run API (Dev):

```bash
cd currency-converter-api/src/CurrencyConverter.Api
dotnet run
```

Frontend

1) Create React app (Vite):

```bash
cd currency-converter-web
npm create vite@latest . -- --template react-ts
npm install
npm run dev
```

2) Build and run tests:

```bash
# Backend tests
dotnet test /p:CollectCoverage=true

# Frontend tests
npm test
```

CI/CD Guidance
--------------
- Use GitHub Actions with separate jobs: build/test/backend, build/test/frontend, coverage upload, build Docker images, push to registry, and (optionally) deploy to environments.
- Use secrets for production settings and environment-specific configs. Include a `deploy/` folder with helm/kubernetes manifests if deploying to k8s.

Acceptance Criteria (minimal for delivery)
----------------------------------------
- Backend endpoints implemented and passing integration tests.
- Excluded currencies produce HTTP 400 with clear message.
- Latest rates and conversion endpoints return correct shapes and cache Frankfurter responses.
- Historical endpoint supports pagination and date-range queries.
- JWT authentication is enforced; sample user flows documented.
- Structured logs include correlation ID and client ID; external calls are correlated.
- Unit coverage >=90% for backend core logic; integration tests exist for external API interactions.
- Frontend provides conversion form, latest rates view, historical rates with pagination, validation, loading and error states.
- Documentation includes run and test commands and CI/CD notes.

What the agent should ask or confirm before making changes
---------------------------------------------------------
- Preferred exchange rate provider name if not Frankfurter by default.
- Choice of distributed cache in Prod (Redis recommended) and connection details.
- JWT issuer and claims format (or provide example tokens for local dev).
- Rate limit policy values per client.

Delivery checklist (for a single PR)
-----------------------------------
- [ ] API project scaffold committed
- [ ] Provider factory + Frankfurter provider implemented
- [ ] Endpoints implemented + validation for excluded currencies
- [ ] Resilience (retry, circuit breaker) and caching wired
- [ ] JWT auth + RBAC + rate-limiting middleware configured
- [ ] Serilog + correlation implemented
- [ ] Unit + integration tests added; coverage report included
- [ ] React app scaffold + key views + tests
- [ ] README and CI docs updated

Appendix: Example HTTP error response
------------------------------------
400 Excluded currency example:

```json
{
	"errorCode": "ExcludedCurrency",
	"message": "Currency MXN is not supported by this API."
}
```

Appendix: Example correlation and propagation
--------------------------------------------
- Client sends: `X-Correlation-ID: abcd-1234`
- API logs and forwards `X-Correlation-ID` to outgoing calls to Frankfurter.

-- end of agent spec --

