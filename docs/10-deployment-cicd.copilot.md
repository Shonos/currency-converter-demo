# Sub-Task 10: Deployment, CI/CD & Documentation

> **Context**: Use with `00-master.copilot.md`. **Depends on**: All previous sub-tasks should be complete.

---

## Objective

Configure multi-environment support, finalize API versioning, create Docker configuration, set up CI/CD pipeline definitions, and write the final README with comprehensive documentation.

---

## 1. Multi-Environment Configuration

### 1.1 Backend Environments

Three environments already have config files: `Development`, `Test`, `Production`.

#### `appsettings.Development.json`
```json
{
  "Serilog": {
    "MinimumLevel": { "Default": "Debug" }
  },
  "JwtSettings": {
    "Secret": "DevSecret-minimum-32-characters-long-key-here!!"
  },
  "Cors": {
    "AllowedOrigin": "http://localhost:5173"
  },
  "Swagger": { "Enabled": true }
}
```

#### `appsettings.Test.json`
```json
{
  "Serilog": {
    "MinimumLevel": { "Default": "Warning" }
  },
  "JwtSettings": {
    "Secret": "TestSecret-minimum-32-characters-long-key-here!!"
  },
  "CurrencyProvider": {
    "Frankfurter": {
      "BaseUrl": "http://localhost:9090/v1/"
    }
  }
}
```

#### `appsettings.Production.json`
```json
{
  "Serilog": {
    "MinimumLevel": { "Default": "Warning" },
    "WriteTo": [
      {
        "Name": "File",
        "Args": {
          "path": "/var/log/currency-converter/log-.json",
          "rollingInterval": "Day",
          "formatter": "Serilog.Formatting.Compact.CompactJsonFormatter, Serilog.Formatting.Compact"
        }
      }
    ]
  },
  "JwtSettings": {
    "Secret": ""
  },
  "Cors": {
    "AllowedOrigin": "https://currency-converter.example.com"
  },
  "Swagger": { "Enabled": false }
}
```

### 1.2 Frontend Environments

```env
# .env.development
VITE_API_BASE_URL=http://localhost:5000/api

# .env.production
VITE_API_BASE_URL=https://api.currency-converter.example.com/api
```

---

## 2. API Versioning (Finalization)

### 2.1 Current State

API versioning is already scaffolded via `Asp.Versioning.Mvc.ApiExplorer`. Ensure:

- All v1 controllers have `[ApiVersion("1.0")]`
- URL path versioning: `/api/v1/...`
- Swagger shows version selector
- V2 stub exists for future use

### 2.2 Deprecation Strategy

```csharp
// Example: when V2 is ready
[ApiVersion("1.0", Deprecated = true)]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/exchange-rates")]
public class ExchangeRatesController : ControllerBase
{
    [HttpGet("latest")]
    [MapToApiVersion("1.0")]
    public async Task<ActionResult> GetLatestRatesV1(...) { }

    [HttpGet("latest")]
    [MapToApiVersion("2.0")]
    public async Task<ActionResult> GetLatestRatesV2(...) { }
}
```

---

## 3. Horizontal Scaling Considerations

Document these design decisions (already implemented or ready):

### 3.1 Stateless API
- JWT tokens are self-contained (no server-side session)
- No in-process state that prevents multiple instances

### 3.2 Cache Strategy for Scale
- Current: `IMemoryCache` (per-instance)
- Production-ready: Switch to `IDistributedCache` with Redis
  - Configuration already supports it via `CacheSettings`
  - Only requires changing DI registration and adding Redis connection string

### 3.3 Rate Limiting for Scale
- Current: in-memory rate limiter (per-instance)
- Production: use Redis-backed rate limiter for distributed counting

---

## 4. Docker Configuration

### 4.1 Backend Dockerfile

```dockerfile
# currency-converter-api/Dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CurrencyConverterDemo/*.slnx .
COPY CurrencyConverterDemo/CurrencyConverterDemo.Api/*.csproj CurrencyConverterDemo.Api/
COPY CurrencyConverterDemo/CurrencyConverterDemo.Application/*.csproj CurrencyConverterDemo.Application/
COPY CurrencyConverterDemo/CurrencyConverterDemo.Domain/*.csproj CurrencyConverterDemo.Domain/
COPY CurrencyConverterDemo/CurrencyConverterDemo.Infrastructure/*.csproj CurrencyConverterDemo.Infrastructure/

RUN dotnet restore

COPY CurrencyConverterDemo/ .
RUN dotnet publish CurrencyConverterDemo.Api -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "CurrencyConverterDemo.Api.dll"]
```

### 4.2 Frontend Dockerfile

```dockerfile
# currency-converter-web/Dockerfile
FROM node:20-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM nginx:alpine AS runtime
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
```

### 4.3 Docker Compose

```yaml
# docker-compose.yml (root)
version: '3.8'

services:
  api:
    build:
      context: ./currency-converter-api
      dockerfile: Dockerfile
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - JwtSettings__Secret=${JWT_SECRET}
      - Cors__AllowedOrigin=http://localhost:3000
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  web:
    build:
      context: ./currency-converter-web
      dockerfile: Dockerfile
    ports:
      - "3000:80"
    depends_on:
      api:
        condition: service_healthy
```

---

## 5. CI/CD Pipeline (GitHub Actions)

### 5.1 Backend CI

