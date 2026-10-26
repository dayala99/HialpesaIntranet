using System.ComponentModel.DataAnnotations;
using Intranet.Entity;
using Intranet.Service;
using Microsoft.AspNetCore.Mvc;
namespace Intranet.Api.Controllers;
[ApiController,Route("api/v1/logistica/importaciones/control")]
public sealed class ImportControlController(IImportControlService service):ControllerBase
{
 [HttpGet("documentos/opciones")] public async Task<IActionResult> DocumentChoices([Required,StringLength(12)]string kind,[StringLength(1)]string annexType="",[StringLength(4)]string annex="",[StringLength(70)]string search="",CancellationToken ct=default)=>(await service.DocumentChoices(kind,annexType,annex,search,ct)).ToActionResult();
 [HttpGet("embarques")] public async Task<IActionResult> Shipments(CancellationToken ct)=>(await service.Shipments(ct)).ToActionResult();
 [HttpPost("embarques")] public async Task<IActionResult> SaveShipment(ImportControlShipmentSave r,CancellationToken ct)=>(await service.SaveShipment(r,ct)).ToActionResult();
 [HttpGet] public async Task<IActionResult> Search([FromQuery]ImportControlFilter f,CancellationToken ct)=>(await service.Search(f,ct)).ToActionResult();
 [HttpGet("catalogos")] public async Task<IActionResult> Catalogs(CancellationToken ct)=>(await service.Catalogs(ct)).ToActionResult();
 [HttpGet("anexos")] public async Task<IActionResult> Annexes([Required,StringLength(1)]string type,[Required,StringLength(70,MinimumLength=2)]string search,CancellationToken ct)=>(await service.Annexes(type,search,ct)).ToActionResult();
 [HttpGet("{number}")] public async Task<IActionResult> Detail([RegularExpression("^[A-Za-z0-9-]{1,10}$")]string number,CancellationToken ct)=>(await service.Detail(number,ct)).ToActionResult();
 [HttpGet("{number}/facturas")] public async Task<IActionResult> Invoices([RegularExpression("^[A-Za-z0-9-]{1,10}$")]string number,CancellationToken ct)=>(await service.Invoices(number,ct)).ToActionResult();
 [HttpPost] public async Task<IActionResult> Create(ImportControlSave r,CancellationToken ct)=>(await service.Save(null,r,ct)).ToActionResult();
 [HttpPut("{number}")] public async Task<IActionResult> Update([RegularExpression("^[A-Za-z0-9-]{1,10}$")]string number,ImportControlSave r,CancellationToken ct)=>(await service.Save(number,r,ct)).ToActionResult();
 [HttpPost("{number}/provision")] public async Task<IActionResult> Provision([RegularExpression("^[A-Za-z0-9-]{1,10}$")]string number,ImportControlProvision r,CancellationToken ct)=>(await service.Provision(number,r,ct)).ToActionResult();
 [HttpDelete("{number}")] public async Task<IActionResult> Delete([RegularExpression("^[A-Za-z0-9-]{1,10}$")]string number,[FromBody]ImportControlDelete r,CancellationToken ct)=>(await service.Delete(number,r,ct)).ToActionResult();
}
