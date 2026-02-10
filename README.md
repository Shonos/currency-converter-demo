# Currency Converter Demo

A full-stack currency conversion platform demonstrating production-grade architecture with **ASP.NET Core 10** and **React 19 + TypeScript**. Retrieves live and historical exchange rates from the [Frankfurter API](https://frankfurter.dev/docs/), performs currency conversion, and showcases clean architecture, resilience patterns, JWT authentication, structured logging, and containerized deployment.

---

## Table of Contents

- [Setup Instructions](#setup-instructions)
  - [Run with Docker (Recommended)](#run-with-docker-recommended)
  - [Run Locally](#run-locally)
  - [IDE & Debugging](#ide--debugging)
- [Architecture Overview](#architecture-overview)
- [API Endpoints](#api-endpoints)
- [AI Usage Explanation](#ai-usage-explanation)
- [Assumptions & Trade-offs](#assumptions--trade-offs)
- [Potential Future Improvements](#potential-future-improvements)

---

## Setup Instructions

### Run with Docker (Recommended)

The entire stack — API, frontend, and Redis — runs with a single command. No SDKs required.

**Prerequisites:** [Docker Desktop](https://www.docker.com/products/docker-desktop/) (includes Docker Compose)

```bash
# Clone the repository
git clone https://github.com/yourorg/currency-converter-demo.git
cd currency-converter-demo

# Create environment file
cp .env.example .env
# (Optional) Edit .env to change JWT secret or Redis password

# Build and start all services
docker-compose up -d

# Verify services are running
docker-compose ps

# View logs
docker-compose logs -f
```

**Access the application:**
| Service | URL |
|---------|-----|
| Frontend | http://localhost:3000 |
| Backend API | http://localhost:5000 |
| Swagger UI | http://localhost:5000/swagger |

**Run tests in Docker:**
```bash
# Run all backend + frontend tests
docker-compose -f docker-compose.yml -f docker-compose.test.yml up --abort-on-container-exit

# Run backend tests only
docker build --target test -t currency-api:test ./currency-converter-api

# Run frontend tests only
docker build --target test -t currency-web:test ./currency-converter-web
```

**Stop everything:**
```bash
docker-compose down        # Stop services
docker-compose down -v     # Stop and remove volumes
```

---

### Run Locally

For local development without Docker, install the following:

**Prerequisites:**
| Tool | Version | Download |
|------|---------|----------|
| .NET SDK | 10.0+ | [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/10.0) |
| Node.js | 20+ | [nodejs.org](https://nodejs.org/) |
| Redis *(optional)* | 7+ | [redis.io](https://redis.io/download/) or use the Redis docker-compose below |

**Backend:**
```bash
cd currency-converter-api/CurrencyConverterDemo

# Restore and build
dotnet restore
dotnet build

# Run tests
dotnet test

# Start API (runs on http://localhost:5000)
dotnet run --project CurrencyConverterDemo.Api
```

**Frontend:**
```bash
cd currency-converter-web

# Install dependencies
npm install

# Start dev server (runs on http://localhost:5173)
npm run dev

# Run tests
npm run test:run
```

**Redis (standalone, for caching):**

If you don't have Redis installed locally, use the provided standalone compose:
```bash
cd currency-converter-redis
docker-compose up -d
# Redis available on localhost:6379
# Redis Commander UI on http://localhost:8081
```

Without Redis, the API defaults to in-memory caching — no configuration change needed.

---

### IDE & Debugging

| IDE | Best For | Notes |
|-----|----------|-------|
| **VS Code** | Full-stack development | C# Dev Kit extension for backend, ESLint + Prettier for frontend. Supports debugging both API and React simultaneously |
| **Visual Studio 2022/2026 Community** | Backend-focused development | Richer .NET debugging, NuGet management, test explorer, and profiling tools |

For VS Code, the recommended extensions are:
- C# Dev Kit (ms-dotnettools.csdevkit)
- ESLint (dbaeumer.vscode-eslint)
- Tailwind CSS IntelliSense (bradlc.vscode-tailwindcss)

---

## Architecture Overview

### System Components

```
┌──────────────┐     ┌──────────────────┐     ┌────────────────────┐
│   React SPA  │────▶│  ASP.NET Core    │────▶│  Frankfurter API   │
│  (Port 3000) │     │  API (Port 5000) │     │  (External)        │
└──────────────┘     └────────┬─────────┘     └────────────────────┘
                              │
                     ┌────────▼─────────┐
                     │   Redis Cache    │
                     │   (Port 6379)    │
                     └──────────────────┘
```

### Backend — Clean Architecture

```
┌─────────────────────────────────────────────────────────┐
│  API Layer (CurrencyConverterDemo.Api)                  │
│  Controllers, Middleware, Filters, Program.cs            │
├─────────────────────────────────────────────────────────┤
│  Application Layer (CurrencyConverterDemo.Application)  │
│  CurrencyService, DTOs, Validators, Exceptions          │
├─────────────────────────────────────────────────────────┤
│  Domain Layer (CurrencyConverterDemo.Domain)            │
│  Models, Interfaces, Enums, Constants                   │
├─────────────────────────────────────────────────────────┤
│  Infrastructure Layer (CurrencyConverterDemo.Infrastructure)  │
│  Frankfurter API client, Caching, Resilience, HTTP      │
└─────────────────────────────────────────────────────────┘
```

**Dependency rule:** Inner layers never reference outer layers. Domain defines `ICurrencyProvider`; Infrastructure implements it.

### Key Design Patterns

| Pattern | Where |
|---------|-------|
| **Factory** | `ICurrencyProviderFactory` — selects currency provider at runtime |
| **Strategy** | Multiple `ICurrencyProvider` implementations possible |
| **Decorator** | `CachedCurrencyProvider` wraps providers with caching |
| **Circuit Breaker + Retry** | Polly v8 resilience pipeline on HTTP calls to Frankfurter |

### Resilience

All external HTTP calls go through a Polly resilience pipeline:
- **Retry** with exponential backoff + jitter (handles transient failures)
- **Circuit Breaker** (prevents hammering a failing upstream)
- **Timeout** (per-attempt and total request timeouts)

### Caching

Flexible caching strategy controlled by configuration:
- **In-Memory:** `IMemoryCache` (default for Development environment)
- **Distributed:** Redis via `IDistributedCache` with StackExchange.Redis (default for Production environment)
- Cache type is controlled by `CacheSettings:Type` ("Memory" or "Distributed") and can be used in any environment
- Includes automatic fallback to in-memory if Redis connection is unavailable
- Cache-aside pattern with configurable TTL per data type

### Security

- JWT Bearer authentication with role-based authorization (Admin, User roles)
- Rate limiting (built-in ASP.NET Core `RateLimiting`)
- CORS policy per environment
- Excluded currencies business rule — TRY, PLN, THB, MXN rejected with HTTP 400

### Observability

- **Serilog** structured logging (JSON) to console and file
- Correlation ID middleware for request tracing
- Health check endpoints (`/health`) for Redis and upstream API

### Frontend

- **React 19** with TypeScript in strict mode
- **TanStack Query** for server state management and caching
- **React Router v6** for SPA routing
- **Tailwind CSS** for styling
- **Axios** HTTP client with interceptors for JWT
- **Vitest + Testing Library + MSW** for testing

---

## API Endpoints

All exchange rate endpoints require JWT authentication. Authenticate first via the login endpoint.

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| `POST` | `/api/v1/auth/login` | No | Authenticate and receive JWT token |
| `GET` | `/api/v1/auth/demo-users` | No | List available demo users |
| `GET` | `/api/v1/currencies` | Yes | List supported currencies |
| `GET` | `/api/v1/exchange-rates/latest` | Yes | Latest rates for a base currency |
| `GET` | `/api/v1/exchange-rates/convert` | Yes | Convert amount between currencies |
| `GET` | `/api/v1/exchange-rates/history` | Yes | Historical rates with pagination |
| `GET` | `/health` | No | Health check |

**Quick test:**
```bash
# Get a token
TOKEN=$(curl -s -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"Admin123!"}' | jq -r '.token')

# Get latest rates
curl -s "http://localhost:5000/api/v1/exchange-rates/latest?baseCurrency=USD" \
  -H "Authorization: Bearer $TOKEN" | jq

# Convert currency
curl -s "http://localhost:5000/api/v1/exchange-rates/convert?from=USD&to=EUR&amount=100" \
  -H "Authorization: Bearer $TOKEN" | jq
```

---

## AI Usage Explanation

### Overview

Approximately **90% of the code** in this project was generated with AI assistance. This section documents how, why, and what was validated.

### The Approach

**Phase 1 — Initial scaffolding with free-tier models**

I started with free models available through GitHub Copilot — **Grok Code Fast 1** and **GPT-5 mini**. During this phase I was able to scaffold:
- The initial ASP.NET Core Web API project
- Swagger/OpenAPI configuration
- API versioning setup with `Asp.Versioning`

This proved **time-consuming and inconsistent** for a project of this scope. The free models frequently produced outdated patterns, required heavy correction, and lacked the ability to maintain architectural coherence across files.

**Phase 2 — Structured agent-driven development with Claude Opus 4.6 and Claude Sonnet 4.5**

Seeing hours go by with limited progress, I shifted strategy. I used **Claude Opus 4.6** to generate **detailed context documents** — one master file and ten sub-task files (the `.copilot.md` files numbered 00 to 10). These documents served as structured prompts for **Claude Sonnet 4.5** via GitHub Copilot agent mode. Each subtask was then implemented by Claude Sonnet 4.5 in a separate agent session:

| Document | Scope |
|----------|-------|
| [`00-master.copilot.md`](docs/00-master.copilot.md) | Master context — architecture, tech stack, conventions |
| [`01-backend-project-setup.copilot.md`](docs/01-backend-project-setup.copilot.md) | Solution structure, DI, factory pattern, provider interfaces |
| [`02-backend-api-endpoints.copilot.md`](docs/02-backend-api-endpoints.copilot.md) | Controllers, DTOs, validation, all API endpoints |
| [`03-backend-resilience.copilot.md`](docs/03-backend-resilience.copilot.md) | Caching, retry policies, circuit breaker (Polly v8) |
| [`04-backend-security.copilot.md`](docs/04-backend-security.copilot.md) | JWT authentication, RBAC, rate limiting |
| [`05-backend-observability.copilot.md`](docs/05-backend-observability.copilot.md) | Serilog structured logging, correlation IDs |
| [`06-backend-testing.copilot.md`](docs/06-backend-testing.copilot.md) | Unit tests, integration tests, coverage targets |
| [`07-frontend-setup.copilot.md`](docs/07-frontend-setup.copilot.md) | React/TypeScript project scaffolding |
| [`08-frontend-features.copilot.md`](docs/08-frontend-features.copilot.md) | UI features, API integration, routing |
| [`09-frontend-testing.copilot.md`](docs/09-frontend-testing.copilot.md) | Frontend component and integration tests |
| [`10-deployment-cicd.copilot.md`](docs/10-deployment-cicd.copilot.md) | Docker multi-stage builds, containerization |

Each sub-task was fed to the agent alongside the master context. After each stage, I tested that everything compiled, ran, and behaved correctly before moving to the next.

### What I Validated and Changed Manually

Every AI-generated stage went through manual review. Specific things I caught and corrected:

- **Polly v8 syntax**: AI initially generated Polly v7 patterns. Had to update to the new `Microsoft.Extensions.Http.Resilience` pipeline builder API
- **Test assertions and queries**: Multiple frontend tests failed due to incorrect selectors, timing issues, and label/input association problems — all fixed manually through iterative debugging
- **Accessibility**: Added `htmlFor`/`id` linking on form inputs, ARIA labels on Spinner and Pagination components — these were missing from AI output
- **Error handling**: AI's `getErrorMessage` utility didn't properly handle Axios mock errors in tests — rewrote the type checking logic
- **Docker port mapping**: Verified .NET 10's default port 8080 requirement was correctly mapped through all Docker and compose configurations
- **Redis password synchronization**: AI generated mismatched passwords across `.env` files — aligned them manually
- **Frontend date formatting**: Tests expected ISO dates but components used `date-fns` formatDate — corrected test expectations

### What I Did NOT Blindly Accept

1. **Architecture decisions** — The clean architecture layers, project references, and dependency rule were designed by me in the master context doc, not suggested by AI
2. **Business rules** — Excluded currencies list, pagination strategy for historical rates, cache TTL values
3. **Security configuration** — JWT secret handling, CORS policies per environment, rate limiting thresholds
4. **Docker strategy** — Multi-stage build approach with test/build/runtime targets, cloud-agnostic design
5. **The agent workflow itself** — Writing structured context documents to drive AI was my approach to the problem

### How I Collaborate with AI

My workflow is: **design the architecture → write detailed specs → let AI implement → test and fix → iterate**. The AI is a fast typist with broad knowledge, but architectural decisions, business logic validation, and integration testing remain my responsibility. The context documents in `docs/` are the clearest evidence of this — they represent the thinking and planning that guided every line of generated code.

---

## Assumptions & Trade-offs

### Assumptions

1. **Demo users are acceptable** — Hardcoded users (admin/user/viewer) with in-memory storage. No real identity provider needed for a demo
2. **Frankfurter API is reliable** — Single external dependency with no fallback provider
3. **Stateless API** — JWT tokens are self-contained; no server-side session state
4. **No persistent database** — All data comes from the external API and is cached temporarily

### Trade-offs

| Decision | Benefit | Trade-off |
|----------|---------|-----------|
| Hardcoded demo users | No database or auth provider dependency | Not production-ready for real users |
| Symmetric JWT (HMAC) | Simple configuration | Less secure than asymmetric (RS256) with key rotation |
| In-memory pagination of historical rates | Simple — Frankfurter returns all dates, we paginate in-memory | Memory-intensive for very large date ranges |
| Single currency provider | Simple implementation, clean factory pattern ready for extension | No failover if Frankfurter is down |
| Excluded currencies as constants | Fast lookups, easy to maintain | Requires code change to modify the list |
| Single-level cache (Redis OR memory) | Simple implementation, clear separation of concerns | No multi-level caching (L1 in-memory + L2 distributed) — can't benefit from local cache speed with distributed cache consistency |

---

## Potential Future Improvements

### High Priority
1. **OAuth2 / OpenID Connect** — Replace demo JWT with a real identity provider (Auth0, Azure AD, Keycloak)
2. **Redis in production** — The distributed cache is implemented and ready; just needs Redis infrastructure
3. **Database layer** — PostgreSQL or SQL Server for user management, audit logs, and conversion history
4. **OpenTelemetry** — Replace Serilog-only observability with distributed tracing and metrics
5. **CI/CD pipeline** — GitHub Actions or Azure DevOps for automated build, test, and deploy

### Medium Priority
6. **Real-time rates** — WebSocket/SignalR for live rate updates
7. **Historical charts** — Visualization with Chart.js or Recharts
8. **Multiple currency providers** — Add fallback providers using the existing factory pattern
9. **Export functionality** — CSV/Excel export of historical data
10. **API response compression** — Gzip/Brotli for large payloads

### Low Priority
11. **Internationalization (i18n)** — Multi-language support with react-i18next
12. **Dark mode** — User preference toggle
13. **GraphQL API** — Alternative query interface for flexible data fetching
14. **Rate change notifications** — Email/SMS alerts for threshold-based rate changes
15. **Mobile app** — React Native or .NET MAUI companion app

---

## Technology Stack

### Backend
| Component | Technology | Version |
|-----------|-----------|---------|
| Runtime | .NET | 10.0 |
| Framework | ASP.NET Core | 10.0 |
| API Versioning | Asp.Versioning.Mvc.ApiExplorer | 8.1.1 |
| API Docs | Swashbuckle (Swagger) | 10.1.2 |
| Resilience | Microsoft.Extensions.Http.Resilience (Polly v8) | 10.2.0 |
| Caching | IMemoryCache + StackExchange.Redis | 10.0.2 |
| Auth | JWT Bearer | 10.0.2 |
| Logging | Serilog | 10.0.0 |
| Testing | xUnit + Moq + FluentAssertions | — |

### Frontend
| Component | Technology | Version |
|-----------|-----------|---------|
| UI Library | React | 19.2.0 |
| Language | TypeScript | 5.9.3 |
| Build Tool | Vite | 7.3.1 |
| Data Fetching | TanStack Query | 5.56.0 |
| HTTP Client | Axios | 1.6.7 |
| Routing | React Router | 6.22.0 |
| Styling | Tailwind CSS | 2.2.19 |
| Testing | Vitest + Testing Library + MSW | 2.1.8 |

### Infrastructure
| Component | Technology |
|-----------|-----------|
| Containerization | Docker with multi-stage builds |
| Orchestration | Docker Compose |
| Cache | Redis 7 / Valkey |
| Web Server | Nginx (frontend container) |

---

## Project Structure

```
currency-converter-demo/
├── README.md
├── docker-compose.yml              # Full stack: API + Web + Redis
├── docker-compose.test.yml         # Test override
├── .env.example                    # Environment template
├── docs/                           # Architecture & agent context docs
│   ├── 00-master.copilot.md
│   ├── 01–10-*.copilot.md
│   └── distributed-cache-implementation.md
├── currency-converter-api/         # Backend
│   ├── Dockerfile
│   └── CurrencyConverterDemo/
│       ├── CurrencyConverterDemo.slnx
│       ├── CurrencyConverterDemo.Api/
│       ├── CurrencyConverterDemo.Application/
│       ├── CurrencyConverterDemo.Domain/
│       ├── CurrencyConverterDemo.Infrastructure/
│       └── CurrencyConverterDemo.Tests/
├── currency-converter-web/         # Frontend
│   ├── Dockerfile
│   ├── nginx.conf
│   └── src/
└── currency-converter-redis/       # Standalone Redis/Valkey for local dev
    └── docker-compose.yml
```

---

## License

MIT