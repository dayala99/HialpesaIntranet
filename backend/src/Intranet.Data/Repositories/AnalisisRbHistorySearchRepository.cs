using System.Data;
using Dapper;
using Intranet.Entity;
using Microsoft.Data.SqlClient;
using System.Text.Json;
namespace Intranet.Data;
public sealed class AnalisisRbHistorySearchRepository(GestionConnectionFactory factory,IConfiguration configuration):IAnalisisRbHistorySearchRepository {
 public static string? SavedType(string? json){if(string.IsNullOrWhiteSpace(json))return null;try{using var doc=JsonDocument.Parse(json);var root=doc.RootElement;if(root.ValueKind!=JsonValueKind.Object)return null;if(!root.TryGetProperty("source",out var source)&&!root.TryGetProperty("Source",out source))return null;if(source.ValueKind!=JsonValueKind.Object)return null;if(!source.TryGetProperty("garmentType",out var type)&&!source.TryGetProperty("GarmentType",out type))return null;return type.ValueKind==JsonValueKind.String?type.GetString()?.Trim():null;}catch(JsonException){return null;}}
 private static async Task<T> Installed<T>(Func<Task<T>> read){try{return await read();}catch(SqlException e)when(e.Number==2812){throw new ArgumentException("Falta ejecutar 05_LG_ANALISIS_RB_HISTORIAL_Busqueda.sql en HIALPESA.",e);}}
 public Task<AnalisisRbHistorySearchPage> Search(string? np,decimal? price,decimal? time,string? type,long? cursor,CancellationToken ct)=>Installed(async()=>{
 await using var c=await factory.OpenAsync(ct);var matches=new List<AnalisisRbHistorySearchRow>();var after=cursor;
 while(matches.Count<51){ct.ThrowIfCancellationRequested();var batch=(await c.QueryAsync<AnalisisRbHistorySearchRow>(new CommandDefinition("dbo.PA_LG_ANALISIS_RB_HISTORIAL_S0002",new{Cod_Fabrica=configuration["AnalisisRb:Factory"]??"001",Cod_OrdPro=np,Imp_Precio_Comercial=price,Min_Costura=time,Num_Registro_Anterior=after},commandType:CommandType.StoredProcedure,commandTimeout:30,cancellationToken:ct))).ToList();
 foreach(var row in batch){row.GarmentType=SavedType(row.SnapshotJson);row.SnapshotJson=null;row.SavedAtUtc=DateTime.SpecifyKind(row.SavedAtUtc,DateTimeKind.Utc);if(type==null||string.Equals(row.GarmentType,type,StringComparison.OrdinalIgnoreCase)){matches.Add(row);if(matches.Count==51)break;}}
 if(batch.Count<200||matches.Count>=51)break;after=batch.Last().Id;
 }
 var more=matches.Count>50;var rows=matches.Take(50).ToList();return new AnalisisRbHistorySearchPage(rows,more,more?rows.Last().Id:null);
 });
 public Task<IReadOnlyList<AnalisisRbType>> Types(CancellationToken ct)=>Installed<IReadOnlyList<AnalisisRbType>>(async()=>{await using var c=await factory.OpenAsync(ct);long cursor=long.MaxValue;var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
 while(true){var batch=(await c.QueryAsync<TypeRecord>(new CommandDefinition("dbo.PA_LG_ANALISIS_RB_HISTORIAL_S0003",new{Cod_Fabrica=configuration["AnalisisRb:Factory"]??"001",Num_Registro_Anterior=cursor},commandType:CommandType.StoredProcedure,commandTimeout:30,cancellationToken:ct))).ToList();foreach(var row in batch){var name=SavedType(row.Json);if(!string.IsNullOrWhiteSpace(name))names.Add(name);}if(batch.Count<200)break;cursor=batch.Last().Id;}
 return names.OrderBy(x=>x,StringComparer.OrdinalIgnoreCase).Select(x=>new AnalisisRbType(x,x)).ToList();});
 private sealed class TypeRecord{public long Id{get;set;}public string? Json{get;set;}}
}
