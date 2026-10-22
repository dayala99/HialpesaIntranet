using Intranet.Entity;
namespace Intranet.Service;

public sealed class ImportsService(IImportsRepository repository, IAviosRepository orders, GestionIdentity identity, ILogger<ImportsService> logger, RequestActor actor) : ApplicationService(actor), IImportsService
{
    private async Task Access(string series, string number, CancellationToken ct) { if (identity.IsDemo || await orders.FindOrder(series, number, identity.GetUser(User), ct, identity.GetReadAllWindowsUser(User)) is not { Class: "01" }) throw new UnauthorizedAccessException(); }
    public async Task<ServiceResult> List(int page, string? search, CancellationToken ct) { if (identity.IsDemo) return Ok(new { rows = Array.Empty<object>(), hasMore = false, page, totalRows = 0, totalPages = 1 }); return Ok(await repository.List(identity.GetUser(User), identity.GetReadAllWindowsUser(User), page, search, ct)); }
    public async Task<ServiceResult> Source(string series, string number, CancellationToken ct) { await Access(series, number, ct); return Ok(await repository.Source(series, number, ct)); }
    public async Task<ServiceResult> Catalogs(CancellationToken ct) { if (identity.IsDemo) return BadRequest(); identity.GetUser(User); return Ok(await repository.Catalogs(ct)); }
    public async Task<ServiceResult> Create(string series, string number, ImportRequest r, CancellationToken ct)
    {
        if (!identity.CanRegister(User) || !Actor.WriteRequested) throw new UnauthorizedAccessException(); await Access(series, number, ct);
        if (r.RegisteredAt.Year < 2000 || r.RegisteredAt.Year > 2100) throw new ArgumentException("Revisa la fecha de registro.");
        var result = await repository.Create(series, number, r, identity.GetUser(User), User.Identity!.Name!, ct);
        logger.LogInformation("Importación {Import} creada desde OC {Series}-{Number}, usuario {User}, trace {Trace}", result.Number, series, number, identity.GetUser(User), Actor.TraceIdentifier); return Ok(result);
    }
}
