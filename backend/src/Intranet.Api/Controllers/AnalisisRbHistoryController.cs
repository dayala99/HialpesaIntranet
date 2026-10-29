using Intranet.Service;
using Intranet.Entity;
using Microsoft.AspNetCore.Mvc;
namespace Intranet.Api.Controllers;
[ApiController,Route("api/v1/logistica/analisis-rb/historial")]
public sealed class AnalisisRbHistoryController(IAnalisisRbHistoryService service):ControllerBase
{
 [HttpGet]public async Task<IActionResult> History([FromQuery]string np,[FromQuery]long? id,[FromQuery]int page=1,CancellationToken ct=default){Response.Headers.CacheControl="no-store";return(await service.History(np,id,page,ct)).ToActionResult();}
 [HttpPost]public async Task<IActionResult> Save([FromBody]AnalisisRbSave request,CancellationToken ct){Response.Headers.CacheControl="no-store";return(await service.Save(request,ct)).ToActionResult();}
}
