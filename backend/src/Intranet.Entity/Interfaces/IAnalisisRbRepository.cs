namespace Intranet.Entity;
public interface IAnalisisRbRepository { Task<AnalisisRbFilterPage> Filter(decimal? price,decimal? time,string? type,string? cursor,CancellationToken ct); Task<IReadOnlyList<AnalisisRbType>> Types(CancellationToken ct); Task<IReadOnlyList<AnalisisRbRow>> Search(string np, CancellationToken ct); }
