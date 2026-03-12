# Environment Configuration Guide

This document explains how environment variables are configured for different deployment scenarios.

## Quick Reference

| Scenario | Frontend URL | API URL | Frontend Env Var | CORS Setting |
|----------|-------------|---------|------------------|--------------|
| **Local Development** | http://localhost:5173 | http://localhost:5037 | `http://localhost:5037` | `http://localhost:5173` |
| **Docker Compose** | http://localhost:3000 | http://localhost:5000 | `/` (nginx proxy) | `http://localhost:3000` |
| **Production** | https://yourdomain.com | https://api.yourdomain.com | `https://api.yourdomain.com` | `https://yourdomain.com` |

---

## Local Development (Without Docker)

### Backend API
- Runs on **http://localhost:5037** (configured in `launchSettings.json`)
- CORS allows: **http://localhost:5173** (Vite dev server)
- Uses in-memory cache (configured in `appsettings.Development.json`)

**To run:**
```bash
cd currency-converter-api/CurrencyConverterDemo/CurrencyConverterDemo.Api
dotnet run
```

### Frontend
- Vite dev server runs on **http://localhost:5173**
- Uses `.env` or `.env.development`:
  ```bash
  VITE_CURRENCY_CONVERTER_API_URL=http://localhost:5037
  ```

**To run:**
```bash
cd currency-converter-web
npm install
npm run dev
```

---

## Docker Compose (Local Containerized)

### Services
- **Redis**: localhost:6379
- **API**: localhost:5000 → container port 8080
- **Web**: localhost:3000 → container port 80

### Configuration
- API CORS allows: **http://localhost:3000**
- Frontend uses **nginx proxy**: `/api` → `http://api:8080`
- Environment set in `Dockerfile` during build:
  ```bash
  VITE_CURRENCY_CONVERTER_API_URL=/
  ```

**To run:**
```bash
docker-compose up -d
```

**To rebuild after changes:**
```bash
docker-compose down
docker-compose build --no-cache
docker-compose up -d
```

---

## Production Deployment

### Frontend (.env.production)
```bash
VITE_CURRENCY_CONVERTER_API_URL=https://api.yourproductiondomain.com
```

### Backend (docker-compose.yml or environment variables)
```bash
ASPNETCORE_ENVIRONMENT=Production
JwtSettings__Secret=<strong-secret-key-256-bits>
Cors__AllowedOrigin=https://yourproductiondomain.com
CacheSettings__Provider=Redis
CacheSettings__RedisConnectionString=redis:6379,password=<redis-password>
```

---

## Environment Files Reference

### Frontend (`currency-converter-web/`)
- **`.env`** - Local development (port 5037)
- **`.env.development`** - Local development (same as .env)
- **`.env.production`** - Production template (update before deploying)
- **`.env.example`** - Template showing expected format
- **Dockerfile** - Sets `VITE_CURRENCY_CONVERTER_API_URL=/` for Docker build

### Backend (`currency-converter-api/CurrencyConverterDemo/CurrencyConverterDemo.Api/`)
- **`appsettings.json`** - Base configuration
- **`appsettings.Development.json`** - Local dev overrides (CORS: localhost:5173)
- **`appsettings.Production.json`** - Production overrides
- **`launchSettings.json`** - Development ports (5037, 7241)
- **`.env.example`** - Template for secrets

---

## Common Issues

### CORS Errors
- **Local dev**: Ensure `appsettings.Development.json` has `"AllowedOrigin": "http://localhost:5173"`
- **Docker**: Ensure `docker-compose.yml` has `Cors__AllowedOrigin=http://localhost:3000`
- **Production**: Update CORS to match your frontend domain

### Wrong API Port
- **Local dev**: Frontend should call `http://localhost:5037`
- **Docker**: Frontend uses nginx proxy at `/api`, which forwards to `api:8080`

### Browser Caching
After rebuilding Docker images, hard refresh your browser:
- **Windows**: `Ctrl + Shift + R` or `Ctrl + F5`
- **Or**: Open DevTools (F12) → Network tab → Check "Disable cache"

---

## Port Reference

| Service | Local Dev | Docker (Host) | Docker (Container) |
|---------|-----------|---------------|-------------------|
| API HTTP | 5037 | 5000 | 8080 |
| API HTTPS | 7241 | - | - |
| Frontend Dev | 5173 | - | - |
| Frontend Prod | - | 3000 | 80 |
| Redis | 6379 | 6379 | 6379 |
