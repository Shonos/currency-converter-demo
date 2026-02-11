using CurrencyConverterDemo.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.IdentityModel.Tokens.Jwt;

namespace CurrencyConverterDemo.Api.Filters;

/// <summary>
/// Action filter that checks if the current JWT token has been blacklisted (revoked).
/// Runs after authentication but before authorization.
/// </summary>
public class TokenBlacklistFilter : IAsyncActionFilter
{
    private readonly ITokenBlacklistService _blacklistService;
    private readonly ILogger<TokenBlacklistFilter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenBlacklistFilter"/> class.
    /// </summary>
    /// <param name="blacklistService">Token blacklist service.</param>
    /// <param name="logger">Logger instance.</param>
    public TokenBlacklistFilter(
        ITokenBlacklistService blacklistService,
        ILogger<TokenBlacklistFilter> logger)
    {
        _blacklistService = blacklistService ?? throw new ArgumentNullException(nameof(blacklistService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
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
            _logger.LogWarning("Token missing jti claim for user {User}", 
                context.HttpContext.User.Identity.Name);
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
