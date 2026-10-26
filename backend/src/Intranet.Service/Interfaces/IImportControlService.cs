using Intranet.Entity;
namespace Intranet.Service;
public interface IImportControlService
{
 Task<ServiceResult> DocumentChoices(string kind,string annexType,string annex,string search,CancellationToken ct);
 Task<ServiceResult> Shipments(CancellationToken ct);
 Task<ServiceResult> SaveShipment(ImportControlShipmentSave request,CancellationToken ct);
 Task<ServiceResult> Provision(string number,ImportControlProvision request,CancellationToken ct);
 Task<ServiceResult> Search(ImportControlFilter filter,CancellationToken ct);
 Task<ServiceResult> Catalogs(CancellationToken ct);
 Task<ServiceResult> Annexes(string type,string search,CancellationToken ct);
 Task<ServiceResult> Detail(string number,CancellationToken ct);
 Task<ServiceResult> Invoices(string number,CancellationToken ct);
 Task<ServiceResult> Save(string? number,ImportControlSave request,CancellationToken ct);
 Task<ServiceResult> Delete(string number,ImportControlDelete request,CancellationToken ct);
}
