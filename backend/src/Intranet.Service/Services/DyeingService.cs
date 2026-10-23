using Intranet.Entity;
namespace Intranet.Service;

public sealed class DyeingService(IDyeingRepository repository, GestionIdentity identity, RequestActor actor) : ApplicationService(actor), IDyeingService
{
    public async Task<ServiceResult> Catalogs(CancellationToken ct)
    {
        var user = identity.GetUser(User);
        if (identity.IsDemo) return Ok(Array.Empty<DyeingCatalogOption>());
        try { return Ok(await repository.Catalogs(user, ct)); }
        catch (DyeingProcedurePendingException ex) { return Conflict(new BusinessProblem { Title = ex.Message }); }
    }
    public async Task<ServiceResult> Search(DyeingFilter filter, CancellationToken ct)
    {
        var user = identity.GetUser(User);
        Validate(filter);
        if (identity.IsDemo) return Ok(new DyeingPage([], 0, filter.Page, 50));
        try { return Ok(await repository.Search(filter, user, ct)); }
        catch (DyeingProcedurePendingException ex) { return Conflict(new BusinessProblem { Title = ex.Message }); }
    }
    public static void Validate(DyeingFilter f)
    {
        static bool Empty(string? value) => string.IsNullOrWhiteSpace(value);
        if (f.Mode < 1 || f.Mode > 7 || f.Page < 1 || f.Page > 100000) throw new ArgumentException("Filtro o página inválidos.");
        if ((f.Mode <= 3 && Empty(f.Client)) || (f.Mode == 1 && (Empty(f.Series) || Empty(f.OrderNumber)))
            || (f.Mode == 4 && Empty(f.Batch)) || (f.Mode == 5 && Empty(f.Status))
            || (f.Mode == 6 && (Empty(f.Factory) || Empty(f.ProductionOrder))) || (f.Mode == 7 && Empty(f.Fiber)))
            throw new ArgumentException("Completa los campos del filtro seleccionado.");
        if (f.Mode is 3 or 7 && (f.From is null || f.To is null || f.From.Value.Date > f.To.Value.Date
            || f.From.Value.Year < 1900 || f.To.Value.Year >= 2100)) throw new ArgumentException("Revisa el rango de fechas.");
    }
}
