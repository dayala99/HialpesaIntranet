using Intranet.Entity;
namespace Intranet.Service;
public interface IAnalisisRbService { Task<ServiceResult> Filter(decimal? price,decimal? time,string? type,string? cursor,CancellationToken ct); Task<ServiceResult> Types(CancellationToken ct); Task<ServiceResult> Search(string np,CancellationToken ct); }
