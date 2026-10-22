using Intranet.Entity;
namespace Intranet.Service;

public interface IAviosService
{
    Task<ServiceResult> PoDetails(string po, string clientCode, CancellationToken ct);
    Task<ServiceResult> ListPos(string po = "", string search = "", int page = 1, CancellationToken ct = default);
    Task<ServiceResult> GetOrder(string series, string number, CancellationToken ct);
    Task<ServiceResult> GetRequirements(string series, string number, string sequence, CancellationToken ct);
    ServiceResult Preview(PreviewRequest request);
}
