namespace Intranet.Entity;

public interface IAviosRepository
{
    Task<object> ListPos(string po, string search, int page, CancellationToken ct);
    Task<bool> PoExists(string po, string clientCode, CancellationToken ct);
    Task<IReadOnlyList<PoDetailLine>> PoDetails(string po, string clientCode, CancellationToken ct);
    Task<PurchaseOrder?> FindOrder(string series, string number, string user, CancellationToken ct, string? readAllWindowsUser = null);
    Task<IReadOnlyList<OrderItem>> Items(string series, string number, CancellationToken ct);
    Task<IReadOnlyList<Requirement>> Requirements(string series, string number, string sequence, CancellationToken ct);
}
