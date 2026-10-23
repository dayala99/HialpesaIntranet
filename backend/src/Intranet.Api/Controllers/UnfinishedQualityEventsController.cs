using Intranet.Entity;
using Intranet.Service;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
namespace Intranet.Api.Controllers;
[ApiController,Route("api/v1/aseguramiento-calidad/eventos-tintoreria-tela-por-acabar")]
public sealed class UnfinishedQualityEventsController(IQualityEventService service):ControllerBase {
 [HttpGet] public async Task<IActionResult> Search([FromQuery]QualityEventFilter f,CancellationToken ct)=>(await service.Search(f,ct,true)).ToActionResult();
 [HttpGet("catalogos")] public async Task<IActionResult> Catalogs(CancellationToken ct)=>(await service.Catalogs(ct)).ToActionResult();
 [HttpGet("partidas/{batch}/items")] public async Task<IActionResult> Items([RegularExpression("^[A-Za-z0-9]{1,5}$")]string batch,CancellationToken ct)=>(await service.Items(batch,ct)).ToActionResult();
 [HttpPost,Consumes("application/json")] public async Task<IActionResult> Save(QualityEventUpdate r,CancellationToken ct)=>(await service.Save(r,ct,true)).ToActionResult();
}
