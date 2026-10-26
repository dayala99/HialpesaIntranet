using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using Intranet.Entity;
using Microsoft.Data.SqlClient;
namespace Intranet.Data;
public sealed partial class ImportControlRepository
{
 private sealed record Supplement(ImportControlDocuments Documents,IReadOnlyList<ImportControlConcept> Concepts,IReadOnlyList<ImportControlCondition> Conditions,IReadOnlyList<ImportControlCatalog> Options,IReadOnlyList<ImportControlDocument> Entries,ImportControlOrder? Order);
 private static async Task<Supplement> ReadSupplement(SqlConnection c,string number,CancellationToken ct,SqlTransaction? tx=null){
  using var r=await c.QueryMultipleAsync(Query("PA_LG_IMPORTACIONES_S0006",new{Number=number},ct,tx));
  var documents=await r.ReadSingleOrDefaultAsync<ImportControlDocuments>()??new("","","","","");
  var concepts=(await r.ReadAsync<ImportControlConcept>()).ToList();var conditions=(await r.ReadAsync<ImportControlCondition>()).ToList();var options=(await r.ReadAsync<ImportControlCatalog>()).ToList();using var docs=await c.QueryMultipleAsync(Query("PA_LG_IMPORTACIONES_S0007",new{Number=number},ct,tx));var entries=(await docs.ReadAsync<ImportControlDocument>()).ToList();var order=await docs.ReadSingleOrDefaultAsync<ImportControlOrder>();return new(documents,concepts,conditions,options,entries,order);
 }
 private static string SupplementVersion(Dictionary<string,object?> header,Supplement s)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new{Header=header.OrderBy(x=>x.Key).ToDictionary(x=>x.Key,x=>x.Value),s.Documents,s.Concepts,s.Conditions,s.Entries,s.Order}))));
 public Task<object> Invoices(string number,CancellationToken ct)=>Pending(async()=>{await using var c=await factory.OpenAsync(ct);return (object)(await c.QueryAsync(Query("SM_MUESTRA_FACTURAS_ASOCIADOS",new{NUM_IMPORTACION=number},ct))).Select(x=>Row((object)x)).ToList();});
 public static bool SameConcept(ImportControlConcept a,ImportControlConcept b)=>a.Code.Trim().Equals(b.Code.Trim(),StringComparison.OrdinalIgnoreCase)&&a.Dollars==b.Dollars&&a.Soles==b.Soles&&a.ExchangeRate==b.ExchangeRate&&a.Other==b.Other&&(a.DocumentNumber??"").Trim()==(b.DocumentNumber??"").Trim();
 private static object ConceptParameters(string action,string number,ImportControlConcept c)=>new{Accion=action,Num_Importacion=number,cod_Concepto=c.Code.Trim().ToUpperInvariant(),Dol_Importe=c.Dollars,Sol_Importe=c.Soles,Tipo_Cambio=c.ExchangeRate,Otros_Importe=c.Other,Num_Docum=(c.DocumentNumber??"").Trim()};
 private static async Task<bool> SaveChildren(SqlConnection c,SqlTransaction tx,string number,ImportControlSave request,ImportControlDetail? previous,CancellationToken ct){
  var changed=false;var oldConcepts=previous?.Concepts??[];var oldConditions=previous?.Conditions??[];
  if(request.Concepts is {} concepts){
   var incoming=concepts.Select(x=>x with{Code=x.Code.Trim().ToUpperInvariant(),DocumentNumber=(x.DocumentNumber??"").Trim()}).ToList();
   var before=oldConcepts.ToDictionary(x=>x.Code.Trim(),StringComparer.OrdinalIgnoreCase);
   var removed=oldConcepts.Where(x=>!incoming.Any(y=>y.Code.Equals(x.Code,StringComparison.OrdinalIgnoreCase))).ToList();
   var modified=incoming.Where(x=>!before.TryGetValue(x.Code,out var old)||!SameConcept(x,old)).ToList();
   if(modified.Count>0){var catalog=(await ReadSupplement(c,number,ct,tx)).Options.Where(o=>o.Kind=="concepts").Select(o=>o.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);if(modified.Any(x=>!catalog.Contains(x.Code)))throw new ArgumentException("Selecciona un concepto válido de Gestión.");}
   foreach(var row in removed){await c.ExecuteAsync(Query("UP_MAN_LG_IMPORTACIONES_CONCEPTOS",ConceptParameters("D",number,row),ct,tx));changed=true;}
   foreach(var row in modified){await c.ExecuteAsync(Query("UP_MAN_LG_IMPORTACIONES_CONCEPTOS",ConceptParameters(before.ContainsKey(row.Code)?"U":"I",number,row),ct,tx));changed=true;}
  }
  if(request.Conditions is {} conditions){
   var before=oldConditions.ToDictionary(x=>x.Sequence);if(conditions.Any(x=>x.Sequence.Length>0&&!before.ContainsKey(x.Sequence)))throw new ArgumentException("Una condición de pago cambió. Vuelve a abrir el registro.");
   foreach(var old in oldConditions.Where(x=>!conditions.Any(y=>y.Sequence==x.Sequence))){await c.ExecuteAsync(Query("UP_MAN_LG_Importaciones_Condiciones",new{ACCION="D",NUM_IMPORTACION=number,SECUENCIA=old.Sequence,DES_CONDICION=old.Description},ct,tx));changed=true;}
   foreach(var row in conditions){
    if(row.Sequence.Length==0){await c.ExecuteAsync(Query("UP_MAN_LG_Importaciones_Condiciones",new{ACCION="I",NUM_IMPORTACION=number,SECUENCIA="",DES_CONDICION=row.Description.Trim()},ct,tx));changed=true;}
    else if(before[row.Sequence].Description.Trim()!=row.Description.Trim()){await c.ExecuteAsync(Query("PA_LG_IMPORTACIONES_CONDICIONES_U0001",new{Num_Importacion=number,Secuencia=row.Sequence,Des_Condicion=row.Description.Trim()},ct,tx));changed=true;}
   }
  }
  return changed;
 }
}