```yaml
# .github/workflows/backend-ci.yml
name: Backend CI

on:
  push:
    paths: ['currency-converter-api/**']
  pull_request:
    paths: ['currency-converter-api/**']

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore
        working-directory: currency-converter-api/CurrencyConverterDemo
        run: dotnet restore

      - name: Build
        working-directory: currency-converter-api/CurrencyConverterDemo
        run: dotnet build --no-restore --configuration Release

      - name: Test with Coverage
        working-directory: currency-converter-api/CurrencyConverterDemo
        run: |
          dotnet test --no-build --configuration Release \
            /p:CollectCoverage=true \
            /p:CoverletOutputFormat=cobertura \
            /p:CoverletOutput=./TestResults/

      - name: Upload Coverage
        uses: codecov/codecov-action@v3
        with:
          file: '**/TestResults/coverage.cobertura.xml'
          fail_ci_if_error: false

      - name: Check Coverage Threshold
        run: |
          # Parse coverage and fail if below 90%
          echo "Coverage check - target: 90%"
```

### 5.2 Frontend CI

```yaml
# .github/workflows/frontend-ci.yml
name: Frontend CI

on:
  push:
    paths: ['currency-converter-web/**']
  pull_request:
    paths: ['currency-converter-web/**']

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup Node
        uses: actions/setup-node@v4
        with:
          node-version: '20'
          cache: 'npm'
          cache-dependency-path: currency-converter-web/package-lock.json

      - name: Install dependencies
        working-directory: currency-converter-web
        run: npm ci

      - name: Lint
        working-directory: currency-converter-web
        run: npm run lint

      - name: Type Check
        working-directory: currency-converter-web
        run: npx tsc --noEmit

      - name: Test
        working-directory: currency-converter-web
        run: npm run test:run

      - name: Build
        working-directory: currency-converter-web
        run: npm run build
```

### 5.3 Docker Build & Push (CD)

```yaml
# .github/workflows/docker-cd.yml
name: Docker Build & Push

on:
  push:
    branches: [main]
    tags: ['v*']

jobs:
  build-api:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: docker/setup-buildx-action@v3
      - uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}
      - uses: docker/build-push-action@v5
        with:
          context: ./currency-converter-api
          push: true
          tags: ghcr.io/${{ github.repository }}/api:${{ github.sha }}

  build-web:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: docker/setup-buildx-action@v3
      - uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}
      - uses: docker/build-push-action@v5
        with:
          context: ./currency-converter-web
          push: true
          tags: ghcr.io/${{ github.repository }}/web:${{ github.sha }}
```

---

## 6. Final README.md

The root `README.md` must include:

### 6.1 Structure

```markdown
# Currency Converter Demo

## Overview
Brief description of the platform.

## Architecture
### System Diagram
### Backend (ASP.NET Core)
### Frontend (React)
### Data Flow

## Setup Instructions
### Prerequisites
### Backend Setup
### Frontend Setup
### Docker Setup
### Running Tests

## API Documentation
### Authentication
### Endpoints Summary
### Example Requests

## Architecture Decisions
### Clean Architecture
### Resilience Patterns
### Caching Strategy
### Security Model

## AI Usage
### How AI Was Used
### What Was Validated Manually
### What Was Not Blindly Accepted

## Testing Strategy
### Backend Coverage
### Frontend Coverage

## Assumptions & Trade-offs

## Potential Future Improvements

## License
```

### 6.2 Key Sections to Write

**AI Usage** (critical for evaluation):
- List specific areas where AI assisted (e.g., "Generated initial test scaffolding", "Suggested Polly v8 resilience pipeline configuration")
- Describe manual review process
- Note design decisions made by the developer
- Be transparent about what AI did and didn't do

**Assumptions & Trade-offs**:
- In-memory cache vs distributed (Redis)
- Hardcoded demo users vs real auth provider
- In-memory pagination of historical rates
- Symmetric JWT key for simplicity
- No database (stateless demo)

**Future Improvements**:
- Redis distributed cache
- OAuth2/OpenID Connect with a real identity provider
- Database for user management
- WebSocket for real-time rate updates
- Rate comparison charts/graphs
- Multi-language support
- API gateway (e.g., Azure API Management)

---

## 7. `.gitignore`

Ensure proper `.gitignore` at root:

```gitignore
# .NET
**/bin/
**/obj/
*.user
*.vs/

# Node
node_modules/
dist/
coverage/

# Environment
.env.local
.env*.local

# IDE
.idea/
.vscode/settings.json

# OS
Thumbs.db
.DS_Store

# Logs
logs/
*.log

# Test results
TestResults/
```

---

## 8. Acceptance Criteria

- [ ] Backend runs correctly in Development, Test, and Production configurations
- [ ] Frontend builds for development and production environments
- [ ] Docker Compose starts both services and they communicate correctly
- [ ] Backend Dockerfile builds and runs successfully
- [ ] Frontend Dockerfile builds and serves the app
- [ ] GitHub Actions CI workflows are defined for both backend and frontend
- [ ] CD workflow builds and pushes Docker images
- [ ] README.md contains all required sections
- [ ] AI Usage section is honest and detailed
- [ ] `.gitignore` covers all generated/sensitive files
- [ ] API versioning works with v1 prefix
- [ ] The application is designed for horizontal scaling (documented)
- [ ] All environment-specific settings are properly separated

---

## 9. Notes for Agent

- Docker files are provided as templates — adjust paths if the actual project structure differs.
- CI/CD YAML files go in `.github/workflows/` at the repository root.
- The README should be **comprehensive but scannable** — use tables, code blocks, and headers.
- The AI Usage section is **mandatory and evaluated** — be specific and honest.
- **Do NOT** include real secrets or passwords in any committed file.
- The `.env` file for the frontend should be in `.gitignore` (only `.env.example` committed).
- Test that `docker-compose up` works end-to-end before marking complete.
- Verify that the health check endpoint (`/health`) works in the containerized API.
