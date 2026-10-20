using Microsoft.Extensions.Options;

namespace Intranet.Api.Infrastructure;

public sealed class GestionIdentity(IOptions<GestionOptions> options, IWebHostEnvironment environment)
{
    public bool CanRegister(System.Security.Claims.ClaimsPrincipal principal) => !IsDemo
        && options.Value.EnableOrderRegistration && principal.Identity?.IsAuthenticated == true
        && options.Value.WriteWindowsUsers.Contains(principal.Identity.Name ?? "", StringComparer.OrdinalIgnoreCase);
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
        if (IsDemo) return "demo";
        var windowsName = principal.Identity?.Name ?? throw new UnauthorizedAccessException();
        var mapping = options.Value.WindowsUsers.FirstOrDefault(p => string.Equals(p.Key, windowsName, StringComparison.OrdinalIgnoreCase));
        // Explicit mapping is required; the API never accepts an arbitrary Gestion user from the browser.
        if (string.IsNullOrWhiteSpace(mapping.Value)) throw new UnauthorizedAccessException("Tu cuenta Windows no está asociada a un usuario de Gestión.");
        return mapping.Value.Trim();
    }
}
