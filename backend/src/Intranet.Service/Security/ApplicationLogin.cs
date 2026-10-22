using Microsoft.Extensions.Hosting;
using Intranet.Entity;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Intranet.Service;

public static class ApplicationLogin
{
    public static bool VerifyPassword(string? password, string encoded)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(encoded)) return false;
        try
        {
            var parts = encoded.Split('$');
            if (parts.Length != 4 || parts[0] != "pbkdf2-sha256" || !int.TryParse(parts[1], out var iterations) || iterations < 100000 || iterations > 1000000) return false;
            var salt = Convert.FromBase64String(parts[2]); var expected = Convert.FromBase64String(parts[3]);
            if (salt.Length < 16 || expected.Length != 32) return false;
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException) { return false; }
    }
    public static ClaimsPrincipal? CreatePrincipal(string username, string? password, ApplicationLoginOptions options)
    {
        if (!options.Enabled || !string.Equals(username.Trim(), options.Username, StringComparison.OrdinalIgnoreCase) || !VerifyPassword(password, options.PasswordHash)) return null;
        // The permission identity is a server-side mapping, not a Windows sign-in.
        return new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, options.PermissionIdentity), new Claim("display_name", options.Username) }, "Cookies"));
    }
}
