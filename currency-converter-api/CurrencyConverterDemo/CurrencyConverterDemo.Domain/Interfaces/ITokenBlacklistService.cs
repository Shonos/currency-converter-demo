namespace CurrencyConverterDemo.Domain.Interfaces;

/// <summary>
/// Service for managing token blacklist (revocation).
/// </summary>
public interface ITokenBlacklistService
{
    /// <summary>
    /// Adds a token to the blacklist.
    /// </summary>
    /// <param name="jti">The JWT ID (jti claim) to blacklist.</param>
    /// <param name="remainingLifetime">How long until the token expires naturally.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task BlacklistTokenAsync(string jti, TimeSpan remainingLifetime);

    /// <summary>
    /// Checks if a token is blacklisted.
    /// </summary>
    /// <param name="jti">The JWT ID (jti claim) to check.</param>
    /// <returns>True if blacklisted, false otherwise.</returns>
    Task<bool> IsTokenBlacklistedAsync(string jti);
}
