using Intranet.Entity;
using Intranet.Service;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace Intranet.Api.Controllers;

[ApiController, Route("api/v1/sesion")]
public sealed class SessionController(GestionIdentity identity, IntranetLogin login, IPortalAccessService access) : ControllerBase
{
    private async Task<SessionResponse> Describe(System.Security.Claims.ClaimsPrincipal user,CancellationToken ct){var permissions=await access.Read(user,ct);return new(user.FindFirst("display_name")?.Value ?? user.Identity?.Name ?? "",identity.GetUser(user),identity.IsDemo,identity.CanRegister(user),permissions.IsGlobalUser,permissions.AllowedOptions,permissions.Profiles);}
    [HttpGet] public async Task<ActionResult<SessionResponse>> Get(CancellationToken ct){Response.Headers.CacheControl="no-store";return Ok(await Describe(User,ct));}
    [AllowAnonymous, HttpPost("login"), EnableRateLimiting("login")]
    public async Task<ActionResult<SessionResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        if (Request.Headers["X-Intranet-Session"] != "1") return BadRequest(new { title = "Solicitud de inicio de sesion no valida." });
        var principal = await login.AuthenticateAsync(request.Username, request.Password, ct);
        if (principal is null) return Unauthorized(new { title = "Usuario o contrasena incorrectos." });
        var response = await Describe(principal,ct);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties { IsPersistent = false });
        return Ok(response);
    }
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (Request.Headers["X-Intranet-Session"] != "1") return BadRequest();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); return NoContent();
    }
}

