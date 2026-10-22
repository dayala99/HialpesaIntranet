using Microsoft.Extensions.Hosting;
using Intranet.Entity;
using Microsoft.Extensions.Options;

namespace Intranet.Service;

public sealed class GestionIdentity(IOptions<GestionOptions> options, IHostEnvironment environment)
{
    public bool CanRegister(System.Security.Claims.ClaimsPrincipal principal) => !IsDemo
        && options.Value.EnableOrderRegistration && principal.Identity?.IsAuthenticated == true
        && (options.Value.WriteWindowsUsers.Contains(principal.Identity.Name ?? "", StringComparer.OrdinalIgnoreCase)
            || (options.Value.AllowAllAuthenticatedWrites && !string.IsNullOrWhiteSpace(principal.FindFirst(IntranetLogin.GestionUserClaim)?.Value)));
    public bool IsDemo => environment.IsDevelopment() && options.Value.Mode == "Demo";
    public string? GetReadAllWindowsUser(System.Security.Claims.ClaimsPrincipal principal)
    {
        var name = principal.Identity?.Name;
        return principal.Identity?.IsAuthenticated == true && name is not null
            && options.Value.ReadAllWindowsUsers.Contains(name, StringComparer.OrdinalIgnoreCase)
            ? name : null;
    }
    public string GetUser(System.Security.Claims.ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true) throw new UnauthorizedAccessException();
        var gestionUser = principal.FindFirst(IntranetLogin.GestionUserClaim)?.Value;
        if (!string.IsNullOrWhiteSpace(gestionUser)) return gestionUser;
        if (IsDemo) return "demo";
        var windowsName = principal.Identity?.Name ?? throw new UnauthorizedAccessException();
        var mapping = options.Value.WindowsUsers.FirstOrDefault(p => string.Equals(p.Key, windowsName, StringComparison.OrdinalIgnoreCase));
        // Explicit mapping is required; the API never accepts an arbitrary Gestion user from the browser.
        if (string.IsNullOrWhiteSpace(mapping.Value)) throw new UnauthorizedAccessException("Tu cuenta Windows no está asociada a un usuario de Gestión.");
        return mapping.Value.Trim();
    }
}


