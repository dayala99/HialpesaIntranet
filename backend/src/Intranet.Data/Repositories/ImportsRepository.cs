using Intranet.Entity;
using System.Data;
using Dapper;
using Microsoft.Extensions.Options;
namespace Intranet.Data;

public sealed class ImportsRepository(GestionConnectionFactory factory, IOptions<GestionOptions> options) : IImportsRepository
{
    public async Task<object> List(string user, string? windows, int page, string? search, CancellationToken ct)
    {


        await using var c = await factory.OpenAsync(ct);
        var admin = windows is not null; // Read-all permission comes from the authenticated application identity.
        var term = search?.Trim() ?? "";
        using var result = await c.QueryMultipleAsync(new CommandDefinition("dbo.PA_LG_IMPORTACIONES_S0001", new { User = user, ReadAll = admin, Search = term, Offset = (page - 1) * 50, End = page * 50 + 1 }, commandTimeout: 60, cancellationToken: ct, commandType: System.Data.CommandType.StoredProcedure));
        var rows = (await result.ReadAsync()).ToList(); var totalRows = await result.ReadSingleAsync<int>(); var totalPages = Math.Max(1, (int)Math.Ceiling(totalRows / 50d));
        return (new { rows = rows.Take(50).Select(x => new { number = ((string)x.Number).Trim(), registeredAt = (DateTime?)x.RegisteredAt, status = ((string?)x.Status)?.Trim(), supplier = ((string?)x.Supplier)?.Trim(), currency = ((string?)x.Currency)?.Trim(), origin = ((string?)x.Origin)?.Trim(), series = ((string?)x.Series)?.Trim(), orderNumber = ((string?)x.OrderNumber)?.Trim() }), hasMore = page < totalPages, page, totalRows, totalPages });
    }
    public async Task<object> Source(string series, string number, CancellationToken ct)
    {
        await using var c = await factory.OpenAsync(ct);
        var source = await c.QuerySingleAsync<ImportSource>(new CommandDefinition("dbo.PA_LG_ORDCOMP_S0003", new { Series = series, Number = number }, cancellationToken: ct, commandType: System.Data.CommandType.StoredProcedure));
        var mode = await c.ExecuteScalarAsync<string>(new CommandDefinition("dbo.PA_TG_CONTROL_S0001", cancellationToken: ct, commandType: System.Data.CommandType.StoredProcedure));
        var valid = await c.ExecuteScalarAsync<int>(new CommandDefinition("dbo.PA_CN_ANEXOSCONTABLES_S0001", new { source.AnnexType, source.Annex, source.SupplierCode }, cancellationToken: ct, commandType: System.Data.CommandType.StoredProcedure));
        return (new { source, automaticNumbering = mode?.Trim() == "N", validAnnex = valid > 0 });
    }
    public async Task<object> Catalogs(CancellationToken ct)
    {
        await using var c = await factory.OpenAsync(ct);

        var rows = await c.QueryAsync(new CommandDefinition("dbo.PA_LG_ORIGEN_IMPORTACION_S0001", cancellationToken: ct, commandType: System.Data.CommandType.StoredProcedure)); return (rows);
    }
    public async Task<ImportCreated> Create(string series, string number, ImportRequest r, string user, string windows, CancellationToken ct)
    {


        if (r.RegisteredAt.Year < 2000 || r.RegisteredAt.Year > 2100) throw new ArgumentException("Revisa la fecha de registro.");
        await using var c = await factory.OpenAsync(ct);
        // Global application write access is explicitly configured for authenticated Gestion users.
        if (!options.Value.AllowAllAuthenticatedWrites && await c.ExecuteScalarAsync<int>(new CommandDefinition("dbo.PA_LG_IMPORTACIONES_S0002", new { Windows = windows }, cancellationToken: ct, commandType: System.Data.CommandType.StoredProcedure)) != 1) throw new UnauthorizedAccessException();
        await using var t = await c.BeginTransactionAsync(ct);
        try
        {
            var source = await c.QuerySingleAsync<ImportSource>(new CommandDefinition("dbo.PA_LG_ORDCOMP_S0003", new { Series = series, Number = number, LockRow = true }, t, cancellationToken: ct, commandType: System.Data.CommandType.StoredProcedure));
            if (!string.IsNullOrWhiteSpace(source.ExistingImport) && source.ExistingImport != "0") throw new RegistrationConflictException("La OC ya tiene una importación: " + source.ExistingImport);
            if (source.SupplierOrigin != "E") throw new ArgumentException("La OC debe corresponder a un proveedor extranjero.");
            if (await c.ExecuteScalarAsync<string>(new CommandDefinition("dbo.PA_TG_CONTROL_S0001", new { LockRow = true }, transaction: t, cancellationToken: ct, commandType: System.Data.CommandType.StoredProcedure)) != "N") throw new RegistrationConflictException("Gestión no está configurado para numeración automática.");
            var valid = await c.ExecuteScalarAsync<int>(new CommandDefinition("dbo.PA_CN_ANEXOSCONTABLES_S0001", new { source.AnnexType, source.Annex, source.SupplierCode }, t, cancellationToken: ct, commandType: System.Data.CommandType.StoredProcedure));
            if (valid != 1) throw new ArgumentException("No se pudo validar el anexo contable del proveedor por RUC.");
            if (await c.ExecuteScalarAsync<int>(new CommandDefinition("dbo.PA_CN_TIPOITEM_S0001", new { r.ItemType }, t, cancellationToken: ct, commandType: System.Data.CommandType.StoredProcedure)) != 1) throw new ArgumentException("Selecciona un tipo de ítem válido para importaciones.");
            await c.ExecuteAsync(new CommandDefinition("dbo.PA_TG_ANOCONTROL_S0001", new { Year = r.RegisteredAt.Year.ToString() }, t, cancellationToken: ct, commandType: System.Data.CommandType.StoredProcedure));
            var p = ImportParameters.Create(r, source, user);
            var result = await c.QuerySingleAsync(new CommandDefinition("dbo.UP_MAN_LG_IMPORTACIONES", p, t, commandType: CommandType.StoredProcedure, commandTimeout: 60, cancellationToken: ct));
            string importNumber = ((string)result.NUM_IMPORTACION).Trim();
            if (string.IsNullOrWhiteSpace(importNumber)) throw new RegistrationConflictException("Gestión no devolvió un número de importación.");
            await c.ExecuteAsync(new CommandDefinition("dbo.UP_MAN_ORDCOMP_IMPORTACION", new { NUM_IMPORTACION = importNumber, COD_TIPOALMACEN = "C", SER_ORDCOMP = series, COD_ORDCOMP = number }, t, commandType: CommandType.StoredProcedure, commandTimeout: 60, cancellationToken: ct));
            var linked = await c.ExecuteScalarAsync<string>(new CommandDefinition("dbo.PA_LG_ORDCOMP_S0004", new { Series = series, Number = number }, t, cancellationToken: ct, commandType: System.Data.CommandType.StoredProcedure));
            if (linked?.Trim() != importNumber) throw new RegistrationConflictException("No se completó la asociación de la OC.");
            await t.CommitAsync(ct);

            return (new ImportCreated(importNumber, series, number));
        }
        catch { if (t.Connection is not null) await t.RollbackAsync(CancellationToken.None); throw; }
    }
}
public static class ImportParameters
{
    public static DynamicParameters Create(ImportRequest r, ImportSource s, string user)
    {
        var p = new DynamicParameters();
        var values = new
        {
            Accion = "I",
            Num_Importacion = "",
            Ser_OrdComp = "",
            Cod_OrdComp = "",
            Num_Guia = r.Guide.Trim(),
            Fecha_Guia = r.GuideDate?.Date,
            Cod_Embarque = r.Shipment.Trim(),
            Cod_OrigenImportacion = r.Origin.Trim(),
            Observacion = r.Observations.Trim(),
            Cod_TipAnex = s.AnnexType,
            Cod_Anxo = s.Annex,
            Cod_TipPro = "I",
            Fec_RegDoc = r.RegisteredAt.Date,
            ConfirmacionPedido = "",
            Proforma = "",
            Cod_TipIte = r.ItemType.Trim(),
            Cod_Moneda = s.Currency,
            cod_usuario = user,
            Flg_Liquidacion_Parcial = "N",
            Num_Importacion_Inicial = "",
            ID_PAIS_DUA = r.Country.Trim(),
            Fec_Pago = r.PaymentDate?.Date,
            fec_vendoc = r.DueDate?.Date,
            Cod_AreaResponsable = r.Area.Trim()
        };
        foreach (var prop in values.GetType().GetProperties()) p.Add(prop.Name, prop.GetValue(values)); return p;
    }
}




