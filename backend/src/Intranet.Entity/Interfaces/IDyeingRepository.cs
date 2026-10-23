namespace Intranet.Entity;
public interface IDyeingRepository
{
    Task<IReadOnlyList<DyeingCatalogOption>> Catalogs(string user, CancellationToken ct);
    Task<DyeingPage> Search(DyeingFilter filter, string user, CancellationToken ct);
}
