using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Threading.RateLimiting;

namespace CurrencyConverterDemo.Api.Extensions;

/// <summary>
/// Extension methods for configuring API rate limiting.
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Adds rate limiting policies to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Global fixed window rate limit (applies to all requests)
            options.AddFixedWindowLimiter("fixed", opt =>
            {
                opt.PermitLimit = configuration.GetValue<int>("RateLimiting:Fixed:PermitLimit", 100);
                opt.Window = TimeSpan.FromSeconds(
                    configuration.GetValue<int>("RateLimiting:Fixed:WindowSeconds", 60));
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 5;
            });

            // Per-user sliding window (based on JWT client_id claim)
            options.AddPolicy("per-user", httpContext =>
            {
                var username = httpContext.User.FindFirstValue("client_id")
                    ?? httpContext.User.Identity?.Name
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous";

                return RateLimitPartition.GetSlidingWindowLimiter(username, _ =>
                    new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = configuration.GetValue<int>("RateLimiting:Sliding:PermitLimit", 30),
                        Window = TimeSpan.FromSeconds(
                            configuration.GetValue<int>("RateLimiting:Sliding:WindowSeconds", 60)),
                        SegmentsPerWindow = configuration.GetValue<int>("RateLimiting:Sliding:SegmentsPerWindow", 6),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 2
                    });
            });

            // Auth endpoint rate limit (stricter to prevent brute force)
            options.AddPolicy("auth", httpContext =>
            {
                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ =>
                    new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = configuration.GetValue<int>("RateLimiting:Auth:PermitLimit", 5),
                        Window = TimeSpan.FromMinutes(
                            configuration.GetValue<int>("RateLimiting:Auth:WindowMinutes", 5)),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0  // No queuing for auth endpoints
                    });
            });

            // Custom response for rate limit exceeded
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.ContentType = "application/problem+json";

                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too Many Requests",
                    Detail = "Rate limit exceeded. Please try again later.",
                    Instance = context.HttpContext.Request.Path
                };

                // Add Retry-After header if available
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    var retryAfterSeconds = (int)retryAfter.TotalSeconds;
                    context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString();
                    problem.Detail += $" Retry after {retryAfterSeconds} seconds.";

                    // Add extension data
                    problem.Extensions["retryAfter"] = retryAfterSeconds;
                }

                await context.HttpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
            };
        });

        return services;
    }
}
