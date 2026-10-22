using System.ComponentModel.DataAnnotations;
using Intranet.Entity;
using Intranet.Service;
using Microsoft.AspNetCore.Mvc;
namespace Intranet.Api.Controllers;

[ApiController, Route("api/v1/proveedor")]
public sealed class ProviderController(IProviderService service) : ControllerBase
{
    [HttpGet("ordenes/{series}/{number}/pos")]
    public async Task<IActionResult> Pos([RegularExpression("^[0-9]{3}$")] string series, [RegularExpression("^[0-9]{6}$")] string number, CancellationToken ct)
    { Response.Headers.CacheControl = "no-store"; return (await service.Pos(series, number, ct)).ToActionResult(); }
    [HttpPost("preparar-llenado")]
    public async Task<IActionResult> Prepare([FromBody] ProviderSelection selection, CancellationToken ct)
    { Response.Headers.CacheControl = "no-store"; return (await service.Prepare(selection, ct)).ToActionResult(); }
    [HttpPost("preparar-apertura-po")]
    public async Task<IActionResult> PreparePoLaunch([FromBody] ProviderPoSelection selection, CancellationToken ct)
    { Response.Headers.CacheControl = "no-store"; return (await service.PreparePoLaunch(selection, ct)).ToActionResult(); }
    [HttpPost("preparar-apertura")]
    public async Task<IActionResult> PrepareLaunch([FromBody] ProviderSelection selection, CancellationToken ct)
    { Response.Headers.CacheControl = "no-store"; return (await service.PrepareLaunch(selection, ct)).ToActionResult(); }
    [Microsoft.AspNetCore.Authorization.AllowAnonymous, HttpPost("recuperar-llenado")]
    public IActionResult Redeem([FromBody] ProviderAgentTicket ticket)
    { Response.Headers.CacheControl = "no-store"; return service.Redeem(ticket).ToActionResult(); }
    [HttpPost("estado-apertura")]
    public IActionResult LaunchStatus([FromBody] ProviderAgentTicket ticket)
    { Response.Headers.CacheControl = "no-store"; return service.LaunchStatus(ticket).ToActionResult(); }
    [HttpPost("iniciar-sesion")]
    public async Task<IActionResult> Login([FromBody] ProviderSelection selection, CancellationToken ct)
    { Response.Headers.CacheControl = "no-store"; return (await service.Login(selection, ct)).ToActionResult(); }
}
