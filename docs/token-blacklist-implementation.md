# Token Blacklist Implementation Specification

## Overview

This document specifies the implementation of server-side token invalidation (blacklist) to support true logout functionality. Currently, tokens remain valid after logout until their natural expiration. This enhancement uses Redis to maintain a blacklist of invalidated tokens.

---

## Motivation

**Current Problem:**
- Logout is client-side only (removes token from localStorage)
- JWT tokens are stateless and remain valid until expiration
- Users can reuse copied tokens after logout
- No immediate way to revoke a compromised token

**Solution:**
- Implement Redis-backed token blacklist
- Add server-side logout endpoint
- Check blacklist on every authenticated request
- Tokens expire from blacklist automatically via Redis TTL

---

## Architecture

### Flow Diagram

```
┌─────────────┐                    ┌──────────────┐                    ┌───────────┐
│   Client    │                    │   API Server │                    │   Redis   │
└──────┬──────┘                    └──────┬───────┘                    └─────┬─────┘
       │                                  │                                   │
       │  POST /auth/logout               │                                   │
       │  Authorization: Bearer {token}   │                                   │
       ├─────────────────────────────────>│                                   │
       │                                  │                                   │
       │                                  │  Extract jti from token           │
       │                                  │  Calculate remaining TTL          │
       │                                  │                                   │
       │                                  │  SET blacklist:{jti} EX {ttl}    │
       │                                  ├──────────────────────────────────>│
       │                                  │                                   │
       │                                  │  OK                               │
       │                                  │<──────────────────────────────────┤
       │                                  │                                   │
       │  200 OK                          │                                   │
       │<─────────────────────────────────┤                                   │
       │                                  │                                   │
       │  Client removes token            │                                   │
       │  from localStorage               │                                   │
       │                                  │                                   │
       │                                  │                                   │
       │  GET /exchange-rates/latest      │                                   │
       │  Authorization: Bearer {token}   │                                   │
       ├─────────────────────────────────>│                                   │
       │                                  │                                   │
       │                                  │  Validate JWT signature           │
       │                                  │  Extract jti                      │
       │                                  │                                   │
       │                                  │  EXISTS blacklist:{jti}           │
       │                                  ├──────────────────────────────────>│
       │                                  │                                   │
       │                                  │  1 (exists)                       │
       │                                  │<──────────────────────────────────┤
       │                                  │                                   │
       │  401 Unauthorized                │                                   │
       │  Token has been revoked          │                                   │
       │<─────────────────────────────────┤                                   │
       │                                  │                                   │
```

### Key Components

1. **Token Blacklist Service** — Manages blacklist operations (add, check)
2. **Logout Endpoint** — Adds token to blacklist
3. **Blacklist Validation Filter** — Checks incoming requests against blacklist
4. **Redis Cache** — Stores blacklisted token IDs with TTL

---

## Technical Specification

### 1. Backend Implementation

#### 1.1 Token Blacklist Service

**Location:** `CurrencyConverterDemo.Infrastructure/Caching/TokenBlacklistService.cs`

**Interface:** `CurrencyConverterDemo.Domain/Interfaces/ITokenBlacklistService.cs`

```csharp
public interface ITokenBlacklistService
{
    /// <summary>
    /// Adds a token to the blacklist.
    /// </summary>
    /// <param name="jti">The JWT ID (jti claim) to blacklist.</param>
    /// <param name="remainingLifetime">How long until the token expires naturally.</param>
    Task BlacklistTokenAsync(string jti, TimeSpan remainingLifetime);

    /// <summary>
    /// Checks if a token is blacklisted.
    /// </summary>
    /// <param name="jti">The JWT ID (jti claim) to check.</param>
    /// <returns>True if blacklisted, false otherwise.</returns>
    Task<bool> IsTokenBlacklistedAsync(string jti);
}
```

