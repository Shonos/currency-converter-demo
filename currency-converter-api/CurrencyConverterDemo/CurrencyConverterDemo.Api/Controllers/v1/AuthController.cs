using Asp.Versioning;
using CurrencyConverterDemo.Api.Models;
using CurrencyConverterDemo.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace CurrencyConverterDemo.Api.Controllers.v1;

/// <summary>
/// Handles authentication and token generation.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[Produces("application/json")]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly Dictionary<string, (string Password, string Role)> _demoUsers;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    /// <param name="tokenService">Token generation service.</param>
    /// <param name="demoUserSettings">Demo user configuration.</param>
    public AuthController(
        ITokenService tokenService,
        IOptions<DemoUserSettings> demoUserSettings)
    {
        _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        _demoUsers = demoUserSettings?.Value?.ParseUsers() ?? new Dictionary<string, (string Password, string Role)>();
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    /// <param name="request">Login credentials.</param>
    /// <returns>JWT token and expiration details.</returns>
    /// <response code="200">Login successful, returns JWT token.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="401">Invalid credentials.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Validate credentials against demo users
        if (!_demoUsers.TryGetValue(request.Username, out var userInfo) ||
            userInfo.Password != request.Password)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Authentication Failed",
                Detail = "Invalid username or password.",
                Instance = HttpContext.Request.Path
            });
        }

        // Generate JWT token
        var response = _tokenService.GenerateToken(request.Username, userInfo.Role);

        return Ok(response);
    }

    /// <summary>
    /// Gets information about available demo users (for testing purposes only).
    /// </summary>
    /// <returns>List of demo usernames and their roles.</returns>
    /// <remarks>
    /// This endpoint is for demonstration purposes only and should be removed in production.
    /// 
    /// Demo credentials:
    /// - Username: admin, Password: Admin123! (Role: Admin)
    /// - Username: user, Password: User123! (Role: User)
    /// - Username: viewer, Password: Viewer123! (Role: Viewer)
    /// </remarks>
    /// <response code="200">Returns demo user information.</response>
    [HttpGet("demo-users")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<object> GetDemoUsers()
    {
        var users = _demoUsers.Select(u => new
        {
            Username = u.Key,
            Role = u.Value.Role,
            Password = "***" // Don't expose passwords in real scenarios
        });

        return Ok(new
        {
            Message = "Demo users for testing (use /auth/login to get a token)",
            Users = users,
            Note = "In production, this endpoint should not exist."
        });
    }
}
