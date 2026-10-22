using Intranet.Entity;
using Microsoft.Extensions.Hosting;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Diagnostics;
namespace Intranet.Service;

public sealed class OrderRegistrationApplicationService(OrderRegistrationService service, IRegistrationReadRepository reads, IAviosRepository orders, GestionIdentity identity, ILogger<OrderRegistrationApplicationService> logger, RequestActor actor) : ApplicationService(actor), IOrderRegistrationApplicationService
{
    private (string Gestion, string Windows) WriteActor()
    {
        if (!identity.CanRegister(User) || !Actor.WriteRequested) throw new UnauthorizedAccessException();
        return (identity.GetUser(User), User.Identity!.Name!);
    }
    private async Task<PurchaseOrder?> Accessible(string series, string number, CancellationToken ct) => identity.IsDemo ? null : await orders.FindOrder(series, number, identity.GetUser(User), ct, identity.GetReadAllWindowsUser(User));


    public async Task<ServiceResult> List(int page = 1, string? search = null, CancellationToken ct = default)
    {
        if (identity.IsDemo) return Ok(new { rows = Array.Empty<object>(), hasMore = false, page, totalRows = 0, totalPages = 1 });
        return Ok(await reads.List(identity.GetUser(User), identity.GetReadAllWindowsUser(User), page, search ?? "", ct));
    }

    public async Task<ServiceResult> SupplierDefaults(string code, CancellationToken ct)
    {
        if (identity.IsDemo) return BadRequest(); identity.GetUser(User);
        var values = await reads.SupplierDefaults(code, ct); return values is null ? NotFound(new { title = "El proveedor no existe o está inactivo." }) : Ok(values);
    }

    public async Task<ServiceResult> Factories(CancellationToken ct)
    {
        if (identity.IsDemo) return BadRequest();
        identity.GetUser(User); return Ok(await reads.Factories(ct));
    }

    public async Task<ServiceResult> GroupsByOp(string factory, string productionOrder, CancellationToken ct)
    {
        if (identity.IsDemo) return BadRequest();
        identity.GetUser(User); return Ok(await reads.GroupsByOp(factory.Trim(), productionOrder.Trim(), ct));
    }

    public async Task<ServiceResult> Catalogs(CancellationToken ct) => identity.IsDemo ? BadRequest(new BusinessProblem { Title = "Los catálogos de registro requieren conexión real." }) : Ok(await reads.Catalogs(ct));

    public async Task<ServiceResult> Search(string kind, string? search, CancellationToken ct)
    {
        if (identity.IsDemo) return BadRequest();
        if (kind == "proveedores" && (search?.Trim().Length ?? 0) < 2) return BadRequest(new BusinessProblem { Title = "Ingresa al menos dos caracteres para buscar proveedores." });
        return Ok(await reads.Search(kind, search ?? "", ct));
    }


    public async Task<ServiceResult> LinkedRequirements(string series, string number, string sequence, CancellationToken ct)
    { if (await Accessible(series, number, ct) is not { Class: "01" }) return NotFound(); return Ok(await reads.LinkedRequirements(new(series, number), sequence, ct)); }

    public async Task<ServiceResult> RemoveRequirements(string series, string number, string sequence, RemoveRequirementLinksRequest request, CancellationToken ct)
    { var actor = WriteActor(); if (await Accessible(series, number, ct) is not { Class: "01" }) return NotFound(); await service.RemoveRequirements(new(series, number), sequence, request.Keys, actor.Gestion, actor.Windows, ct); return NoContent(); }

    public async Task<ServiceResult> ItemEdit(string series, string number, string sequence, CancellationToken ct)
    { if (await Accessible(series, number, ct) is not { Class: "01" }) return NotFound(); var item = await reads.EditableItem(new(series, number), sequence, ct); return item is null ? NotFound() : Ok(item); }

    public async Task<ServiceResult> ItemUpdate(string series, string number, string sequence, UpdateOrderItemRequest request, CancellationToken ct)
    { var actor = WriteActor(); if (await Accessible(series, number, ct) is not { Class: "01" }) return NotFound(); await service.ChangeItem(new(series, number), sequence, request, actor.Gestion, actor.Windows, ct); return NoContent(); }

