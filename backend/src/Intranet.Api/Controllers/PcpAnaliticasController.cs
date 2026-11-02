using Intranet.Service;using Intranet.Entity;using Microsoft.AspNetCore.Mvc;
namespace Intranet.Api.Controllers;
[ApiController,Route("api/v1/pcp/secuenciador")]
public sealed class PcpAnaliticasController(IPcpAnaliticasService service):ControllerBase {
 [HttpGet("requisitos")]public async Task<IActionResult> Requirements([FromQuery]string np,CancellationToken ct){Response.Headers.CacheControl="no-store";return(await service.Requirements(np,ct)).ToActionResult();}
 [HttpGet("envios")]public async Task<IActionResult> Read([FromQuery]string? np,[FromQuery]long? id,[FromQuery]long? cursor,[FromQuery]string? status="E",CancellationToken ct=default){Response.Headers.CacheControl="no-store";return(await service.Read(np,id,cursor,status,ct)).ToActionResult();}
 [HttpPost("envios")]public async Task<IActionResult> Send([FromBody]PcpSend request,CancellationToken ct){Response.Headers.CacheControl="no-store";return(await service.Send(request,ct)).ToActionResult();}
}
