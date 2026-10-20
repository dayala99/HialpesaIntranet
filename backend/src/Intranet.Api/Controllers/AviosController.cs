using System.ComponentModel.DataAnnotations;
using Intranet.Api.Contracts;
using Intranet.Api.Infrastructure;
using Intranet.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Intranet.Api.Controllers;

[ApiController, Route("api/v1/logistica/avios")]
public sealed class AviosController(AviosRepository repository, GestionIdentity identity, QuantityCalculator calculator) : ControllerBase
{
    [HttpGet("ordenes/{series}/{number}")]
    public async Task<ActionResult<OrderDetail>> GetOrder([RegularExpression("^[0-9]{3}$")] string series, [RegularExpression("^[0-9]{6}$")] string number, CancellationToken ct)
    {
        if (identity.IsDemo) return series == "002" && number == "000001" ? Ok(DemoData.Order) : NotFound(new ProblemDetails { Title = "En demostración está disponible la OC 002-000001." });
        var order = await repository.FindOrder(series, number, identity.GetUser(User), ct, identity.GetReadAllWindowsUser(User));
        if (order is null) return NotFound(new ProblemDetails { Title = "La OC no existe o tu usuario no tiene acceso a su clase." });
        if (order.Class != "01") return BadRequest(new ProblemDetails { Title = "Este primer módulo admite OC de clase 01: Compra Avíos Producción." });
        return Ok(new OrderDetail(order, await repository.Items(series, number, ct)));
    }

    [HttpGet("ordenes/{series}/{number}/items/{sequence}/requerimientos")]
    public async Task<ActionResult<IReadOnlyList<Requirement>>> GetRequirements([RegularExpression("^[0-9]{3}$")] string series, [RegularExpression("^[0-9]{6}$")] string number, [RegularExpression("^[0-9]{3}$")] string sequence, CancellationToken ct)
    {
        if (identity.IsDemo) return series == "002" && number == "000001" && sequence is "001" or "002" ? Ok(DemoData.Requirements.Select(r => r with { ItemCode = sequence == "001" ? "AV-DEMO-01" : "AV-DEMO-02" })) : NotFound();
        var order = await repository.FindOrder(series, number, identity.GetUser(User), ct, identity.GetReadAllWindowsUser(User));
        if (order is null) return NotFound();
        if (order.Class != "01") return BadRequest(new ProblemDetails { Title = "La OC no corresponde a Compra Avíos Producción." });
        return Ok(await repository.Requirements(series, number, sequence, ct));
    }

    // Pure calculation: no repository call and no database writes.
    [HttpPost("previsualizacion")]
    public ActionResult<PreviewResponse> Preview([FromBody] PreviewRequest request)
    {
        try { return Ok(calculator.Calculate(request)); }
        catch (ArgumentException ex) { return BadRequest(new ProblemDetails { Title = ex.Message }); }
    }
}
