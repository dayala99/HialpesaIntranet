using Intranet.Entity;
namespace Intranet.Service;
public interface IDyeingService
{
    Task<ServiceResult> Catalogs(CancellationToken ct);
    Task<ServiceResult> Search(DyeingFilter filter, CancellationToken ct);
}
