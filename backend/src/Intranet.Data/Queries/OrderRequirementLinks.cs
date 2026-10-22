using Intranet.Entity;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;
namespace Intranet.Data;

public static class OrderRequirementLinks
{
    public static readonly string[] Fields = ["Cod_Fabrica", "Cod_OrdPro", "Cod_Present", "Cod_CompEst", "Cod_Item", "Cod_Comb", "Cod_Color", "Cod_Talla", "Cod_Destino", "Cod_EstCli", "Can_Requerida"];
    public static string Key(IDictionary<string, object> row) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(Fields.Select(f => RegistrationQueries.Text(row, f))))));
    public static async Task<IReadOnlyList<IDictionary<string, object>>> Read(SqlConnection c, SqlTransaction? tx, OrderReference order, string sequence, CancellationToken ct)
    {
        const string sql = "dbo.PA_LG_ORDCOMPITEMREQ_S0001";
        return (await c.QueryAsync(new CommandDefinition(sql, new { order.Series, order.Number, Sequence = sequence, LockRow = tx is not null }, tx, commandTimeout: 60, cancellationToken: ct, commandType: System.Data.CommandType.StoredProcedure))).Select(r => (IDictionary<string, object>)r).ToList();
    }
}
