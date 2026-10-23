using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Intranet.Entity;
namespace Intranet.Data;

public sealed class DyeingRepository(GestionConnectionFactory factory) : IDyeingRepository
{
    private const string Procedure = "dbo.PA_TI_ORDTRA_TINTORERIA_ITEMS_S0001";
    private static object Parameters(DyeingFilter f, string user, bool catalogs = false) => new
    {
        OPCION = catalogs ? "0" : f.Mode.ToString(System.Globalization.CultureInfo.InvariantCulture),
        COD_CLIENTE_TEX = f.Client.Trim(), SER_ORDCOMP = f.Series.Trim(), COD_ORDCOMP = f.OrderNumber.Trim(),
        COD_COLOR = f.Color.Trim(), COD_ORDTRA = f.Batch.Trim(), FECHA_DESDE = f.From?.Date, FECHA_HASTA = f.To?.Date,
        FLG_STATUS = f.Status.Trim(), COD_FABRICA = f.Factory.Trim(), COD_ORDPRO = f.ProductionOrder.Trim(),
        FEC_INICIO = f.From?.Date, FEC_FIN = f.To?.Date, COD_FIBRA = f.Fiber.Trim(), COD_USUARIO = user,
        PAGINA = f.Page, TAMANO_PAGINA = 50
    };
    public async Task<IReadOnlyList<DyeingCatalogOption>> Catalogs(string user, CancellationToken ct)
    {
        await using var c = await factory.OpenAsync(ct);
        try { return (await c.QueryAsync<DyeingCatalogOption>(new CommandDefinition(Procedure, Parameters(new(), user, true), commandType: CommandType.StoredProcedure, commandTimeout: 60, cancellationToken: ct))).ToList(); }
        catch (SqlException ex) when (ex.Number == 2812) { throw new DyeingProcedurePendingException(); }
    }
    public async Task<DyeingPage> Search(DyeingFilter filter, string user, CancellationToken ct)
    {
        await using var c = await factory.OpenAsync(ct);
        try
        {
            using var results = await c.QueryMultipleAsync(new CommandDefinition(Procedure, Parameters(filter, user), commandType: CommandType.StoredProcedure, commandTimeout: 60, cancellationToken: ct));
            var total = await results.ReadSingleAsync<int>();
            var rows = (await results.ReadAsync()).Select(row => ((IDictionary<string, object>)row).ToDictionary(
                entry => entry.Key.ToLowerInvariant(), entry => entry.Value is string value ? (object?)value.Trim() : entry.Value)).ToList();
            return new(rows, total, filter.Page, 50);
        }
        catch (SqlException ex) when (ex.Number == 2812) { throw new DyeingProcedurePendingException(); }
    }
}
