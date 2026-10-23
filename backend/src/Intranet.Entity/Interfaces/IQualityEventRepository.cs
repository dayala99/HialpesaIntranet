namespace Intranet.Entity;
public interface IQualityEventRepository {
 Task<IReadOnlyList<Dictionary<string,object?>>> Search(QualityEventFilter filter,CancellationToken ct,bool unfinished=false);
 Task<QualityEventBatch> Items(string batch,CancellationToken ct);
 Task<IReadOnlyList<DyeingCatalogOption>> Catalogs(CancellationToken ct);
 Task Save(QualityEventUpdate request,CancellationToken ct,bool unfinished=false);
}
