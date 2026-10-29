using Intranet.Service;
using Microsoft.AspNetCore.Mvc;
namespace Intranet.Api.Controllers;
[ApiController,Route("api/v1/logistica/analisis-rb")]
public sealed class AnalisisRbController(IAnalisisRbService service,IAnalisisRbImageService images,IAnalisisRbOperationsService operations):ControllerBase
{
 [HttpGet("otros")] public async Task<IActionResult> Filter([FromQuery]decimal? price,[FromQuery]decimal? time,[FromQuery]string? type,[FromQuery]string? cursor,CancellationToken ct){Response.Headers.CacheControl="no-store";return(await service.Filter(price,time,type,cursor,ct)).ToActionResult();}
 [HttpGet("tipos")] public async Task<IActionResult> Types(CancellationToken ct){Response.Headers.CacheControl="no-store";return(await service.Types(ct)).ToActionResult();}
 [HttpGet("costura")] public async Task<IActionResult> SewingOperations([FromQuery]string np,[FromQuery]string? variant,[FromQuery]string? sector,CancellationToken ct){Response.Headers.CacheControl="no-store";return(await operations.Read(np,variant,ct,sector)).ToActionResult();}
 [HttpGet("imagen/historial")] public async Task<IActionResult> HistoryImage([FromQuery]string np,[FromQuery]long id,CancellationToken ct){Response.Headers.CacheControl="private, no-store";Response.Headers["X-Content-Type-Options"]="nosniff";return(await images.ReadHistory(np,id,ct)).ToActionResult();}
 [HttpGet("imagen")] public async Task<IActionResult> Image([FromQuery]string np,CancellationToken ct){Response.Headers.CacheControl="private, no-store";Response.Headers["X-Content-Type-Options"]="nosniff";return(await images.Read(np,ct)).ToActionResult();}
 [HttpGet] public async Task<IActionResult> Search([FromQuery]string np,CancellationToken ct)
 { Response.Headers.CacheControl="no-store";return (await service.Search(np,ct)).ToActionResult(); }
}
