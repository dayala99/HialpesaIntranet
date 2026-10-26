using System.Data;
using Dapper;using Intranet.Entity;using Microsoft.Data.SqlClient;
namespace Intranet.Data;
public sealed partial class ImportControlRepository {
 public Task<object> DocumentChoices(string kind,string annexType,string annex,string search,CancellationToken ct)=>Pending(async()=>{await using var c=await factory.OpenAsync(ct);if(kind=="paymentForms"){return (object)(await c.QueryAsync(Query("PA_LG_CONDVENT_S0001",null,ct))).Select(x=>Row((object)x)).Select(x=>new{code=Text(x,"code"),description=Text(x,"description")}).ToList();}if(kind=="conceptEquiv"){var date=DateTime.ParseExact(search,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture);var value=await c.ExecuteScalarAsync<decimal?>(Query("cn_tipoCambioEquivalencia",new{Fecha=date.Date},ct));return (object)(value.HasValue?new[]{new{equivalence=value.Value}}:Array.Empty<object>()); }if(kind=="orderDetail"){var parts=search.Split('|');return (object)(await c.QueryAsync(Query("CN_datos_ordcomp",new{COD_TIPOALMACEN=parts[0],SER_ORDCOMP=parts[1],COD_ORDCOMP=parts[2]},ct))).Select(x=>Row((object)x)).ToList();}if(kind=="exchangeRate"){var date=DateTime.ParseExact(search,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture);return (object)(await c.QueryAsync(Query("UP_SHOW_CN_TIPOCAMBIO",new{Fecha=date.Date},ct))).Select(x=>Row((object)x)).Select(x=>new{exchangeRate=Convert.ToDecimal(x.GetValueOrDefault("tipo_venta")??0m)}).ToList();}return (object)(await c.QueryAsync(Query("PA_LG_IMPORTACIONES_S0008",new{Kind=kind,AnnexType=annexType,Annex=annex,Search=search.Trim()},ct))).Select(x=>Row((object)x)).ToList();});
 private static async Task<List<ImportControlShipment>> ReadShipments(SqlConnection c,CancellationToken ct,SqlTransaction? tx=null){var rows=(await c.QueryAsync(Query("UP_MAN_TG_TIPEMB",new{Accion="V",Cod_Embarque="",Des_Embarque=""},ct,tx))).Select(x=>Row((object)x));return rows.Select(r=>new ImportControlShipment(Text(r,"cod_embarque"),Text(r,"des_embarque"),Text(r,"flg_vigente"),Version(r))).ToList();}
 public Task<object> Shipments(CancellationToken ct)=>Pending(async()=>{await using var c=await factory.OpenAsync(ct);return (object)await ReadShipments(c,ct);});
 public Task<object> SaveShipment(ImportControlShipmentSave r,string user,CancellationToken ct)=>Pending(async()=>{
 await using var c=await factory.OpenAsync(ct);await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);try{
 var rows=await ReadShipments(c,ct,tx);var old=rows.FirstOrDefault(x=>x.Code.Equals(r.Code.Trim(),StringComparison.OrdinalIgnoreCase));
 if(r.Action=="I"&&old is not null)throw new ArgumentException("El código de embarque ya existe.");
 if(r.Action!="I"&&(old is null||old.Version!=r.Version))throw new RegistrationConflictException("El embarque cambió. Vuelve a consultarlo.");
 await c.ExecuteAsync(Query("UP_MAN_TG_TIPEMB",new{Accion=r.Action,Cod_Embarque=r.Code.Trim().ToUpperInvariant(),Des_Embarque=r.Description.Trim()},ct,tx));await tx.CommitAsync(ct);return (object)new{saved=true};
 }catch{if(tx.Connection is not null)await tx.RollbackAsync(CancellationToken.None);throw;}});
 private static async Task<bool> SaveDocuments(SqlConnection c,SqlTransaction tx,string number,ImportControlSave request,ImportControlDetail? previous,string user,CancellationToken ct){
 var changed=false;
 foreach(var d in request.DocumentEntries??[]){
 var old=previous?.DocumentEntries?.SingleOrDefault(x=>x.Kind==d.Kind);
 if(d.Kind=="FI"&&d.Correlative=="")old=null;
 if(old is not null&&old==d)continue;
 if(old is null&&d.Correlative!=""||old is not null&&(old.Correlative!=d.Correlative||old.Type!=d.Type||old.Series!=d.Series||old.Number!=d.Number||old.IssuedAt.Date!=d.IssuedAt.Date||old.AnnexType!=d.AnnexType||old.Annex!=d.Annex))throw new RegistrationConflictException("La identidad del documento cambió. Vuelve a abrir el registro.");
 var p=new DynamicParameters(new{ACCION=old is null?"I":"U",NUM_CORRE=old?.Correlative??"",COD_TIPANE=d.AnnexType.Trim(),COD_ANEXO=d.Annex.Trim(),COD_TIPDOC=d.Type,SER_DOCUM=d.Series.Trim(),NUM_DOCUM=d.Number.Trim(),COD_TIPPRO="",FEC_EMIDOC=d.IssuedAt.Date,FEC_REGDOC=d.RegisteredAt.Date,COD_TIPITE=d.ItemType.Trim(),COD_MONEDA=d.Currency.Trim(),FACEQUMON=d.Equivalence,TIPO_CAMBIO=d.ExchangeRate,GLOSA=d.Description.Trim(),COD_TIPCAN="FA",COD_USUARIOREGISTRO=user,IMP_OTROSSOL=d.Soles,IMP_OTROSDOL=d.Dollars,IMP_OTROSOTROS=d.Other,SOL_IMPTOT=d.TotalSoles,DOL_IMPTOT=d.TotalDollars,OTR_IMPTOT=d.TotalOther,NUM_IMPORTACION=number});
 if(d.Kind=="FI"){p.Add("FEC_VENDOC",d.DueDate?.Date);p.Add("COD_AREARESPONSABLE",request.Values.Area);}
 var results=(await c.QueryAsync(Query(d.Kind=="FI"?"UP_MAN_CN_DOCUM_ADICIONAL":"UP_MAN_CN_DOCUM_AUXILIAR",p,ct,tx))).ToList();
 if(!results.Any(x=>!string.IsNullOrWhiteSpace(Text(Row((object)x),"num_corre"))))throw new RegistrationConflictException("Gestión no confirmó el documento; no se guardó la importación.");changed=true;
 }
 if(request.Order is {} o&&o!=previous?.Order){await c.ExecuteAsync(Query("UP_MAN_ORDCOMP_IMPORTACION",new{NUM_IMPORTACION=number,COD_TIPOALMACEN=o.Warehouse,SER_ORDCOMP=o.Series,COD_ORDCOMP=o.Number},ct,tx));changed=true;}
 if(request.Policy is {} policy){
 if(!string.IsNullOrWhiteSpace(previous?.Documents?.Policy))throw new ArgumentException("La importación ya tiene póliza asociada.");
 var row=await c.QuerySingleOrDefaultAsync(Query("PA_LG_IMPORTACIONES_S0008",new{Kind="policyRead",AnnexType="",Annex="",Search=policy.Correlative},ct,tx));if(row is null)throw new ArgumentException("La póliza ya no está disponible.");var doc=Row((object)row);
 await c.ExecuteAsync(Query("UP_ASOCIACION_IMPORTACION",new{Opcion="A",Cod_TipAnex=Text(doc,"annextype"),Cod_Anxo=Text(doc,"annex"),Cod_TipDoc=Text(doc,"type"),Ser_Docum=Text(doc,"series"),Num_Docum=Text(doc,"number"),Num_Importacion=number,EsPolizaImportacion="S"},ct,tx));changed=true;
 }
 return changed;
 }
}
