namespace Intranet.Entity;

public interface IRegistrationReadRepository
{
    Task<object> List(string user, string? windows, int page, string search, CancellationToken ct);
    Task<RegistrationCatalogs> Catalogs(CancellationToken ct);
    Task<IReadOnlyList<LookupOption>> DetailGroups(OrderReference order, CancellationToken ct);
    Task<IReadOnlyList<LookupOption>> DetailOps(OrderReference order, string group, CancellationToken ct);
    Task<IReadOnlyList<LookupOption>> DetailItems(OrderReference order, string group, string op, CancellationToken ct);
    Task<object> LinkedRequirements(OrderReference order, string sequence, CancellationToken ct);
    Task<UpdateOrderItemRequest?> EditableItem(OrderReference order, string sequence, CancellationToken ct);
    Task<object> EditableHeader(OrderReference order, CancellationToken ct);
    Task<IReadOnlyList<LookupOption>> OrderGroups(OrderReference order, CancellationToken ct);
    Task<IReadOnlyList<LookupOption>> Factories(CancellationToken ct);
    Task<IReadOnlyList<LookupOption>> GroupsByOp(string factoryAbbreviation, string productionOrder, CancellationToken ct);
    Task<SupplierDefaults?> SupplierDefaults(string code, CancellationToken ct);
    Task<IReadOnlyList<LookupOption>> Search(string kind, string search, CancellationToken ct);
    Task<IReadOnlyList<PendingRequirement>> Pending(OrderReference order, string op, string item, CancellationToken ct);
    Task<PrintOrder> Print(PurchaseOrder order, IReadOnlyList<OrderItem> items, CancellationToken ct);
}
