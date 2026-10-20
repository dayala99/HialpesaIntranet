using Intranet.Api.Contracts;

namespace Intranet.Api.Services;

public interface IOrderWriteGateway
{
    Task<IOrderWriteSession> Begin(string gestionUser, string windowsUser, CancellationToken ct);
}
public interface IOrderWriteSession : IAsyncDisposable
{
    Task<OrderReference> CreateHeader(CreateOrderRequest request, CancellationToken ct);
    Task LinkGroup(OrderReference order, string group, CancellationToken ct);
    Task<OrderRegistrationHeader> LockOrder(OrderReference order, CancellationToken ct);
    Task<IReadOnlyList<PendingRequirement>> Pending(OrderRegistrationHeader order, string productionOrder, string itemCode, CancellationToken ct);
    Task<IReadOnlyList<SavedItem>> Items(OrderReference order, CancellationToken ct);
    Task InsertRequirement(OrderReference order, string previousSequence, PendingRequirement requirement, CancellationToken ct);
    Task SetPurchase(OrderReference order, string sequence, decimal quantity, decimal unitPrice, CancellationToken ct);
    Task Commit(CancellationToken ct);
}
public sealed class RegistrationConflictException(string message) : Exception(message);

public sealed class OrderRegistrationService(IOrderWriteGateway gateway)
{
    public async Task<OrderReference> Create(CreateOrderRequest request, string gestionUser, string windowsUser, CancellationToken ct)
    {
        await using var session = await gateway.Begin(gestionUser, windowsUser, ct);
        var order = await session.CreateHeader(request, ct);
        if (string.IsNullOrWhiteSpace(order.Series) || string.IsNullOrWhiteSpace(order.Number))
            throw new RegistrationConflictException("Gestión no devolvió el número de OC; la operación se revirtió.");
        await session.LinkGroup(order, request.ManufacturingGroup.Trim(), ct);
        await session.Commit(ct);
        return order;
    }
    public async Task<DetailsSavedResponse> Add(OrderReference order, AddOrderDetailsRequest request, string gestionUser, string windowsUser, CancellationToken ct)
    {
        if (request.UnitPrice is null or < 0 || request.RequirementKeys.Count is < 1 or > 500 || request.RequirementKeys.Distinct(StringComparer.Ordinal).Count()!=request.RequirementKeys.Count)
            throw new ArgumentException("Selecciona requerimientos sin duplicados e ingresa el precio unitario.");
        await using var session = await gateway.Begin(gestionUser, windowsUser, ct);
        var header = await session.LockOrder(order, ct);
        if (header.ClassCode != "01" || header.Status != "P")
            throw new RegistrationConflictException("Solo se puede agregar detalle a una OC de avíos en estado Planeada.");
        var pending = await session.Pending(header, request.ProductionOrder.Trim(), request.ItemCode.Trim(), ct);
        var selected = new List<PendingRequirement>();
        foreach(var key in request.RequirementKeys)
        {
            var matches = pending.Where(p=>p.Key==key).ToList();
            if(matches.Count!=1 || matches[0].Required<=0) throw new RegistrationConflictException("Los saldos cambiaron o la selección ya fue incorporada. Vuelve a consultar OP e ítem.");
            selected.Add(matches[0]);
        }
        var before = await session.Items(order, ct);
        var previous = before.Select(i=>i.Sequence).Order(StringComparer.Ordinal).LastOrDefault() ?? "000";
        foreach(var requirement in selected) await session.InsertRequirement(order, previous, requirement, ct);
        var after = await session.Items(order, ct);
        var previousIds = before.Select(i=>i.Sequence).ToHashSet(StringComparer.Ordinal);
        var newItems = after.Where(i=>!previousIds.Contains(i.Sequence)).ToList();
        if(newItems.Count==0) throw new RegistrationConflictException("No se generaron secuencias nuevas. La operación se revirtió.");
        foreach(var item in newItems)
            await session.SetPurchase(order, item.Sequence, QuantityCalculator.RoundRequiredQuantity(item.Required), request.UnitPrice.Value, ct);
        await session.Commit(ct);
        return new(order.Series,order.Number,selected.Count,newItems.Count,newItems.Sum(i=>i.Required),newItems.Sum(i=>QuantityCalculator.RoundRequiredQuantity(i.Required)));
    }
}