**Implementation Details:**
- Use Redis key pattern: `blacklist:{jti}`
- Store value: `"revoked"` or `"1"` (actual value doesn't matter)
- Set Redis TTL = token's remaining lifetime
- Use `IDistributedCache` or `IConnectionMultiplexer` (StackExchange.Redis)
- Fallback: If Redis unavailable, log error and allow request (fail-open for availability)

**Redis Commands:**
```redis
# Add to blacklist
SET blacklist:{jti} 1 EX {seconds_until_expiration}

# Check blacklist
EXISTS blacklist:{jti}
```

#### 1.2 Logout Endpoint

**Location:** `CurrencyConverterDemo.Api/Controllers/v1/AuthController.cs`

**Endpoint:**
```
POST /api/v1/auth/logout
Authorization: Bearer {token}
```

**Request:** None (token in header)

**Response:**
```json
{
  "message": "Logout successful",
  "tokenRevoked": true
}
```

**Implementation:**
```csharp
[HttpPost("logout")]
[Authorize]
[ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> Logout()
{
    // Extract jti claim from current user's token
    var jtiClaim = User.FindFirst(JwtRegisteredClaimNames.Jti);
    if (jtiClaim == null)
    {
        return BadRequest(new ProblemDetails
        {
            Title = "Invalid Token",
            Detail = "Token does not contain a valid JWT ID (jti).",
            Status = StatusCodes.Status400BadRequest
        });
    }

    // Extract expiration to calculate remaining lifetime
    var expClaim = User.FindFirst(JwtRegisteredClaimNames.Exp);
    if (expClaim == null || !long.TryParse(expClaim.Value, out var exp))
    {
        return BadRequest(new ProblemDetails
        {
            Title = "Invalid Token",
            Detail = "Token does not contain a valid expiration (exp).",
            Status = StatusCodes.Status400BadRequest
        });
    }

    var expirationTime = DateTimeOffset.FromUnixTimeSeconds(exp);
    var remainingLifetime = expirationTime - DateTimeOffset.UtcNow;

    // If token already expired, no need to blacklist
    if (remainingLifetime <= TimeSpan.Zero)
    {
        return Ok(new { message = "Logout successful", tokenRevoked = false });
    }

    // Add to blacklist
    await _tokenBlacklistService.BlacklistTokenAsync(jtiClaim.Value, remainingLifetime);

    return Ok(new { message = "Logout successful", tokenRevoked = true });
}
```

#### 1.3 Blacklist Validation Filter

**Location:** `CurrencyConverterDemo.Api/Filters/TokenBlacklistFilter.cs`

**Type:** Action Filter (runs after authentication, before authorization)

```csharp
public class TokenBlacklistFilter : IAsyncActionFilter
{
    private readonly ITokenBlacklistService _blacklistService;
    private readonly ILogger<TokenBlacklistFilter> _logger;

    public TokenBlacklistFilter(
        ITokenBlacklistService blacklistService,
        ILogger<TokenBlacklistFilter> logger)
    {
        _blacklistService = blacklistService;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        // Skip if user is not authenticated
        if (context.HttpContext.User?.Identity?.IsAuthenticated != true)
        {
            await next();
            return;
        }

        // Extract jti claim
        var jtiClaim = context.HttpContext.User.FindFirst(JwtRegisteredClaimNames.Jti);
        if (jtiClaim == null)
        {
            // Token doesn't have jti — allow for backward compatibility
            _logger.LogWarning("Token missing jti claim");
            await next();
            return;
        }

        // Check blacklist
        var isBlacklisted = await _blacklistService.IsTokenBlacklistedAsync(jtiClaim.Value);
        if (isBlacklisted)
        {
            _logger.LogWarning(
                "Blacklisted token attempted access. JTI: {Jti}, User: {User}",
                jtiClaim.Value,
                context.HttpContext.User.Identity.Name);

            context.Result = new UnauthorizedObjectResult(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Token Revoked",
                Detail = "This token has been revoked. Please log in again.",
                Instance = context.HttpContext.Request.Path
            });
            return;
        }

        // Token is valid, continue
        await next();
    }
}
```

**Registration in Program.cs:**
```csharp
builder.Services.AddControllers(options =>
{
    options.Filters.Add<TokenBlacklistFilter>();
});
```

#### 1.4 Dependency Injection Registration

**Location:** `CurrencyConverterDemo.Infrastructure/Extensions/InfrastructureServiceExtensions.cs`

```csharp
// Register token blacklist service
if (cacheType.Equals("Distributed", StringComparison.OrdinalIgnoreCase) && 
    !string.IsNullOrEmpty(redisConnection))
{
    services.AddSingleton<ITokenBlacklistService, RedisTokenBlacklistService>();
}
else
{
    // Fallback to in-memory blacklist for development
    services.AddSingleton<ITokenBlacklistService, InMemoryTokenBlacklistService>();
}
```

#### 1.5 Configuration

**appsettings.json** — No changes needed (uses existing Redis connection)

**appsettings.Development.json** — Can use in-memory blacklist or Redis if available

**appsettings.Production.json** — Must use Redis-backed blacklist

---

### 2. Frontend Implementation

#### 2.1 Logout API Call

**Location:** `currency-converter-web/src/api/auth.ts`

```typescript
export const logout = async (): Promise<void> => {
  await apiClient.post('/auth/logout');
};
```

#### 2.2 Update AuthContext

**Location:** `currency-converter-web/src/context/AuthContext.tsx`

```typescript
const logout = async () => {
  try {
    // Call server-side logout to blacklist token
    await apiLogout();
  } catch (error) {
    // Log error but still clear local storage
    console.error('Server logout failed:', error);
  } finally {
    // Clear client-side state regardless of server response
    setToken(null);
    setRole(null);
    localStorage.removeItem('token');
    localStorage.removeItem('role');
    toast.success('Logged out successfully');
  }
};
```

**Behavior:**
- Always clear client-side state (even if server call fails)
- Toast shows success message regardless
- Network error doesn't block logout UX
- Token still blacklisted on server if call succeeds

#### 2.3 Handle 401 "Token Revoked" Response

**Location:** `currency-converter-web/src/api/client.ts`

Already handled by existing 401 interceptor — redirects to login.

---

### 3. Testing Strategy

#### 3.1 Backend Unit Tests

**Location:** `CurrencyConverterDemo.Tests/Unit/Services/TokenBlacklistServiceTests.cs`

Test cases:
- ✅ `BlacklistTokenAsync_AddsToRedis_WithCorrectTTL`
- ✅ `IsTokenBlacklistedAsync_ReturnsTrue_WhenTokenBlacklisted`
- ✅ `IsTokenBlacklistedAsync_ReturnsFalse_WhenTokenNotBlacklisted`
- ✅ `IsTokenBlacklistedAsync_ReturnsFalse_WhenRedisUnavailable` (fail-open)
- ✅ `BlacklistTokenAsync_HandlesExpiredToken_Gracefully`

#### 3.2 Backend Integration Tests

**Location:** `CurrencyConverterDemo.Tests/Integration/AuthControllerTests.cs`

Test cases:
- ✅ `Logout_BlacklistsToken_AndReturns200`
- ✅ `Logout_RequiresAuthentication`
- ✅ `Logout_WithExpiredToken_Returns200_WithoutBlacklisting`
- ✅ `AuthenticatedRequest_WithBlacklistedToken_Returns401`
- ✅ `AuthenticatedRequest_AfterLogout_Returns401`

#### 3.3 Frontend Unit Tests

**Location:** `currency-converter-web/src/context/AuthContext.test.tsx`

Test cases:
- ✅ `logout_CallsServerLogoutEndpoint`
- ✅ `logout_ClearsLocalStorage_EvenIfServerFails`
- ✅ `logout_ShowsSuccessToast`

#### 3.4 End-to-End Test Scenarios

1. **Happy Path:**
   - Login → Get token → Logout → Verify 401 on subsequent request

2. **Token Reuse:**
   - Login → Copy token → Logout → Try reusing token → Verify 401

3. **Network Failure:**
   - Login → Disconnect network → Logout → Verify client clears state

4. **Redis Unavailable:**
   - Login → Stop Redis → Logout → Verify graceful degradation

---

## Configuration

### Redis Key Pattern

```
blacklist:{jti}
```

**Example:**
```
blacklist:3fa85f64-5717-4562-b3fc-2c963f66afa6
```

### TTL Strategy

**Formula:**
```
TTL = token_exp_timestamp - current_timestamp
```

**Example:**
- Token issued at: `2026-02-11T10:00:00Z`
- Token expires at: `2026-02-11T11:00:00Z` (60 min)
- User logs out at: `2026-02-11T10:30:00Z`
- TTL = `1800 seconds` (30 minutes remaining)

**Why this matters:**
- No need to manually clean up blacklist
- Redis automatically removes expired entries
- Minimal memory footprint

---

## Performance Considerations

### Redis Lookup Cost

**Per Authenticated Request:**
- 1 additional Redis `EXISTS` command
- Latency: ~0.5-2ms (same datacenter)
- Negligible compared to typical API processing time (50-200ms)

**Optimization:**
- Use Redis pipelining/batching if checking multiple tokens
- Consider caching blacklist checks in-memory for 1-2 seconds (optional)

### Memory Usage

**Per Blacklisted Token:**
- Key: `blacklist:{UUID}` ≈ 50 bytes
- Value: `1` ≈ 1 byte
- Total: ~51 bytes per token

**Estimate for 1,000 concurrent logouts/hour:**
- Memory: ~51 KB
- Negligible Redis memory impact

---

## Security Considerations

### 1. Race Conditions

**Scenario:** User logs out while another request is in-flight

**Mitigation:**
- Blacklist check happens on every request
- In-flight requests complete normally
- Next request with same token is rejected

### 2. Redis Unavailability

**Strategy:** Fail-open (allow requests if Redis is down)

**Rationale:**
- Availability > Security for this demo
- Production: Would need circuit breaker + monitoring

**Alternative:** Fail-closed (reject all requests if Redis down)

### 3. Token Expiration Edge Cases

**Scenario:** Token expires during logout call

**Handling:**
- Check `remainingLifetime <= 0` before blacklisting
- Return success but don't add to Redis (already expired)

### 4. JTI Claim Requirement

**Current:** TokenService already generates unique `jti` per token

**Verification:** Ensure all tokens have `jti` claim

**Migration:** Tokens without `jti` are allowed (backward compatibility)

---

## Deployment Strategy

### Phase 1: Backend Implementation (No Behavior Change)

1. Add `ITokenBlacklistService` interface and implementations
2. Register services in DI (but don't use yet)
3. Deploy and verify no impact

### Phase 2: Add Logout Endpoint (Optional)

1. Add `POST /auth/logout` endpoint
2. Users can call it, but no enforcement yet
3. Deploy and verify endpoint works

### Phase 3: Enable Blacklist Validation (Breaking Change)

1. Add `TokenBlacklistFilter`
2. Register filter globally
3. Deploy with monitoring
4. Tokens ARE now invalidated on logout

### Phase 4: Frontend Integration

1. Update frontend logout to call backend endpoint
2. Deploy frontend
3. Users experience true logout

### Rollback Plan

Remove `TokenBlacklistFilter` from global filters to disable enforcement while keeping infrastructure in place.

---

## Alternative Approaches Considered

### 1. Database-Backed Blacklist

**Pros:**
- No Redis dependency
- Persistent across restarts

**Cons:**
- Slower (DB query on every request)
- More complex cleanup logic
- Database load increases significantly

**Verdict:** Redis is better fit for this use case

### 2. Short-Lived Tokens + Refresh Tokens

**Pros:**
- Industry best practice
- Reduces blacklist size (only blacklist refresh tokens)

**Cons:**
- Major architectural change
- Requires refresh token storage and rotation
- Out of scope for this task

**Verdict:** Future enhancement

### 3. Server-Side Sessions

**Pros:**
- True stateful auth
- No blacklist needed

**Cons:**
- Breaks stateless API design
- Doesn't scale horizontally without session store
- Contradicts JWT approach

**Verdict:** Not aligned with architecture

---

## Open Questions

1. **Fail-open vs fail-closed when Redis unavailable?**
   - Recommendation: Fail-open for demo, fail-closed for production

2. **Should logout require confirmation?**
   - Recommendation: No confirmation (standard UX)

3. **What about "logout all devices"?**
   - Recommendation: Out of scope (requires tracking all tokens per user)

4. **Rate limiting on logout endpoint?**
   - Recommendation: Yes, reuse existing `auth` rate limit policy

---

## Success Criteria

✅ **Functional:**
- Logout endpoint blacklists token
- Blacklisted tokens return 401
- Non-blacklisted tokens continue to work
- Frontend integration completes logout flow

✅ **Non-Functional:**
- Redis lookup adds < 5ms to request latency
- No memory leaks (Redis TTL cleanup works)
- Graceful handling of Redis unavailability

✅ **Testing:**
- All unit tests pass
- Integration tests cover logout scenarios
- E2E test validates token reuse fails

---

## Timeline Estimate

| Phase | Effort | Dependencies |
|-------|--------|--------------|
| Backend service implementation | 2 hours | Redis connection working |
| Logout endpoint | 30 min | Backend service |
| Blacklist filter | 1 hour | Backend service |
| Unit tests | 1.5 hours | All backend done |
| Integration tests | 1 hour | All backend done |
| Frontend API integration | 30 min | Logout endpoint deployed |
| Frontend context update | 30 min | Frontend API |
| Frontend tests | 30 min | Frontend done |
| Documentation updates | 30 min | All done |
| **Total** | **~8 hours** | |

---

## References

- JWT RFC 7519: https://datatracker.ietf.org/doc/html/rfc7519
- Redis TTL Commands: https://redis.io/commands/expire/
- ASP.NET Core Action Filters: https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/filters
- Token Revocation Best Practices: https://datatracker.ietf.org/doc/html/rfc7009
