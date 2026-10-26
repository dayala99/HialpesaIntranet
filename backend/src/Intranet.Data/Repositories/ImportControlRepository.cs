using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using Intranet.Entity;
using Microsoft.Data.SqlClient;
namespace Intranet.Data;
public sealed partial class ImportControlRepository(GestionConnectionFactory factory):IImportControlRepository
{
 private static async Task<T> Pending<T>(Func<Task<T>> action){try{return await action();}catch(SqlException e) when(e.Number==2812){throw new ImportControlPendingException();}}
 public Task<object> Search(ImportControlFilter f,CancellationToken ct)=>Pending(()=>SearchCore(f,ct));
 public Task<ImportControlCatalogs> Catalogs(CancellationToken ct)=>Pending(()=>CatalogsCore(ct));
 public Task<object> Annexes(string type,string search,CancellationToken ct)=>Pending(()=>AnnexesCore(type,search,ct));
 public Task<ImportControlDetail?> Detail(string number,CancellationToken ct)=>Pending(()=>DetailCore(number,ct));
 public Task<string> Save(string? number,ImportControlSave r,string user,CancellationToken ct)=>Pending(()=>SaveCore(number,r,user,ct));
 public async Task Delete(string number,string version,string user,CancellationToken ct)=>await Pending(async()=>{await DeleteCore(number,version,user,ct);return true;});
 private static CommandDefinition Query(string name,object? p,CancellationToken ct,SqlTransaction? tx=null)=>new("dbo."+name,p,tx,commandTimeout:90,commandType:CommandType.StoredProcedure,cancellationToken:ct);
 private static Dictionary<string,object?> Row(object row)=>((IDictionary<string,object>)row).ToDictionary(x=>x.Key.ToLowerInvariant(),x=>x.Value is string s?(object?)s.Trim():x.Value);
 private static string Text(Dictionary<string,object?> r,string key)=>Convert.ToString(r.GetValueOrDefault(key))?.Trim()??"";
 private static DateTime? Date(Dictionary<string,object?> r,string key)=>r.GetValueOrDefault(key) is DateTime d?d:null;
 private static bool Flag(Dictionary<string,object?> r,string key)=>r.GetValueOrDefault(key) is bool b&&b;
 private static string Version(Dictionary<string,object?> r)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(r.OrderBy(x=>x.Key).ToDictionary(x=>x.Key,x=>x.Value)))));
 private async Task<object> SearchCore(ImportControlFilter f,CancellationToken ct){
  await using var c=await factory.OpenAsync(ct);using var results=await c.QueryMultipleAsync(Query("PA_LG_IMPORTACIONES_S0009",new{f.Mode,f.Number,f.AnnexType,f.Annex,f.Status,f.Guide,f.Origin,f.Item,f.Country,f.Associated,f.Policy,f.Year,f.Month,f.DocumentType,f.DocumentSeries,f.DocumentNumber,f.ItemType,f.Factory,f.ProductionOrder,From=f.From?.Date,To=f.To?.Date,Offset=(f.Page-1)*100},ct));
  var total=await results.ReadSingleAsync<int>();var rows=(await results.ReadAsync()).Select(x=>Row((object)x)).ToArray();return new{rows,totalRows=total,page=f.Page,totalPages=Math.Max(1,(int)Math.Ceiling(total/100d)),hasMore=f.Page*100<total};
 }
 private async Task<ImportControlCatalogs> CatalogsCore(CancellationToken ct){await using var c=await factory.OpenAsync(ct);using var result=await c.QueryMultipleAsync(Query("PA_LG_IMPORTACIONES_S0005",null,ct));var options=(await result.ReadAsync<ImportControlCatalog>()).ToList();var automatic=await result.ReadSingleAsync<bool>();var supplement=await ReadSupplement(c,"",ct);return new(options.Concat(supplement.Options).ToList(),automatic);}
 private async Task<object> AnnexesCore(string type,string search,CancellationToken ct){await using var c=await factory.OpenAsync(ct);return (await c.QueryAsync(Query("PA_CN_ANEXOSCONTABLES_S0002",new{Type=type,Search=search.Trim()},ct))).Select(x=>Row((object)x)).ToArray();}
 private static async Task<(ImportControlDetail Detail,Dictionary<string,object?> Raw)?> Read(SqlConnection c,string number,CancellationToken ct,SqlTransaction? tx=null){
  var data=await c.QuerySingleOrDefaultAsync(Query("PA_LG_IMPORTACIONES_S0004",new{Number=number,Lock=tx is not null},ct,tx));if(data is null)return null;var r=Row((object)data);
  var v=new ImportControlValues{Number=Text(r,"num_importacion"),AnnexType=Text(r,"cod_tipanex"),Annex=Text(r,"cod_anxo"),RegisteredAt=Date(r,"fec_regdoc")??DateTime.Today,ItemType=Text(r,"cod_tipite"),Currency=Text(r,"cod_moneda"),Origin=Text(r,"cod_origenimportacion"),Shipment=Text(r,"cod_embarque"),Country=Text(r,"id_pais_dua"),Area=Text(r,"cod_arearesponsable_asociada"),Guide=Text(r,"num_guia"),GuideDate=Date(r,"fecha_guia"),PaymentDate=Date(r,"fecha_pago"),DueDate=Date(r,"fec_vendoc"),Observations=Text(r,"observacion"),Partial=Text(r,"flg_liquidacion_parcial")=="S",Principal=Text(r,"num_importacion_inicial")};
  var supplement=await ReadSupplement(c,number,ct,tx);bool edit=Flag(r,"canedit");return(new(v.Number,Text(r,"flg_status"),Text(r,"annexname"),v,SupplementVersion(r,supplement),edit,edit&&!Flag(r,"deleteblocked"),supplement.Documents,supplement.Concepts,supplement.Conditions,supplement.Entries,supplement.Order),r);
 }
 private async Task<ImportControlDetail?> DetailCore(string number,CancellationToken ct){await using var c=await factory.OpenAsync(ct);return(await Read(c,number,ct))?.Detail;}
 public static DynamicParameters Parameters(string action,ImportControlValues v,string user){
  var p=new DynamicParameters();p.Add("Accion",action);p.Add("Num_Importacion",v.Number.Trim());p.Add("Ser_OrdComp","");p.Add("Cod_OrdComp","");
  p.Add("Num_Guia",v.Guide.Trim());p.Add("Fecha_Guia",v.GuideDate?.Date);p.Add("Cod_Embarque",v.Shipment.Trim());p.Add("Cod_OrigenImportacion",v.Origin.Trim());p.Add("Observacion",v.Observations.Trim());p.Add("Cod_TipAnex",v.AnnexType.Trim());p.Add("Cod_Anxo",v.Annex.Trim());p.Add("Cod_TipPro","I");p.Add("Fec_RegDoc",v.RegisteredAt.Date);p.Add("ConfirmacionPedido","");p.Add("Proforma","");p.Add("Cod_TipIte",v.ItemType.Trim());p.Add("Cod_Moneda",v.Currency.Trim());p.Add("cod_usuario",user);p.Add("Flg_Liquidacion_Parcial",v.Partial?"S":"N");p.Add("Num_Importacion_Inicial",v.Partial?v.Principal.Trim():"");p.Add("ID_PAIS_DUA",v.Country.Trim());p.Add("Fec_Pago",v.PaymentDate?.Date);p.Add("fec_vendoc",v.DueDate?.Date);p.Add("Cod_AreaResponsable",v.Area.Trim());return p;
 }
 private async Task<string> SaveCore(string? number,ImportControlSave request,string user,CancellationToken ct){
  await using var c=await factory.OpenAsync(ct);await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
  try{
   var v=request.Values;bool create=number is null;ImportControlDetail? previous=null;
   if(!create){var current=(await Read(c,number!,ct,tx))?.Detail??throw new RegistrationConflictException("La importación ya no existe.");
    previous=current;if(current.Version!=request.Version)throw new RegistrationConflictException("Los datos cambiaron. Regresa al listado y vuelve a abrir la importación.");
    if(!current.CanEdit)throw new ArgumentException("El estado de la importación no permite modificaciones.");
    if(current.Status!="R"&&(v.AnnexType!=current.Values.AnnexType||v.Annex!=current.Values.Annex))throw new ArgumentException("No puede cambiar el anexo en este estado.");
    v.Number=number!;v.Partial=current.Values.Partial;v.Principal=current.Values.Principal;
   }else{
    var automatic=await c.ExecuteScalarAsync<string>(Query("PA_TG_CONTROL_S0001",new{LockRow=true},ct,tx));
    if(automatic?.Trim()=="N")v.Number="";else if(string.IsNullOrWhiteSpace(v.Number))throw new ArgumentException("Ingresa el número de importación: Gestión utiliza numeración manual.");
    await c.ExecuteAsync(Query("PA_TG_ANOCONTROL_S0001",new{Year=v.RegisteredAt.Year.ToString()},ct,tx));
   }
   var saved=await c.QuerySingleAsync(Query("UP_MAN_LG_IMPORTACIONES",Parameters(create?"I":"U",v,user),ct,tx));var savedNumber=Text(Row((object)saved),"num_importacion");
   if(string.IsNullOrWhiteSpace(savedNumber))throw new RegistrationConflictException("Gestión no devolvió el número de importación.");
   var documentChanges=await SaveDocuments(c,tx,savedNumber,request,previous,user,ct);
   if(await SaveChildren(c,tx,savedNumber,request,previous,ct)||documentChanges){v.Number=savedNumber;await c.QuerySingleAsync(Query("UP_MAN_LG_IMPORTACIONES",Parameters("U",v,user),ct,tx));}
   await tx.CommitAsync(ct);return savedNumber;
  }catch{if(tx.Connection is not null)await tx.RollbackAsync(CancellationToken.None);throw;}
 }
 private async Task DeleteCore(string number,string version,string user,CancellationToken ct){
  await using var c=await factory.OpenAsync(ct);await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
  try{var current=(await Read(c,number,ct,tx))?.Detail??throw new RegistrationConflictException("La importación ya no existe.");
   if(current.Version!=version)throw new RegistrationConflictException("Los datos cambiaron. Vuelve a consultar la importación antes de eliminarla.");
   if(!current.CanDelete)throw new ArgumentException("No se puede eliminar: revisa el estado, documentos, pagos o liquidaciones asociadas en Gestión.");
   await c.ExecuteAsync(Query("UP_MAN_LG_IMPORTACIONES",Parameters("D",current.Values,user),ct,tx));
   await c.ExecuteAsync(Query("PA_LG_IMPORTACIONES_D0001",new{Number=number},ct,tx));await tx.CommitAsync(ct);
  }catch{if(tx.Connection is not null)await tx.RollbackAsync(CancellationToken.None);throw;}
 }
}
