using Intranet.Service;
using Intranet.Entity;
using Microsoft.AspNetCore.Mvc;
namespace Intranet.Api.Controllers;
[ApiController,Route("api/v1/logistica/analisis-rb/historial")]
public sealed class AnalisisRbHistoryController(IAnalisisRbHistoryService service,IAnalisisRbHistorySearchService search):ControllerBase
{
 [HttpGet("buscar")]public async Task<IActionResult> Search([FromQuery]string? np,[FromQuery]decimal? price,[FromQuery]decimal? time,[FromQuery]string? type,[FromQuery]long? cursor,CancellationToken ct){Response.Headers.CacheControl="no-store";return(await search.Search(np,price,time,type,cursor,ct)).ToActionResult();}
 [HttpGet("tipos")]public async Task<IActionResult> Types(CancellationToken ct){Response.Headers.CacheControl="no-store";return(await search.Types(ct)).ToActionResult();}
 [HttpGet]public async Task<IActionResult> History([FromQuery]string np,[FromQuery]long? id,[FromQuery]int page=1,CancellationToken ct=default){Response.Headers.CacheControl="no-store";return(await service.History(np,id,page,ct)).ToActionResult();}
 [HttpPost]public async Task<IActionResult> Save([FromBody]AnalisisRbSave request,CancellationToken ct){Response.Headers.CacheControl="no-store";return(await service.Save(request,ct)).ToActionResult();}
}
