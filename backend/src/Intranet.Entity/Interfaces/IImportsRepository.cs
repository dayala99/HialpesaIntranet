namespace Intranet.Entity;

public interface IImportsRepository
{
    Task<object> List(string user, string? windows, int page, string? search, CancellationToken ct);
    Task<object> Source(string series, string number, CancellationToken ct);
    Task<object> Catalogs(CancellationToken ct);
    Task<ImportCreated> Create(string series, string number, ImportRequest r, string user, string windows, CancellationToken ct);
}
