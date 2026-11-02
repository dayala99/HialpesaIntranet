using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
namespace Intranet.Data;
internal static class AnalisisRbHistorySchema {
 internal static async Task<bool> UsesSpanish(SqlConnection connection,CancellationToken ct){try{return await connection.QuerySingleAsync<bool>(new CommandDefinition("dbo.PA_LG_ANALISIS_RB_HISTORIAL_S0004",commandType:CommandType.StoredProcedure,cancellationToken:ct));}catch(SqlException e)when(e.Number==2812){throw new ArgumentException("Falta ejecutar 05_LG_ANALISIS_RB_HISTORIAL_Busqueda.sql en HIALPESA.",e);}}
}
