using Microsoft.Extensions.Hosting;
using Intranet.Entity;
using System.Security.Claims;

namespace Intranet.Service;

public sealed class IntranetLogin(IGestionLoginRepository users)
{
    public const string GestionUserClaim = "gestion_user";
    // Explicit application exception, independent of SEGURIDAD and its availability.
    private const string AdministratorHash = "pbkdf2-sha256$210000$0T/OUN5+vwIPRlKZbsiczg==$s9nAc4F9u2FOBWkHp+8QebOid8m5MLEwV7ssbJKwOWM=";
    public async Task<ClaimsPrincipal?> AuthenticateAsync(string username, string password, CancellationToken ct)
    {
        var code = username.Trim();
        if (string.Equals(code, "programador04", StringComparison.OrdinalIgnoreCase))
        {
            if (!ApplicationLogin.VerifyPassword(password, AdministratorHash)) return null;
            return Principal(@"HIALPESA\programador04", "programador04", "programador04");
        }
        if (code.Length is < 1 or > 15 || string.IsNullOrEmpty(password) || password.Length > 128) return null;
        var user = await users.VerifyAsync(code, password, ct);
        if (user is null || string.IsNullOrWhiteSpace(user.Code)) return null;
        return Principal("gestion:" + user.Code, user.Code, string.IsNullOrWhiteSpace(user.Name) ? user.Code : user.Name);
    }
    private static ClaimsPrincipal Principal(string identity, string code, string displayName) => new(new ClaimsIdentity(new[]{
  new Claim(ClaimTypes.Name,identity),new Claim(GestionUserClaim,code),new Claim("display_name",displayName)
 }, "Cookies"));
}
