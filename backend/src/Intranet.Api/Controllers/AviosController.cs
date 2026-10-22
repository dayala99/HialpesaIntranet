using System.ComponentModel.DataAnnotations;
using Intranet.Entity;
using Intranet.Service;
using Microsoft.AspNetCore.Mvc;
namespace Intranet.Api.Controllers;

[ApiController, Route("api/v1/logistica/avios")]
public sealed class AviosController(IAviosService service) : ControllerBase
{
    [HttpGet("pos/{po}/detalle")]
    public async Task<IActionResult> PoDetails([RegularExpression("^[A-Za-z0-9_./ -]{1,70}$")] string po, [FromQuery, Required, StringLength(20)] string clientCode, CancellationToken ct)
    { return (await service.PoDetails(po, clientCode, ct)).ToActionResult(); }
    [HttpGet("pos")]
    public async Task<IActionResult> ListPos([FromQuery, StringLength(70)] string po = "", [FromQuery, StringLength(100)] string search = "", [FromQuery, Range(1, 1000000)] int page = 1, CancellationToken ct = default)
    { return (await service.ListPos(po, search, page, ct)).ToActionResult(); }
    [HttpGet("ordenes/{series}/{number}")]
    public async Task<ActionResult<OrderDetail>> GetOrder([RegularExpression("^[0-9]{3}$")] string series, [RegularExpression("^[0-9]{6}$")] string number, CancellationToken ct)
    { return (await service.GetOrder(series, number, ct)).ToActionResult(); }
    [HttpGet("ordenes/{series}/{number}/items/{sequence}/requerimientos")]
    public async Task<ActionResult<IReadOnlyList<Requirement>>> GetRequirements([RegularExpression("^[0-9]{3}$")] string series, [RegularExpression("^[0-9]{6}$")] string number, [RegularExpression("^[0-9]{3}$")] string sequence, CancellationToken ct)
    { return (await service.GetRequirements(series, number, sequence, ct)).ToActionResult(); }
    [HttpPost("previsualizacion")]
    public ActionResult<PreviewResponse> Preview([FromBody] PreviewRequest request)
    { return service.Preview(request).ToActionResult(); }
}
