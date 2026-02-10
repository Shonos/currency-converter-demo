# Sub-Task 10: Deployment & Containerization Documentation

> **Context**: Use with `00-master.copilot.md`. **Depends on**: All previous sub-tasks should be complete.

---

## Objective

Configure multi-environment support, finalize API versioning, create production-ready Docker configurations with multi-stage builds for testing, building, and runtime deployment, and write comprehensive documentation. The Docker setup will be cloud-agnostic and ready for deployment to AWS ECS, EC2, GCP, Azure App Service, or any container orchestration platform.

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

## 4. Docker Configuration Strategy

### 4.1 Multi-Stage Docker Approach

We'll use **multi-stage Dockerfiles** with named build targets for maximum flexibility:

1. **Build Stage**: Restore dependencies and compile the application
2. **Test Stage**: Run unit and integration tests (optional target)
3. **Runtime Stage**: Create minimal production image with only runtime dependencies

This approach allows:
- **Local Development**: Build and test using `--target test`
- **CI/CD Validation**: Run tests in containerized environment
- **Production Deployment**: Deploy optimized runtime image to any cloud provider (AWS, GCP, Azure, etc.)
- **Single Source of Truth**: One Dockerfile per service, multiple use cases

---

### 4.2 Backend Multi-Stage Dockerfile

```dockerfile
# currency-converter-api/Dockerfile
# Stage 1: Base - SDK for building and testing
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS base
WORKDIR /src

# Stage 2: Restore - Restore dependencies separately for better caching
FROM base AS restore
COPY CurrencyConverterDemo/*.slnx ./
COPY CurrencyConverterDemo/CurrencyConverterDemo.Api/*.csproj CurrencyConverterDemo.Api/
COPY CurrencyConverterDemo/CurrencyConverterDemo.Application/*.csproj CurrencyConverterDemo.Application/
COPY CurrencyConverterDemo/CurrencyConverterDemo.Domain/*.csproj CurrencyConverterDemo.Domain/
COPY CurrencyConverterDemo/CurrencyConverterDemo.Infrastructure/*.csproj CurrencyConverterDemo.Infrastructure/
COPY CurrencyConverterDemo/CurrencyConverterDemo.Tests/*.csproj CurrencyConverterDemo.Tests/

RUN dotnet restore

# Stage 3: Build - Compile the application
FROM restore AS build
COPY CurrencyConverterDemo/ ./
RUN dotnet build CurrencyConverterDemo.Api -c Release --no-restore

# Stage 4: Test - Run tests (optional target)
FROM build AS test
RUN dotnet test CurrencyConverterDemo.Tests \
    -c Release \
    --no-build \
    --verbosity normal \
    --logger "trx;LogFileName=test-results.trx" \
    /p:CollectCoverage=true \
    /p:CoverletOutputFormat=cobertura \
    /p:Threshold=80

# Stage 5: Publish - Create publish output
FROM build AS publish
RUN dotnet publish CurrencyConverterDemo.Api \
    -c Release \
    -o /app/publish \
    --no-build \
    --no-restore

# Stage 6: Runtime - Final minimal production image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN groupadd -r appuser && useradd -r -g appuser appuser

# Copy published application
COPY --from=publish /app/publish .

# Create logs directory with proper permissions
RUN mkdir -p /var/log/currency-converter && \
    chown -R appuser:appuser /var/log/currency-converter

# Switch to non-root user
USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "CurrencyConverterDemo.Api.dll"]
```

**Usage Examples:**

```bash
# Build and run tests
docker build --target test -t currency-api:test .

# Build production image
docker build --target runtime -t currency-api:latest .

# Build with specific tag
docker build --target runtime -t currency-api:v1.0.0 .

# Run production container
docker run -d -p 5000:8080 \
  -e JwtSettings__Secret="your-secret-key" \
  -e ASPNETCORE_ENVIRONMENT=Production \
  --name currency-api \
  currency-api:latest
```

---

### 4.3 Frontend Multi-Stage Dockerfile

