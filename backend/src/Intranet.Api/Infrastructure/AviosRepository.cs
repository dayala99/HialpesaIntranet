using System.Data;
using System.Globalization;
using Dapper;
using Intranet.Api.Contracts;
using Microsoft.Extensions.Options;

namespace Intranet.Api.Infrastructure;

public sealed class AviosRepository(GestionConnectionFactory factory, IOptions<GestionOptions> options)
{
    private static string Text(IDictionary<string, object> row, params string[] names)
    {
        foreach (var name in names)
        {
            var pair = row.FirstOrDefault(p => string.Equals(p.Key, name, StringComparison.OrdinalIgnoreCase));
            if (pair.Value is not null and not DBNull) return Convert.ToString(pair.Value, CultureInfo.InvariantCulture)?.Trim() ?? "";
        }
        return "";
    }
    private static decimal Number(IDictionary<string, object> row, params string[] names) => decimal.TryParse(Text(row, names), NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : 0;
    private CommandDefinition Command(string name, object parameters, CancellationToken ct) => new(name, parameters, commandType: CommandType.StoredProcedure, commandTimeout: options.Value.CommandTimeoutSeconds, cancellationToken: ct);

    public async Task<PurchaseOrder?> FindOrder(string series, string number, string user, CancellationToken ct, string? readAllWindowsUser = null)
    {
        await using var connection = await factory.OpenAsync(ct);
        // Existing procedure applies LG_SEGORDCOMP permissions for the mapped Gestion user.
        var rows = await connection.QueryAsync(Command("dbo.UP_SEL_ORDCOMP", new {
            TIPO_ELECCION = 1, SER_ORDCOMP = series, COD_ORDCOMP = number,
            COD_PROVEEDOR = "", COD_STAORDCOMP = "", COD_GRUPO = "", COD_USUARIO = user,
            COD_FABRICA = "", COD_ORDPRO = "", NUM_REQ = 0
        }, ct));
        var raw = rows.FirstOrDefault();
        if (raw is null && readAllWindowsUser is not null
            && options.Value.ReadAllWindowsUsers.Contains(readAllWindowsUser, StringComparer.OrdinalIgnoreCase))
        {
            // Administrative read access requires the authenticated Windows identity to be
            // the actual SQL login, already sysadmin. A privileged service account alone is insufficient.
            const string adminRead = """
                IF ORIGINAL_LOGIN() = @WindowsIdentity AND IS_SRVROLEMEMBER('sysadmin') = 1
                BEGIN
                    SELECT o.Cod_StaOrdComp, o.Cod_ClaOrdComp, o.Fec_Creacion,
                           p.Des_Proveedor AS PROVEEDOR, c.Nom_Cliente AS NOM_CLIENTE
                    FROM dbo.LG_ORDCOMP o
                    LEFT JOIN dbo.LG_PROVEEDOR p ON p.Cod_Proveedor = o.Cod_Proveedor
                    LEFT JOIN dbo.TG_CLIENTE c ON c.Cod_Cliente = o.COD_CLIENTE
                    WHERE o.Ser_OrdComp = @Series AND o.Cod_OrdComp = @Number
                END
                """;
            raw = (await connection.QueryAsync(new CommandDefinition(adminRead,
                new { WindowsIdentity = readAllWindowsUser, Series = series, Number = number },
                commandTimeout: options.Value.CommandTimeoutSeconds, cancellationToken: ct))).FirstOrDefault();
        }
        if (raw is null) return null;
        var row = (IDictionary<string, object>)raw;
        DateTime? date = DateTime.TryParse(Text(row, "FEC_CREACION"), CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed : null;
        return new(series, number, Text(row, "PROVEEDOR"), Text(row, "COD_STAORDCOMP", "ESTADOOC"), Text(row, "COD_CLAORDCOMP", "CLASEOC"), Text(row, "NOM_CLIENTE", "ABR_CLIENTE"), date);
    }

    public async Task<IReadOnlyList<OrderItem>> Items(string series, string number, CancellationToken ct)
    {
        await using var connection = await factory.OpenAsync(ct);
        var rows = await connection.QueryAsync(Command("dbo.UP_SEL_ORDCOMPITEM", new { TIP_ITEM = "I", SER_ORDCOMP = series, COD_ORDCOMP = number }, ct));
        return rows.Select(raw => {
            var r = (IDictionary<string, object>)raw;
            return new OrderItem(Text(r,"SEC_ORDCOMP"), Text(r,"CODIGO","COD_ITEM"), Text(r,"DESCRIPCION","DES_ITEM"), Text(r,"COD_TALLA"), Text(r,"COD_ESTCLI"), Number(r,"CAN_REQUERIDA"), Number(r,"CAN_COMPRADA"), Number(r,"CAN_RECIBIDA"));
        }).ToList();
    }

    public async Task<IReadOnlyList<Requirement>> Requirements(string series, string number, string sequence, CancellationToken ct)
    {
        await using var connection = await factory.OpenAsync(ct);
        var rows = await connection.QueryAsync(Command("dbo.UP_SEL_ORDCOMPITEMREQ", new { Tip_Item = "I", Ser_OrdComp = series, Cod_OrdComp = number, Sec_OrdComp = sequence }, ct));
        return rows.Select(raw => {
            var r = (IDictionary<string, object>)raw;
            return new Requirement(Text(r,"COD_FABRICA"), Text(r,"COD_ORDPRO"), Text(r,"COD_PRESENT"), Text(r,"DES_PRESENT"), Text(r,"COD_COMPEST"), Text(r,"COD_ITEM"), Text(r,"COD_ESTCLI"), Text(r,"DES_COLOR"), Text(r,"COD_TALLA"), Text(r,"COD_DESTINO"), Number(r,"CAN_REQUERIDA"));
        }).ToList();
    }
}
