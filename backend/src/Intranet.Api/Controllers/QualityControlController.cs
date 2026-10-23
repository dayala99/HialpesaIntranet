using Intranet.Entity;
using Intranet.Service;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
namespace Intranet.Api.Controllers;
[ApiController,Route("api/v1/tintoreria/partidas")]
public sealed class QualityControlController(IQualityControlService service):ControllerBase
{
 [HttpGet("{batch}/items/{sequence:int}/control-calidad")]
 public async Task<IActionResult> Read([RegularExpression("^[A-Za-z0-9]{1,5}$")]string batch,[Range(1,int.MaxValue)]int sequence,CancellationToken ct)=>(await service.Read(batch,sequence,ct)).ToActionResult();
 [HttpPut("{batch}/items/{sequence:int}/control-calidad"),Consumes("application/json")]
 public async Task<IActionResult> Save([RegularExpression("^[A-Za-z0-9]{1,5}$")]string batch,[Range(1,int.MaxValue)]int sequence,QualityControlUpdate request,CancellationToken ct)=>(await service.Save(batch,sequence,request,ct)).ToActionResult();
 [HttpGet("control-calidad/telas")]
 public async Task<IActionResult> Fabrics([FromQuery,MaxLength(100)]string search="",CancellationToken ct=default)=>(await service.Fabrics(search,ct)).ToActionResult();
 [HttpGet("{batch}/items/{sequence:int}/control-calidad/consumo")]
 public async Task<IActionResult> Consumption([RegularExpression("^[A-Za-z0-9]{1,5}$")]string batch,[Range(1,int.MaxValue)]int sequence,CancellationToken ct)=>(await service.Consumption(batch,sequence,ct)).ToActionResult();
}
