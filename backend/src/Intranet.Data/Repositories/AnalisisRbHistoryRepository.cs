using System.Data;
using System.Text.Json;
using Dapper;
using Intranet.Entity;
using Microsoft.Data.SqlClient;
namespace Intranet.Data;
public sealed class AnalisisRbHistoryRepository(GestionConnectionFactory factory,IConfiguration configuration):IAnalisisRbHistoryRepository
{
 private string Plant=>configuration["AnalisisRb:Factory"]??"001";
 private static AnalisisRbHistory Utc(AnalisisRbHistory row){row.SavedAtUtc=DateTime.SpecifyKind(row.SavedAtUtc,DateTimeKind.Utc);return row;}
 private static async Task<T> Pending<T>(Func<Task<T>> action){try{return await action();}catch(SqlException e)when(e.Number is 207 or 8144){throw new ArgumentException("Verifica los scripts del historial y la migración 04 de nombres en español.",e);}catch(SqlException e)when(e.Number is 208 or 2812){throw new ArgumentException("Falta registrar los scripts de tabla e historial de Analisis-Rb en la base de datos.",e);}}
 public Task<AnalisisRbHistory> Insert(Guid requestId,AnalisisRbSnapshot snapshot,string user,string traceId,CancellationToken ct)=>Pending(async()=>{
  await using var connection=await factory.OpenAsync(ct);
  var source=snapshot.Source;
  var json=JsonSerializer.Serialize(snapshot,new JsonSerializerOptions(JsonSerializerDefaults.Web));
  var spanish=await AnalisisRbHistorySchema.UsesSpanish(connection,ct);
  object parameters=spanish?new{Cod_Solicitud=requestId,Cod_Fabrica=Plant,Cod_OrdPro=source.Np,Num_Prendas_Programadas=source.ProgrammedQuantity,Imp_Precio_Comercial=source.CommercialPrice,Min_Costura=source.SewingTime,Min_Considerados=snapshot.UsedTime,Imp_Precio_Servicio=snapshot.ServicePrice,Imp_Otros_Gastos=snapshot.OtherExpenses,Des_Ruta_Imagen=source.ImagePath,Txt_Datos_Analisis=json,Cod_Version_Reglas=snapshot.RuleVersion,Cod_Usuario=user,Cod_Traza=traceId}:new{RequestId=requestId,Factory=Plant,source.Np,source.ProgrammedQuantity,source.CommercialPrice,source.SewingTime,snapshot.UsedTime,snapshot.ServicePrice,snapshot.OtherExpenses,source.ImagePath,SnapshotJson=json,snapshot.RuleVersion,GestionUser=user,TraceId=traceId};
  var command=new CommandDefinition("dbo.PA_LG_ANALISIS_RB_HISTORIAL_I0001",parameters,commandType:CommandType.StoredProcedure,commandTimeout:30,cancellationToken:ct);
  return spanish?(await connection.QuerySingleAsync<AnalisisRbHistorialRegistro>(command)).ToContract():Utc(await connection.QuerySingleAsync<AnalisisRbHistory>(command));
 });
 public Task<AnalisisRbHistoryPage> Read(string np,long? id,int page,CancellationToken ct)=>Pending(async()=>{
  await using var connection=await factory.OpenAsync(ct);
  var spanish=await AnalisisRbHistorySchema.UsesSpanish(connection,ct);
  object parameters=spanish?new{Cod_Fabrica=Plant,Cod_OrdPro=np,Num_Registro=id,Num_Desplazamiento=(page-1)*50,Num_Tamano_Pagina=50}:new{Factory=Plant,Np=np,Id=id,Offset=(page-1)*50,PageSize=50};
  using var results=await connection.QueryMultipleAsync(new CommandDefinition("dbo.PA_LG_ANALISIS_RB_HISTORIAL_S0001",parameters,commandType:CommandType.StoredProcedure,commandTimeout:30,cancellationToken:ct));
  var count=await results.ReadSingleAsync<int>();
  var rows=spanish?(await results.ReadAsync<AnalisisRbHistorialRegistro>()).Select(row=>row.ToContract()).ToList():(await results.ReadAsync<AnalisisRbHistory>()).Select(Utc).ToList();
  return new AnalisisRbHistoryPage(rows,count,page,Math.Max(1,(int)Math.Ceiling(count/50d)));
 });
}