    public async Task<ServiceResult> ItemDelete(string series, string number, string sequence, CancellationToken ct)
    { var actor = WriteActor(); if (await Accessible(series, number, ct) is not { Class: "01" }) return NotFound(); await service.ChangeItem(new(series, number), sequence, null, actor.Gestion, actor.Windows, ct); return NoContent(); }

    public async Task<ServiceResult> EditHeader(string series, string number, CancellationToken ct)
    {
        var order = await Accessible(series, number, ct); if (order is not { Class: "01" }) return NotFound();
        if (order.Status != "P") return Conflict(new { title = "La OC debe estar Planeada para editarla." });
        return Ok(await reads.EditableHeader(new(series, number), ct));
    }

    public async Task<ServiceResult> UpdateHeader(string series, string number, UpdateOrderRequest request, CancellationToken ct)
    {
        var actor = WriteActor(); if (await Accessible(series, number, ct) is not { Class: "01" }) return NotFound();
        await service.Update(new(series, number), request, actor.Gestion, actor.Windows, ct); return NoContent();
    }

    public async Task<ServiceResult> OrderGroups(string series, string number, CancellationToken ct)
    {
        if (await Accessible(series, number, ct) is not { Class: "01" }) return NotFound();
        return Ok(await reads.OrderGroups(new(series, number), ct));
    }

    public async Task<ServiceResult> AddGroups(string series, string number, AddManufacturingGroupsRequest request, CancellationToken ct)
    {
        var actor = WriteActor();
        if (await Accessible(series, number, ct) is not { Class: "01" }) return NotFound();
        await service.ChangeGroups(new(series, number), request.Groups, request.RemovedGroups, actor.Gestion, actor.Windows, ct);
        return NoContent();
    }

    public async Task<ServiceResult> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var actor = WriteActor();
        var order = await service.Create(request, actor.Gestion, actor.Windows, ct);
        logger.LogInformation("OC creada {Series}-{Number}, usuario {GestionUser}, identidad {WindowsUser}, trace {Trace}", order.Series, order.Number, actor.Gestion, actor.Windows, Actor.TraceIdentifier);
        return Created($"/api/v1/logistica/avios/ordenes/{order.Series}/{order.Number}", order);
    }

    public async Task<ServiceResult> DetailSelector(string series, string number, string? group, string? productionOrder, CancellationToken ct)
    {
        var header = await Accessible(series, number, ct); if (header is not { Class: "01" }) return NotFound();
        var order = new OrderReference(series, number);
        if (string.IsNullOrWhiteSpace(group)) return Ok(await reads.DetailGroups(order, ct));
        if (string.IsNullOrWhiteSpace(productionOrder)) return Ok(await reads.DetailOps(order, group, ct));
        return Ok(await reads.DetailItems(order, group, productionOrder, ct));
    }

    public async Task<ServiceResult> Pending(string series, string number, string productionOrder, string itemCode, CancellationToken ct)
    {
        if (await Accessible(series, number, ct) is not { Class: "01" }) return NotFound();
        return Ok(await reads.Pending(new(series, number), productionOrder, itemCode, ct));
    }

    public async Task<ServiceResult> Add(string series, string number, AddOrderDetailsRequest request, CancellationToken ct)
    {
        var actor = WriteActor();
        if (await Accessible(series, number, ct) is not { Class: "01" }) return NotFound();
        var result = await service.Add(new(series, number), request, actor.Gestion, actor.Windows, ct);
        logger.LogInformation("Detalle agregado a {Series}-{Number}, {Count} requerimientos, usuario {User}, trace {Trace}", series, number, result.RequirementsAdded, actor.Gestion, Actor.TraceIdentifier);
        return Ok(result);
    }

    public async Task<ServiceResult> Print(string series, string number, CancellationToken ct)
    {
        var order = await Accessible(series, number, ct);
        if (order is not { Class: "01" }) return NotFound();
        return Ok(await reads.Print(order, await orders.Items(series, number, ct), ct));
    }
}
