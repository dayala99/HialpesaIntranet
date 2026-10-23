using Intranet.Entity;
using Intranet.Service;
using Microsoft.AspNetCore.Mvc;
namespace Intranet.Api.Controllers;

[ApiController, Route("api/v1/tintoreria/partidas")]
public sealed class DyeingController(IDyeingService service) : ControllerBase
{
    [HttpGet("catalogos")]
    public async Task<IActionResult> Catalogs(CancellationToken ct) => (await service.Catalogs(ct)).ToActionResult();
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] DyeingFilter filter, CancellationToken ct) => (await service.Search(filter, ct)).ToActionResult();
}
