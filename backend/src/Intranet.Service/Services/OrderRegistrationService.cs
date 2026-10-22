using Microsoft.Extensions.Hosting;
using Intranet.Entity;

namespace Intranet.Service;

public sealed class OrderRegistrationService(IOrderWriteGateway gateway)
{
    public async Task RemoveRequirements(OrderReference order, string sequence, IReadOnlyList<string> keys, string gestionUser, string windowsUser, CancellationToken ct)
    {
        if (keys.Count is < 1 or > 500 || keys.Distinct().Count() != keys.Count) throw new ArgumentException("Selecciona requerimientos sin duplicados.");
        await using var session = await gateway.Begin(gestionUser, windowsUser, ct); var header = await session.LockOrder(order, ct);
        if (header.ClassCode != "01" || header.Status != "P") throw new RegistrationConflictException("Retira requerimientos únicamente de una OC de avíos Planeada.");
        await session.RemoveRequirements(order, sequence, keys, ct); await session.Commit(ct);
    }
    public async Task ChangeItem(OrderReference order, string sequence, UpdateOrderItemRequest? request, string gestionUser, string windowsUser, CancellationToken ct)
    {
        await using var session = await gateway.Begin(gestionUser, windowsUser, ct); var header = await session.LockOrder(order, ct);
        if (header.ClassCode != "01" || header.Status != "P") throw new RegistrationConflictException("Modifica detalles únicamente en OC de avíos Planeadas.");
        await session.ChangeItem(order, sequence, request, ct); await session.Commit(ct);
    }
    public async Task Update(OrderReference order, UpdateOrderRequest request, string gestionUser, string windowsUser, CancellationToken ct)
    {
        await using var session = await gateway.Begin(gestionUser, windowsUser, ct);
        var header = await session.LockOrder(order, ct);
        if (header.ClassCode != "01" || header.Status != "P") throw new RegistrationConflictException("Edita únicamente OC de avíos Planeadas.");
        await session.UpdateHeader(order, request, ct); await session.Commit(ct);
    }
    public Task AddGroups(OrderReference order, IReadOnlyList<string> groups, string gestionUser, string windowsUser, CancellationToken ct) => ChangeGroups(order, groups, Array.Empty<string>(), gestionUser, windowsUser, ct);
    public async Task ChangeGroups(OrderReference order, IReadOnlyList<string> groups, IReadOnlyList<string> removed, string gestionUser, string windowsUser, CancellationToken ct)
    {
        if (groups.Count + removed.Count is < 1 or > 100 || groups.Concat(removed).Any(g => string.IsNullOrWhiteSpace(g) || g.Trim().Length > 8)) throw new ArgumentException("Selecciona grupos válidos.");
        var added = groups.Select(g => g.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var deleted = removed.Select(g => g.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (added.Intersect(deleted, StringComparer.OrdinalIgnoreCase).Any()) throw new ArgumentException("Un grupo no puede añadirse y eliminarse a la vez.");
        await using var session = await gateway.Begin(gestionUser, windowsUser, ct);
        var header = await session.LockOrder(order, ct);
        if (header.ClassCode != "01" || header.Status != "P") throw new RegistrationConflictException("Modifica grupos únicamente en una OC de avíos Planeada.");
        foreach (var group in deleted) await session.RemoveGroup(order, group, ct);
        foreach (var group in added) await session.LinkGroup(order, group, ct);
        await session.Commit(ct);
    }
    public async Task<OrderReference> Create(CreateOrderRequest request, string gestionUser, string windowsUser, CancellationToken ct)
    {
        await using var session = await gateway.Begin(gestionUser, windowsUser, ct);
        var order = await session.CreateHeader(request, ct);
        if (string.IsNullOrWhiteSpace(order.Series) || string.IsNullOrWhiteSpace(order.Number))
            throw new RegistrationConflictException("Gestión no devolvió el número de OC; la operación se revirtió.");
        foreach (var group in new[] { request.ManufacturingGroup }.Concat(request.AdditionalManufacturingGroups).Where(g => !string.IsNullOrWhiteSpace(g)).Select(g => g.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
            await session.LinkGroup(order, group, ct);
        await session.Commit(ct);
        return order;
    }
    public async Task<DetailsSavedResponse> Add(OrderReference order, AddOrderDetailsRequest request, string gestionUser, string windowsUser, CancellationToken ct)
    {
        if (request.UnitPrice is null or < 0 || request.RequirementKeys.Count is < 1 or > 500 || request.RequirementKeys.Distinct(StringComparer.Ordinal).Count() != request.RequirementKeys.Count)
            throw new ArgumentException("Selecciona requerimientos sin duplicados e ingresa el precio unitario.");
        await using var session = await gateway.Begin(gestionUser, windowsUser, ct);
        var header = await session.LockOrder(order, ct);
        if (header.ClassCode != "01" || header.Status != "P")
            throw new RegistrationConflictException("Solo se puede agregar detalle a una OC de avíos en estado Planeada.");
        var pending = await session.Pending(header, request.ProductionOrder.Trim(), request.ItemCode.Trim(), ct);
        var selected = new List<PendingRequirement>();
        foreach (var key in request.RequirementKeys)
        {
            var matches = pending.Where(p => p.Key == key).ToList();
            if (matches.Count != 1 || matches[0].Required <= 0) throw new RegistrationConflictException("Los saldos cambiaron o la selección ya fue incorporada. Vuelve a consultar OP e ítem.");
            selected.Add(matches[0]);
        }
        var before = await session.Items(order, ct);
        var previous = before.Select(i => i.Sequence).Order(StringComparer.Ordinal).LastOrDefault() ?? "000";
        foreach (var requirement in selected) await session.InsertRequirement(order, previous, requirement, ct);
        var after = await session.Items(order, ct);
        var previousIds = before.Select(i => i.Sequence).ToHashSet(StringComparer.Ordinal);
        var newItems = after.Where(i => !previousIds.Contains(i.Sequence)).ToList();
        if (newItems.Count == 0) throw new RegistrationConflictException("No se generaron secuencias nuevas. La operación se revirtió.");
        foreach (var item in newItems)
            await session.SetPurchase(order, item.Sequence, QuantityCalculator.RoundRequiredQuantity(item.Required), request.UnitPrice.Value, ct);
        await session.Commit(ct);
        return new(order.Series, order.Number, selected.Count, newItems.Count, newItems.Sum(i => i.Required), newItems.Sum(i => QuantityCalculator.RoundRequiredQuantity(i.Required)));
    }
}
