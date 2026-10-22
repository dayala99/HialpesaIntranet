using Intranet.Entity;
namespace Intranet.Service;

public interface IImportsService
{
    Task<ServiceResult> List(int page, string? search, CancellationToken ct);
    Task<ServiceResult> Source(string series, string number, CancellationToken ct);
    Task<ServiceResult> Catalogs(CancellationToken ct);
    Task<ServiceResult> Create(string series, string number, ImportRequest r, CancellationToken ct);
}
