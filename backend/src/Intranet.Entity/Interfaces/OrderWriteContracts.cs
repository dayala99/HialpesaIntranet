namespace Intranet.Entity;

public interface IOrderWriteGateway
{
    Task<IOrderWriteSession> Begin(string gestionUser, string windowsUser, CancellationToken ct);
}
public interface IOrderWriteSession : IAsyncDisposable
{
    Task<OrderReference> CreateHeader(CreateOrderRequest request, CancellationToken ct);
    Task LinkGroup(OrderReference order, string group, CancellationToken ct);
    Task RemoveRequirements(OrderReference order, string sequence, IReadOnlyList<string> keys, CancellationToken ct);
    Task ChangeItem(OrderReference order, string sequence, UpdateOrderItemRequest? request, CancellationToken ct);
    Task UpdateHeader(OrderReference order, UpdateOrderRequest request, CancellationToken ct);
    Task RemoveGroup(OrderReference order, string group, CancellationToken ct);
    Task<OrderRegistrationHeader> LockOrder(OrderReference order, CancellationToken ct);
    Task<IReadOnlyList<PendingRequirement>> Pending(OrderRegistrationHeader order, string productionOrder, string itemCode, CancellationToken ct);
    Task<IReadOnlyList<SavedItem>> Items(OrderReference order, CancellationToken ct);
    Task InsertRequirement(OrderReference order, string previousSequence, PendingRequirement requirement, CancellationToken ct);
    Task SetPurchase(OrderReference order, string sequence, decimal quantity, decimal unitPrice, CancellationToken ct);
    Task Commit(CancellationToken ct);
}
public sealed class RegistrationConflictException(string message) : Exception(message);

