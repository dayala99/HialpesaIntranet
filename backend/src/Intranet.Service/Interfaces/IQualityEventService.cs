using Intranet.Entity;
namespace Intranet.Service;
public interface IQualityEventService {Task<ServiceResult> Search(QualityEventFilter f,CancellationToken ct,bool unfinished=false);Task<ServiceResult> Catalogs(CancellationToken ct);Task<ServiceResult> Items(string batch,CancellationToken ct);Task<ServiceResult> Save(QualityEventUpdate r,CancellationToken ct,bool unfinished=false);}
