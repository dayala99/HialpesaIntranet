using Intranet.Entity;
namespace Intranet.Service;

public interface IOrderRegistrationApplicationService
{
    Task<ServiceResult> List(int page = 1, string? search = null, CancellationToken ct = default);
    Task<ServiceResult> SupplierDefaults(string code, CancellationToken ct);
    Task<ServiceResult> Factories(CancellationToken ct);
    Task<ServiceResult> GroupsByOp(string factory, string productionOrder, CancellationToken ct);
    Task<ServiceResult> Catalogs(CancellationToken ct);
    Task<ServiceResult> Search(string kind, string? search, CancellationToken ct);
    Task<ServiceResult> LinkedRequirements(string series, string number, string sequence, CancellationToken ct);
    Task<ServiceResult> RemoveRequirements(string series, string number, string sequence, RemoveRequirementLinksRequest request, CancellationToken ct);
    Task<ServiceResult> ItemEdit(string series, string number, string sequence, CancellationToken ct);
    Task<ServiceResult> ItemUpdate(string series, string number, string sequence, UpdateOrderItemRequest request, CancellationToken ct);
    Task<ServiceResult> ItemDelete(string series, string number, string sequence, CancellationToken ct);
    Task<ServiceResult> EditHeader(string series, string number, CancellationToken ct);
    Task<ServiceResult> UpdateHeader(string series, string number, UpdateOrderRequest request, CancellationToken ct);
    Task<ServiceResult> OrderGroups(string series, string number, CancellationToken ct);
    Task<ServiceResult> AddGroups(string series, string number, AddManufacturingGroupsRequest request, CancellationToken ct);
    Task<ServiceResult> Create(CreateOrderRequest request, CancellationToken ct);
    Task<ServiceResult> DetailSelector(string series, string number, string? group, string? productionOrder, CancellationToken ct);
    Task<ServiceResult> Pending(string series, string number, string productionOrder, string itemCode, CancellationToken ct);
    Task<ServiceResult> Add(string series, string number, AddOrderDetailsRequest request, CancellationToken ct);
    Task<ServiceResult> Print(string series, string number, CancellationToken ct);
}
