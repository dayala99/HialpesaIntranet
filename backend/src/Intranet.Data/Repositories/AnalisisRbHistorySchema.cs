using Dapper;
using Microsoft.Data.SqlClient;
namespace Intranet.Data;
internal static class AnalisisRbHistorySchema {
 internal static Task<bool> UsesSpanish(SqlConnection connection,CancellationToken ct)=>connection.QuerySingleAsync<bool>(new CommandDefinition("SELECT CAST(CASE WHEN COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Num_Registro') IS NULL THEN 0 ELSE 1 END AS bit)",cancellationToken:ct));
}
