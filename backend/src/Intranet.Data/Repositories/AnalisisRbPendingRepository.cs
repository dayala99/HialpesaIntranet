using System.Data;
using System.Text.RegularExpressions;
using Dapper;
using Intranet.Entity;
using Microsoft.Data.SqlClient;
namespace Intranet.Data;
public sealed class AnalisisRbPendingRepository(GestionConnectionFactory factory,IConfiguration configuration):IAnalisisRbPendingRepository {
 public static string? NormalizeNp(int type,string? np){var value=np?.Trim().ToUpperInvariant();return type==2&&value!=null&&Regex.IsMatch(value,"^[A-Z0-9]{5}$")?value:null;}
 public static bool StatusReady(IEnumerable<string?> values,string kind){var items=values.Select(value=>value?.Trim().ToUpperInvariant()??"").ToList();return items.Count>0&&items.All(value=>kind switch{"oc"=>value=="OK","tariff"=>value=="D","sheet"=>value=="SI",_=>false});}
 public async Task<AnalisisRbPendingResult> Read(CancellationToken ct){
 await using var c=await factory.OpenAsync(ct);
 List<AnalisisRbRow> rows;
 try{rows=(await c.QueryAsync<AnalisisRbRow>(new CommandDefinition("dbo.PA_LG_PCP_SECUENCIADOR_S0001",new{Cod_Fabrica=configuration["AnalisisRb:Factory"]??"001"},commandType:CommandType.StoredProcedure,commandTimeout:120,cancellationToken:ct))).ToList();}
 catch(SqlException e)when(e.Number==2812){throw new ArgumentException("Falta ejecutar 12_PCP_Secuenciador_Optimizado.sql en HIALPESA.",e);}
 var statuses=rows.ToDictionary(row=>row.Np.Trim(),row=>new AnalisisRbPendingIndicators(false,StatusReady([row.TariffStatus],"tariff"),StatusReady([row.TechnicalSheetStatus],"sheet"),"Sin dato",row.TariffStatus?.Trim()??"Sin dato",row.TechnicalSheetStatus?.Trim()??"Sin dato"));
 return new(rows,rows.Count,rows.Count,DateTime.UtcNow){Statuses=statuses};
 }
}
