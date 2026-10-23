using Intranet.Entity;
namespace Intranet.Service;
public interface IQualityControlService
{
 Task<ServiceResult> Read(string batch,int sequence,CancellationToken ct);
 Task<ServiceResult> Save(string batch,int sequence,QualityControlUpdate request,CancellationToken ct);
 Task<ServiceResult> Fabrics(string search,CancellationToken ct);
 Task<ServiceResult> Consumption(string batch,int sequence,CancellationToken ct);
}
