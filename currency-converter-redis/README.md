# Currency Converter - Valkey Cache

This folder contains the Docker Compose setup for **Valkey**, an open-source alternative to Redis, used for distributed caching in the Currency Converter application.

## What is Valkey?

Valkey is a high-performance data structure server that is the open-source successor to Redis, maintained by the Linux Foundation. It provides 100% compatibility with Redis APIs while being fully open source under the BSD license.

## Services

### 1. Valkey Server
- **Image**: `valkey/valkey:latest`
- **Port**: `6379` (standard Redis/Valkey port)
- **Password**: `dev-password-change-in-production` (⚠️ Change for production!)
- **Persistence**: Append-only file (AOF) enabled for data durability
- **Health Check**: Automatic health monitoring

### 2. Redis Commander (UI)
- **Image**: `rediscommander/redis-commander:latest`
- **Port**: `8081`
- **Purpose**: Web-based GUI for viewing and managing cached data
- **Access**: http://localhost:8081

## Quick Start

### Start Services

```bash
docker-compose up -d
```

### Check Status

```bash
docker-compose ps
```

### View Logs

```bash
# All services
docker-compose logs -f

# Valkey only
docker-compose logs -f valkey
```

### Stop Services

```bash
docker-compose down
```

### Stop and Remove Data

```bash
docker-compose down -v
```

## Connection Details

### From Application (Local Development)

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379,password=dev-password-change-in-production,abortConnect=false"
  }
}
```

### From Docker Container (Same Network)

```json
{
  "ConnectionStrings": {
    "Redis": "valkey:6379,password=dev-password-change-in-production,abortConnect=false"
  }
}
```

## Testing the Connection

### Using Valkey CLI (from container)

```bash
# Connect to Valkey
docker exec -it currency-converter-valkey valkey-cli -a dev-password-change-in-production

# Test commands
SET test:key "Hello Valkey"
GET test:key
KEYS *
FLUSHALL
EXIT
```

### Using Redis Commander UI

1. Open http://localhost:8081 in your browser
2. Browse keys, view data, and execute commands
3. Monitor real-time activity

## Data Persistence

- **Method**: Append-Only File (AOF)
- **Sync Strategy**: `everysec` (writes every second)
- **Location**: Docker volume `valkey-data`
- **Backup**: Data persists across container restarts

## Monitoring

### Health Check

```bash
docker inspect --format='{{json .State.Health}}' currency-converter-valkey
```

### Memory Usage

```bash
docker exec currency-converter-valkey valkey-cli -a dev-password-change-in-production INFO memory
```

### Connection Stats

```bash
docker exec currency-converter-valkey valkey-cli -a dev-password-change-in-production INFO clients
```

## Production Considerations

⚠️ **Before deploying to production:**

1. **Change the password** in both the `docker-compose.yml` and application configuration
2. **Enable TLS/SSL** for encrypted connections
3. **Configure proper backups** (AOF + RDB snapshots)
4. **Set resource limits** (memory, CPU)
5. **Use a dedicated network** with firewall rules
6. **Monitor with proper observability tools** (Prometheus, Grafana)
7. **Consider Redis/Valkey cluster mode** for high availability

### Example Production Password Change

```yaml
environment:
  - VALKEY_PASSWORD=${VALKEY_PASSWORD}
command: >
  valkey-server
  --requirepass ${VALKEY_PASSWORD}
  --appendonly yes
```

Then set in `.env` file:
```
VALKEY_PASSWORD=your-strong-production-password-here
```

## Troubleshooting

### Container Won't Start

```bash
# Check logs
docker-compose logs valkey

# Verify port availability
netstat -an | findstr 6379
```

### Connection Refused

1. Verify container is running: `docker-compose ps`
2. Check password matches in application config
3. Ensure firewall allows port 6379

### Data Not Persisting

1. Check volume exists: `docker volume ls`
2. Verify AOF is enabled: `docker exec currency-converter-valkey valkey-cli -a dev-password-change-in-production CONFIG GET appendonly`

## Switching to Production Redis

If you prefer to use Redis instead of Valkey:

1. Change the image in `docker-compose.yml`:
   ```yaml
   image: redis:latest
   ```

2. Update command syntax (redis-server instead of valkey-server)

Both are API-compatible, so no application code changes needed.

## Development vs Production

| Aspect | Development | Production |
|--------|------------|------------|
| Password | Simple | Strong, from env var |
| Persistence | AOF | AOF + RDB snapshots |
| Port Exposure | Host exposed | Private network only |
| UI Tool | Enabled | Disabled (use monitoring) |
| TLS | Disabled | Enabled |
| Resource Limits | None | CPU/Memory limits set |

## References

- [Valkey Official Site](https://valkey.io/)
- [Valkey GitHub](https://github.com/valkey-io/valkey)
- [Valkey Documentation](https://valkey.io/docs/)
- [Redis Commander](https://github.com/joeferner/redis-commander)