```dockerfile
# currency-converter-web/Dockerfile
# Stage 1: Base - Node.js for building
FROM node:20-alpine AS base
WORKDIR /app

# Stage 2: Dependencies - Install dependencies
FROM base AS dependencies
COPY package*.json ./
RUN npm ci --only=production && \
    cp -R node_modules /prod_node_modules && \
    npm ci

# Stage 3: Build - Build the application
FROM dependencies AS build
COPY . .
RUN npm run build

# Stage 4: Test - Run tests (optional target)
FROM dependencies AS test
COPY . .
RUN npm run lint && \
    npm run test:run && \
    npm run build

# Stage 5: Runtime - Nginx for serving
FROM nginx:alpine AS runtime

# Copy custom nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy built application
COPY --from=build /app/dist /usr/share/nginx/html

# Add healthcheck
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD wget --quiet --tries=1 --spider http://localhost/health || exit 1

EXPOSE 80

# Add labels for better container management
LABEL org.opencontainers.image.title="Currency Converter Web"
LABEL org.opencontainers.image.description="React frontend for Currency Converter"

CMD ["nginx", "-g", "daemon off;"]
```

**Frontend nginx.conf:**

```nginx
# currency-converter-web/nginx.conf
server {
    listen 80;
    server_name _;
    root /usr/share/nginx/html;
    index index.html;

    # Gzip compression
    gzip on;
    gzip_types text/plain text/css application/json application/javascript text/xml application/xml application/xml+rss text/javascript;
    gzip_min_length 1000;

    # Security headers
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;

    # Cache static assets
    location ~* \.(js|css|png|jpg|jpeg|gif|ico|svg|woff|woff2|ttf|eot)$ {
        expires 1y;
        add_header Cache-Control "public, immutable";
    }

    # Health check endpoint
    location /health {
        access_log off;
        return 200 "healthy\n";
        add_header Content-Type text/plain;
    }

    # SPA routing - all routes serve index.html
    location / {
        try_files $uri $uri/ /index.html;
    }

    # API proxy (optional - if same domain needed)
    location /api {
        proxy_pass http://api:8080;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

**Usage Examples:**

```bash
# Build and run tests
docker build --target test -t currency-web:test .

# Build production image
docker build --target runtime -t currency-web:latest .

# Run production container
docker run -d -p 3000:80 \
  --name currency-web \
  currency-web:latest
```

---

### 4.4 Docker Compose for Local Development and Testing

```yaml
# docker-compose.yml (root)
version: '3.8'

services:
  # Redis for distributed caching (optional but recommended for production)
  redis:
    image: redis:7-alpine
    container_name: currency-redis
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data
    command: redis-server --appendonly yes
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 3s
      retries: 3
    networks:
      - currency-network

  # Backend API
  api:
    build:
      context: ./currency-converter-api
      dockerfile: Dockerfile
      target: runtime
    container_name: currency-api
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - JwtSettings__Secret=${JWT_SECRET:-DevSecret-minimum-32-characters-long-key-here!!}
      - Cors__AllowedOrigin=http://localhost:3000
      - CacheSettings__Provider=Redis
      - CacheSettings__RedisConnectionString=redis:6379
      - CacheSettings__AbsoluteExpirationMinutes=60
    depends_on:
      redis:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 10s
    networks:
      - currency-network
    restart: unless-stopped

  # Frontend Web
  web:
    build:
      context: ./currency-converter-web
      dockerfile: Dockerfile
      target: runtime
    container_name: currency-web
    ports:
      - "3000:80"
    depends_on:
      api:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "wget", "--quiet", "--tries=1", "--spider", "http://localhost/health"]
      interval: 30s
      timeout: 10s
      retries: 3
    networks:
      - currency-network
    restart: unless-stopped

volumes:
  redis-data:
    driver: local

networks:
  currency-network:
    driver: bridge
```

**Docker Compose Commands:**

```bash
# Build all images
docker-compose build

# Build and run tests before starting
docker-compose build --target test

# Start all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop all services
docker-compose down

# Stop and remove volumes
docker-compose down -v

# Restart a specific service
docker-compose restart api
```

---

### 4.5 Docker Compose Override for Testing

```yaml
# docker-compose.test.yml
version: '3.8'

services:
  api:
    build:
      target: test
    environment:
      - ASPNETCORE_ENVIRONMENT=Test
    command: dotnet test --no-build --verbosity normal

  web:
    build:
      target: test
    command: npm run test:run
