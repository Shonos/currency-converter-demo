# Currency Converter Platform – Master Copilot Context

> **Purpose**: This is the master orchestration document for AI agents working on the Currency Converter Demo project. Each sub-task has its own dedicated `.copilot.md` file. When working on a specific sub-task, use **this master document + the relevant sub-task document** as context.

---

## 1. Project Overview

Build a full-stack currency conversion platform:

- **Backend**: C# / ASP.NET Core Web API (.NET 10)
- **Frontend**: React / TypeScript SPA
- **Data Source**: [Frankfurter API](https://frankfurter.dev/docs/) (free, no API key)

The platform retrieves live and historical exchange rates, performs currency conversion, and demonstrates production-grade architecture.

---

## 2. Repository Structure

```
currency-converter-demo/
├── README.md                          # Root readme (setup, architecture, AI usage)
├── docs/
│   ├── 0-master.copilot.md              # THIS FILE – master context
│   ├── 01-backend-project-setup.copilot.md
│   ├── 02-backend-api-endpoints.copilot.md
│   ├── 03-backend-resilience.copilot.md
│   ├── 04-backend-security.copilot.md
│   ├── 05-backend-observability.copilot.md
│   ├── 06-backend-testing.copilot.md
│   ├── 07-frontend-setup.copilot.md
│   ├── 08-frontend-features.copilot.md
│   ├── 09-frontend-testing.copilot.md
│   └── 10-deployment-cicd.copilot.md
├── currency-converter-api/
│   └── CurrencyConverterDemo/
│       ├── CurrencyConverterDemo.slnx
│       ├── CurrencyConverterDemo.Api/           # ASP.NET Core Web API
│       ├── CurrencyConverterDemo.Application/   # Business logic / use cases
│       ├── CurrencyConverterDemo.Domain/        # Domain models & interfaces
│       ├── CurrencyConverterDemo.Infrastructure/# External integrations (Frankfurter, caching)
│       └── CurrencyConverterDemo.Tests/         # Unit + integration tests
│           ├── Unit/
│           └── Integration/
└── currency-converter-web/                      # React/TypeScript frontend
    ├── src/
    ├── public/
    ├── package.json
    └── tsconfig.json
```

---

## 3. Technology Stack & Versions

### Backend
| Component          | Technology                        | Version / Notes             |
|--------------------|-----------------------------------|-----------------------------|
| Runtime            | .NET 10                           | Already in `.csproj`        |
| Web Framework      | ASP.NET Core                      | Minimal + Controllers       |
| API Versioning     | `Asp.Versioning.Mvc.ApiExplorer`  | v8.1.1 (already installed)  |
| API Docs           | Swashbuckle (Swagger)             | v10.1.2 (already installed) |
| HTTP Resilience    | `Microsoft.Extensions.Http.Resilience` (Polly v8+) | |
| Caching            | `IMemoryCache` + `IDistributedCache` | In-memory default, Redis-ready |
| Authentication     | `Microsoft.AspNetCore.Authentication.JwtBearer` | |
| Rate Limiting      | Built-in `Microsoft.AspNetCore.RateLimiting` | .NET 7+ native |
| Logging            | Serilog + `Serilog.AspNetCore`    | Structured JSON logging     |
| Testing            | xUnit + Moq/NSubstitute + FluentAssertions | |
| Coverage           | Coverlet + ReportGenerator        | Target ≥ 90%               |

### Frontend
| Component          | Technology                        |
|--------------------|-----------------------------------|
| Framework          | React 18+                         |
| Language           | TypeScript (strict)               |
| Build Tool         | Vite                              |
| HTTP Client        | Axios or fetch + React Query      |
| Routing            | React Router v6                   |
| UI Components      | Tailwind CSS (or similar)         |
| State Management   | React Query + React Context       |
| Testing            | Vitest + React Testing Library    |

---

## 4. Architecture Principles

### 4.1 Clean Architecture (Backend)
```
┌─────────────────────────────────────────────┐
│                   API Layer                  │  Controllers, Middleware, Filters
├─────────────────────────────────────────────┤
│               Application Layer             │  Use Cases, DTOs, Validation
├─────────────────────────────────────────────┤
│                 Domain Layer                 │  Entities, Interfaces, Enums
├─────────────────────────────────────────────┤
│             Infrastructure Layer            │  Frankfurter client, Caching, Auth
└─────────────────────────────────────────────┘
```

- **Dependency Rule**: Inner layers never reference outer layers.
- **Domain** defines `ICurrencyProvider` interface; **Infrastructure** implements it.
- **Application** contains business logic (conversion rules, excluded currencies).
- **API** is thin — delegates to Application services.

### 4.2 Key Design Patterns
| Pattern            | Usage                                                 |
|--------------------|-------------------------------------------------------|
| Factory            | `ICurrencyProviderFactory` selects provider at runtime |
| Strategy           | Multiple `ICurrencyProvider` implementations           |
| Repository-like    | Provider abstraction over external API                 |
| Decorator          | Caching & resilience wrapping the HTTP client          |
| Mediator (optional)| MediatR for CQRS if complexity warrants it            |

### 4.3 Frontend Architecture
```
src/
├── api/           # API client functions & React Query hooks
├── components/    # Reusable UI components
├── features/      # Feature modules (conversion, rates, history)
├── hooks/         # Custom React hooks
├── layouts/       # Page layouts
├── pages/         # Route-level page components
├── types/         # TypeScript interfaces & types
├── utils/         # Helpers, constants
└── App.tsx        # Root component + routing
```

---

## 5. Frankfurter API Reference

**Base URL**: `https://api.frankfurter.dev/v1/`

| Endpoint                              | Description                     | Example                                              |
|---------------------------------------|---------------------------------|------------------------------------------------------|
| `GET /latest`                         | Latest rates (default base EUR) | `/latest?base=USD&symbols=GBP,JPY`                   |
| `GET /{date}`                         | Historical rates for a date     | `/2024-01-15?base=EUR`                               |
| `GET /{start_date}..{end_date}`       | Time series over a range        | `/2024-01-01..2024-01-31?base=EUR&symbols=USD`       |
| `GET /currencies`                     | List of supported currencies    | Returns `{ "AUD": "Australian Dollar", ... }`        |

**Key details**:
- No API key required, no rate limits (but be respectful)
- Dates in UTC; data updates ~16:00 CET on working days
- Response includes `amount`, `base`, `date`, and `rates` object
- Time series response has `start_date`, `end_date`, `rates` keyed by date

---

## 6. Excluded Currencies (Business Rule)

The following currencies must be **rejected with HTTP 400** when used as source or target:

| Code | Name           |
|------|----------------|
| TRY  | Turkish Lira   |
| PLN  | Polish Złoty   |
| THB  | Thai Baht      |
| MXN  | Mexican Peso   |

This rule applies to:
- Currency conversion endpoint
- Must return clear error message in response body

---

## 7. Cross-Cutting Concerns

### 7.1 Authentication & Authorization
- JWT Bearer tokens (symmetric key for demo, configurable per environment)
- Role-based: `Admin`, `User` roles minimum
- All currency endpoints require authentication
- `/currencies` list can be public (for frontend dropdowns)

### 7.2 API Versioning
- Already scaffolded with `Asp.Versioning`
- Use URL path versioning: `/api/v1/...`, `/api/v2/...`
- V1 is the current implementation; V2 stub exists for future

### 7.3 Environments
- `Development` – verbose logging, Swagger enabled, relaxed CORS
- `Test` – in-memory services, test JWT issuer
- `Production` – minimal logging, CORS locked, HTTPS enforced

### 7.4 Error Response Format
Standardize error responses across the API:
```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Currency 'TRY' is not supported for conversion.",
  "traceId": "00-abc123..."
}
```
Use ASP.NET Core's `ProblemDetails` for consistency.

---

## 8. Sub-Task Index

| # | File | Scope | Depends On |
|---|------|-------|------------|
| 01 | `01-backend-project-setup.copilot.md` | Solution structure, projects, DI, factory pattern, provider interfaces | — |
| 02 | `02-backend-api-endpoints.copilot.md` | Controllers, DTOs, validation, all 3 endpoints | 01 |
| 03 | `03-backend-resilience.copilot.md` | Caching, retry policies, circuit breaker | 01, 02 |
| 04 | `04-backend-security.copilot.md` | JWT auth, RBAC, rate limiting | 01, 02 |
| 05 | `05-backend-observability.copilot.md` | Serilog, structured logging, correlation, metrics | 01, 02 |
| 06 | `06-backend-testing.copilot.md` | Unit tests, integration tests, coverage | 01–05 |
| 07 | `07-frontend-setup.copilot.md` | React/TS project scaffolding, tooling | — |
| 08 | `08-frontend-features.copilot.md` | All UI features, API integration | 07 |
| 09 | `09-frontend-testing.copilot.md` | Frontend component & integration tests | 07, 08 |
| 10 | `10-deployment-cicd.copilot.md` | CI/CD, Docker, environments, README | All |

**Recommended execution order**: 01 → 02 → (03, 04, 05 in parallel) → 06 → 07 → 08 → 09 → 10

---

## 9. Conventions & Standards


### Naming
- **C# Namespaces**: `CurrencyConverterDemo.{Layer}.{Feature}`
- **C# Files**: PascalCase, one class per file
- **TS/React**: PascalCase components, camelCase functions/variables, kebab-case files
- **API Routes**: lowercase, kebab-case: `/api/v1/exchange-rates/latest`

### Code Quality
- Nullable reference types enabled (`<Nullable>enable</Nullable>`)
- Implicit usings enabled
- All public APIs must have XML doc comments
- Frontend: ESLint + Prettier enforced

### Git
- Conventional commits: `feat:`, `fix:`, `test:`, `docs:`, `chore:`
- Feature branches: `feature/backend-endpoints`, `feature/frontend-setup`

---

## 10. AI Usage Documentation (for README)

Every agent should note in commit messages or PR descriptions:
- What AI generated or assisted with
- What was manually reviewed, modified, or rejected
- Key design decisions made by the developer (not AI)

This will be consolidated into the final README's "AI Usage" section.
