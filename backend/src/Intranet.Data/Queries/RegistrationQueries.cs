using Intranet.Entity;
using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;

using Microsoft.Data.SqlClient;

namespace Intranet.Data;

public static class RegistrationQueries
{
    public static string Text(IDictionary<string, object> row, params string[] keys)
    {
        foreach (var key in keys)
        {
            var pair = row.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.OrdinalIgnoreCase));
            if (pair.Value is not null and not DBNull) return Convert.ToString(pair.Value, CultureInfo.InvariantCulture)?.Trim() ?? "";
        }
        return "";
    }
    public static decimal Number(IDictionary<string, object> row, params string[] keys) => decimal.TryParse(Text(row, keys), NumberStyles.Any, CultureInfo.InvariantCulture, out var n) ? n : 0;
    public static async Task<OrderRegistrationHeader> Header(SqlConnection connection, OrderReference order, SqlTransaction? transaction, bool lockRow, CancellationToken ct)
    {
        const string sql = "dbo.PA_LG_ORDCOMP_S0001";
        var raw = await connection.QuerySingleOrDefaultAsync(new CommandDefinition(sql, new { order.Series, order.Number, LockRow = lockRow }, transaction, cancellationToken: ct, commandType: System.Data.CommandType.StoredProcedure));
        if (raw is null) throw new RegistrationConflictException("La OC no existe.");
        var row = (IDictionary<string, object>)raw;
        return new(order.Series, order.Number, Text(row, "Cod_Proveedor"), Text(row, "Cod_StaOrdComp"), Text(row, "Cod_ClaOrdComp"), Text(row, "Cod_Grupo"));
    }
    public static async Task<IReadOnlyList<PendingRequirement>> Pending(SqlConnection connection, OrderRegistrationHeader header, string op, string item, SqlTransaction? transaction, int timeout, CancellationToken ct)
    {
        // Only NPs already linked to a manufacturing group on this OC may be used.
        const string linked = "dbo.PA_ES_ORDPRO_S0001";
        var exists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(linked, new { Op = op, header.Series, header.Number }, transaction, cancellationToken: ct, commandType: System.Data.CommandType.StoredProcedure));
        if (exists == 0) throw new RegistrationConflictException("La OP no pertenece a un grupo de manufactura asociado a esta OC.");
        var rows = await connection.QueryAsync(new CommandDefinition("dbo.UP_SEL_REQUEXCOMPRARLOG", new
        {
            COD_GRUPOLOG = "",
            COD_ORDPRO = op,
            COD_FAMITEM = item[..2],
            COD_PROVEEDOR = header.SupplierCode,
            COD_ITEM = item,
            SER_ORDCOMP = header.Series,
            COD_ORDCOMP = header.Number,
            DES_PRESENTACION = ""
        }, transaction, commandType: CommandType.StoredProcedure, commandTimeout: timeout, cancellationToken: ct));
        return rows.Select(raw =>
        {
            var row = (IDictionary<string, object>)raw;
            var required = Number(row, "CANTXCOMPRAR");
            var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new[] { Text(row, "COD_FABRICA"), Text(row, "COD_ORDPRO"), Text(row, "COD_PRESENT"), Text(row, "COD_COMPEST"), Text(row, "COD_ITEM"), Text(row, "COD_COMB"), Text(row, "COD_COLOR"), Text(row, "COD_TALLA"), Text(row, "COD_DESTINO"), Text(row, "COD_ESTCLI"), required.ToString(CultureInfo.InvariantCulture) }))));
            return new PendingRequirement(key, Text(row, "COD_FABRICA"), Text(row, "COD_ORDPRO"), (int)Number(row, "COD_PRESENT"), Text(row, "PRESENTACION"), Text(row, "COD_COMPEST"), Text(row, "COD_ITEM"), Text(row, "ITEM"), Text(row, "COD_COMB"), Text(row, "COD_COLOR"), Text(row, "COD_TALLA"), Text(row, "COD_DESTINO"), Text(row, "COD_ESTCLI"), Text(row, "COD_PROV"), required, QuantityRules.RoundRequiredQuantity(required), Number(row, "PRECIO"));
        }).Where(r => r.Required > 0).DistinctBy(r => r.Key).ToList();
    }
}