```

**Usage:**

```bash
# Run tests in Docker
docker-compose -f docker-compose.yml -f docker-compose.test.yml up --abort-on-container-exit

# Run only API tests
docker-compose -f docker-compose.test.yml up api

# Run only Web tests
docker-compose -f docker-compose.test.yml up web
```

---

### 4.6 Deployment-Ready Docker Images

The multi-stage approach produces optimized images:

**Backend API:**
- Build stage: ~2GB (includes SDK)
- Test stage: ~2GB (includes test frameworks)
- **Runtime stage: ~200MB** (only ASP.NET runtime)

**Frontend Web:**
- Build stage: ~500MB (includes Node.js and build tools)
- Test stage: ~500MB (includes test dependencies)
- **Runtime stage: ~50MB** (only Nginx + static files)

---

### 4.7 Cloud Deployment Readiness

These Dockerfiles are ready for deployment to:

**AWS:**
- **ECS (Fargate/EC2)**: Push images to ECR, create task definitions
- **EC2**: Pull images and run with docker-compose or standalone
- **App Runner**: Direct deployment from container registry
- **EKS**: Deploy with Kubernetes manifests

**Google Cloud:**
- **Cloud Run**: Serverless container deployment
- **GKE**: Kubernetes deployment
- **Compute Engine**: VM with Docker

**Azure:**
- **Container Instances**: Simple container deployment
- **App Service**: Container-based web apps
- **AKS**: Azure Kubernetes Service

**General Container Platforms:**
- **Kubernetes**: Any managed or self-hosted cluster
- **Docker Swarm**: Native Docker orchestration
- **Nomad**: HashiCorp's orchestrator

---

## 5. Container Registry & Environment Configuration

### 5.1 Image Tagging Convention

```bash
# Local development
currency-api:latest
currency-web:latest

# Versioned for registry
<registry>/<repository>:<version>
# Examples:
ghcr.io/yourorg/currency-api:1.0.0
123456789.dkr.ecr.us-east-1.amazonaws.com/currency-api:1.0.0
```

### 5.2 Environment Variables

**Root .env file (for docker-compose):**
```env
# Redis Password (shared between Redis and API)
REDIS_PASSWORD=dev-password-change-in-production

# JWT Secret (minimum 32 characters)
JWT_SECRET=DevSecret-minimum-32-characters-long-key-here!!
```

**Backend (.env in api directory or environment-specific config):**
```env
JWT_SECRET=your-secret-key-minimum-32-characters-long
CORS_ALLOWED_ORIGIN=http://localhost:3000
CACHE_PROVIDER=Redis
REDIS_CONNECTION=localhost:6379,password=dev-password-change-in-production
CACHE_ABSOLUTE_EXPIRATION_MINUTES=60
ASPNETCORE_ENVIRONMENT=Production
```

**Frontend (.env):**
```env
VITE_API_BASE_URL=http://localhost:5000/api
```

**Important:** The Redis password must match in:
- Root `.env` file (`REDIS_PASSWORD`)
- API `.env` file (in `REDIS_CONNECTION` string)
- `currency-converter-redis/.env` file (`VALKEY_PASSWORD`)

---

## 6. Quick Reference

### Setup
```bash
# Create environment file from example
cp .env.example .env
# Edit .env and update passwords and secrets

# Or manually create .env with:
# REDIS_PASSWORD=dev-password-change-in-production
# JWT_SECRET=DevSecret-minimum-32-characters-long-key-here!!
```

### Build & Run
```bash
# Build images
docker build -t currency-api:latest ./currency-converter-api
docker build -t currency-web:latest ./currency-converter-web

# Run all services locally
docker-compose up -d

# Run tests in Docker
docker-compose -f docker-compose.yml -f docker-compose.test.yml up --abort-on-container-exit

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

### Access Points
- Frontend: http://localhost:3000
- Backend API: http://localhost:5000
- API Docs: http://localhost:5000/swagger (development)
- Redis: localhost:6379

### Image Optimization
- Backend runtime: ~200MB ( from ~2GB build image)
- Frontend runtime: ~50MB (from ~500MB build image)

---
