using Intranet.Entity;
using Microsoft.Extensions.Hosting;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Diagnostics;
namespace Intranet.Service;

public sealed class AviosService(IAviosRepository repository, GestionIdentity identity, QuantityCalculator calculator, IRegistrationReadRepository reads, RequestActor actor) : ApplicationService(actor), IAviosService
{

    public async Task<ServiceResult> PoDetails(string po, string clientCode, CancellationToken ct)
    {
        identity.GetUser(User);
        if (!await repository.PoExists(po, clientCode, ct)) return NotFound();
        return Ok(await repository.PoDetails(po, clientCode, ct));
    }


    public async Task<ServiceResult> ListPos(string po = "", string search = "", int page = 1, CancellationToken ct = default)
    {
        if (identity.IsDemo) return Ok(new { rows = Array.Empty<object>(), page, totalRows = 0, totalPages = 1, hasMore = false });
        return Ok(await repository.ListPos((po ?? "").Trim(), (search ?? "").Trim(), page, ct));
    }


    public async Task<ServiceResult> GetOrder(string series, string number, CancellationToken ct)
    {
        if (identity.IsDemo) return series == "002" && number == "000001" ? Ok(DemoData.Order) : NotFound(new BusinessProblem { Title = "En demostración está disponible la OC 002-000001." });
        var order = await repository.FindOrder(series, number, identity.GetUser(User), ct, identity.GetReadAllWindowsUser(User));
        if (order is null) return NotFound(new BusinessProblem { Title = "La OC no existe o tu usuario no tiene acceso a su clase." });
        if (order.Class != "01") return BadRequest(new BusinessProblem { Title = "Este primer módulo admite OC de clase 01: Compra Avíos Producción." });
        return Ok(new OrderDetail(order, await repository.Items(series, number, ct), await reads.OrderGroups(new(series, number), ct)));
    }


    public async Task<ServiceResult> GetRequirements(string series, string number, string sequence, CancellationToken ct)
    {
        if (identity.IsDemo) return series == "002" && number == "000001" && sequence is "001" or "002" ? Ok(DemoData.Requirements.Select(r => r with { ItemCode = sequence == "001" ? "AV-DEMO-01" : "AV-DEMO-02" })) : NotFound();
        var order = await repository.FindOrder(series, number, identity.GetUser(User), ct, identity.GetReadAllWindowsUser(User));
        if (order is null) return NotFound();
        if (order.Class != "01") return BadRequest(new BusinessProblem { Title = "La OC no corresponde a Compra Avíos Producción." });
        return Ok(await repository.Requirements(series, number, sequence, ct));
    }

    // Pure calculation: no repository call and no database writes.

    public ServiceResult Preview(PreviewRequest request)
    {
        try { return Ok(calculator.Calculate(request)); }
        catch (ArgumentException ex) { return BadRequest(new BusinessProblem { Title = ex.Message }); }
    }
}
