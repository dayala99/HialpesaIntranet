using Intranet.Entity;
namespace Intranet.Service;
public interface IAnalisisRbHistoryService
{
 Task<ServiceResult> Save(AnalisisRbSave request,CancellationToken ct);
 Task<ServiceResult> History(string np,long? id,int page,CancellationToken ct);
}
