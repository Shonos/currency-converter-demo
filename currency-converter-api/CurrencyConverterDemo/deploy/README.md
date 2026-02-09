# Deployment Guide

This folder contains deployment configurations for the Currency Converter Platform.

## Environments

- **Dev**: Local development with in-memory cache and self-signed certificates.
- **Test**: Testing environment with relaxed rate limits and debug logging.
- **Prod**: Production environment with Redis caching, JWT secrets from environment variables, and restricted hosts.

## Configuration

Use environment variables for secrets in production:

- `JWT_KEY`: JWT signing key
- `REDIS_CONNECTION`: Redis connection string

## Kubernetes (Optional)

If deploying to Kubernetes, add Helm charts or YAML manifests here.

Example Helm chart structure:
- `templates/`: Kubernetes templates
- `values.yaml`: Configuration values
- `Chart.yaml`: Chart metadata

## Docker

Build the API with:
```bash
docker build -t currency-converter-api .
```

Run with environment variables set.