namespace CurrencyConverterDemo.Api.Models;

/// <summary>
/// Configuration for demo users (testing only).
/// </summary>
public class DemoUserSettings
{
    /// <summary>
    /// Gets or sets the demo users in format: username:password:role|username:password:role
    /// </summary>
    public string Users { get; set; } = string.Empty;

    /// <summary>
    /// Parses the users string into a dictionary.
    /// </summary>
    /// <returns>Dictionary of username to (password, role).</returns>
    public Dictionary<string, (string Password, string Role)> ParseUsers()
    {
        if (string.IsNullOrWhiteSpace(Users))
        {
            // Return default demo users if not configured
            return new Dictionary<string, (string Password, string Role)>
            {
                ["admin"] = ("Admin123!", "Admin"),
                ["user"] = ("User123!", "User"),
                ["viewer"] = ("Viewer123!", "Viewer")
            };
        }

        var users = new Dictionary<string, (string Password, string Role)>();
        
        foreach (var userEntry in Users.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = userEntry.Split(':', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 3)
            {
                users[parts[0]] = (parts[1], parts[2]);
            }
        }

        return users;
    }
}
