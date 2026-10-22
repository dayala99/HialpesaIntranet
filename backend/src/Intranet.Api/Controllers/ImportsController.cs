using System.ComponentModel.DataAnnotations;
using Intranet.Entity;
using Intranet.Service;
using Microsoft.AspNetCore.Mvc;
namespace Intranet.Api.Controllers;

[ApiController, Route("api/v1/logistica/importaciones")]
public sealed class ImportsController(IImportsService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery, Range(1, 100000)] int page = 1, [FromQuery, MaxLength(70)] string? search = null, CancellationToken ct = default)
    { return (await service.List(page, search, ct)).ToActionResult(); }
    [HttpGet("ordenes/{series}/{number}")]
    public async Task<IActionResult> Source([RegularExpression("^[0-9]{3}$")] string series, [RegularExpression("^[0-9]{6}$")] string number, CancellationToken ct)
    { return (await service.Source(series, number, ct)).ToActionResult(); }
    [HttpGet("catalogos")]
    public async Task<IActionResult> Catalogs(CancellationToken ct)
    { return (await service.Catalogs(ct)).ToActionResult(); }
    [HttpPost("ordenes/{series}/{number}"), Consumes("application/json")]
    public async Task<IActionResult> Create([RegularExpression("^[0-9]{3}$")] string series, [RegularExpression("^[0-9]{6}$")] string number, ImportRequest r, CancellationToken ct)
    { return (await service.Create(series, number, r, ct)).ToActionResult(); }
}
