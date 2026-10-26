namespace Intranet.Entity;
public interface IImportControlRepository
{
 Task<object> DocumentChoices(string kind,string annexType,string annex,string search,CancellationToken ct);
 Task<object> Shipments(CancellationToken ct);
 Task<object> SaveShipment(ImportControlShipmentSave request,string user,CancellationToken ct);
 Task<ImportControlProvisionResult> Provision(string number,ImportControlProvision request,string user,CancellationToken ct);
 Task<object> Search(ImportControlFilter filter,CancellationToken ct);
 Task<ImportControlCatalogs> Catalogs(CancellationToken ct);
 Task<object> Annexes(string type,string search,CancellationToken ct);
 Task<ImportControlDetail?> Detail(string number,CancellationToken ct);
 Task<object> Invoices(string number,CancellationToken ct);
 Task<string> Save(string? number,ImportControlSave request,string user,CancellationToken ct);
 Task Delete(string number,string version,string user,CancellationToken ct);
}
