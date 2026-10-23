namespace Intranet.Entity;
public interface IQualityControlRepository
{
    Task<QualityControlScreen?> Read(string batch,int sequence,string user,CancellationToken ct);
    Task Save(string batch,int sequence,QualityControlUpdate request,string user,CancellationToken ct);
    Task<IReadOnlyList<DyeingCatalogOption>> Fabrics(string search,CancellationToken ct);
    Task<IReadOnlyList<Dictionary<string,object?>>> Consumption(string batch,int sequence,string user,CancellationToken ct);
}
