using Intranet.Entity;
namespace Intranet.Service;
public sealed partial class ImportControlService(IImportControlRepository repository,GestionIdentity identity,RequestActor actor):ApplicationService(actor),IImportControlService
{
 private async Task<ServiceResult> Read(Func<Task<ServiceResult>> action){identity.GetUser(User);try{return await action();}catch(ImportControlPendingException e){return Conflict(new{title=e.Message});}}
 public Task<ServiceResult> Search(ImportControlFilter f,CancellationToken ct)=>Read(async()=>{ValidateFilter(f);return Ok(await repository.Search(f,ct));});
 public Task<ServiceResult> Catalogs(CancellationToken ct)=>Read(async()=>Ok(await repository.Catalogs(ct)));
 public Task<ServiceResult> Annexes(string type,string search,CancellationToken ct)=>Read(async()=>Ok(await repository.Annexes(type,search,ct)));
 public Task<ServiceResult> Detail(string number,CancellationToken ct)=>Read(async()=>await repository.Detail(number,ct) is {} d?Ok(d):NotFound(new{title="La importación no existe."}));
 public Task<ServiceResult> Invoices(string number,CancellationToken ct)=>Read(async()=>await repository.Detail(number,ct) is not null?Ok(await repository.Invoices(number,ct)):NotFound(new{title="La importación no existe."}));
 public Task<ServiceResult> Save(string? number,ImportControlSave r,CancellationToken ct)=>Read(async()=>{
  if(!identity.CanRegister(User)||!Actor.WriteRequested)throw new UnauthorizedAccessException();
  ValidateValues(r.Values);ValidateChildren(r);ValidateDocuments(r);if(number is not null&&!System.Text.RegularExpressions.Regex.IsMatch(r.Version,"^[A-F0-9]{64}$"))throw new ArgumentException("Vuelve a abrir el registro antes de guardar.");
  return Ok(new{number=await repository.Save(number,r,identity.GetUser(User),ct)});
 });
 public Task<ServiceResult> Provision(string number,ImportControlProvision request,CancellationToken ct)=>Read(async()=>{if(!identity.CanRegister(User)||!Actor.WriteRequested)throw new UnauthorizedAccessException();ImportControlProvisioning.ValidateRequest(request);return Ok(await repository.Provision(number,request,identity.GetUser(User),ct));});
 public Task<ServiceResult> Delete(string number,ImportControlDelete r,CancellationToken ct)=>Read(async()=>{
  if(!identity.CanRegister(User)||!Actor.WriteRequested)throw new UnauthorizedAccessException();
  await repository.Delete(number,r.Version,identity.GetUser(User),ct);return Ok(new{deleted=true});
 });
 public static bool HasPrimaryFilter(ImportControlFilter f)=>f.Mode switch{
 "1"=>!string.IsNullOrWhiteSpace(f.Number),"2"=>f.Year!=0||f.Month!=0,"3"=>!string.IsNullOrWhiteSpace(f.DocumentNumber)||!string.IsNullOrWhiteSpace(f.DocumentSeries),"4"=>!string.IsNullOrWhiteSpace(f.Policy),"5"=>!string.IsNullOrWhiteSpace(f.ItemType),"7"=>!string.IsNullOrWhiteSpace(f.Annex),"8"=>!string.IsNullOrWhiteSpace(f.Status),"9"=>!string.IsNullOrWhiteSpace(f.Guide),"0"=>!string.IsNullOrWhiteSpace(f.Origin),"A"=>!string.IsNullOrWhiteSpace(f.Item),"B"=>!string.IsNullOrWhiteSpace(f.Factory)||!string.IsNullOrWhiteSpace(f.ProductionOrder),"C"=>!string.IsNullOrWhiteSpace(f.Associated),"D"=>!string.IsNullOrWhiteSpace(f.Country),_=>false};
 public static void ValidateFilter(ImportControlFilter f){
  if(f.Page<1||f.Page>100000||!new[]{"0","1","2","3","4","5","6","7","8","9","A","B","C","D"}.Contains(f.Mode))throw new ArgumentException("Filtro o página inválidos.");
  bool empty(string? s)=>string.IsNullOrWhiteSpace(s);
  if(HasPrimaryFilter(f)&&(f.Mode switch{"1"=>empty(f.Number),"2"=>f.Year<2000||f.Year>2100||f.Month<1||f.Month>12,"3"=>empty(f.DocumentType)||empty(f.DocumentNumber),"4"=>empty(f.Policy),"5"=>empty(f.ItemType),"7"=>empty(f.AnnexType)||empty(f.Annex),"8"=>empty(f.Status),"9"=>empty(f.Guide),"0"=>empty(f.Origin),"A"=>empty(f.Item),"B"=>empty(f.Factory)||f.ProductionOrder.Trim().Length!=5,"C"=>empty(f.Associated),"D"=>empty(f.Country),_=>false}))throw new ArgumentException("Completa los campos del filtro seleccionado.");
  if((f.From.HasValue||f.To.HasValue)&&!new[]{"0","5","7","8","D"}.Contains(f.Mode))throw new ArgumentException("El filtro seleccionado no utiliza rango de fechas.");
  if(new[]{f.From,f.To}.Any(d=>d.HasValue&&(d.Value.Year<2000||d.Value.Year>2100))||(f.From.HasValue&&f.To.HasValue&&f.From>f.To))throw new ArgumentException("Revisa el rango de fechas.");
 }
 public static void ValidateChildren(ImportControlSave r){
  if(r.Concepts is {} concepts){
   if(concepts.Any(c=>c is null))throw new ArgumentException("Revisa los conceptos del registro.");
   if(concepts.Count>200||concepts.Select(c=>c.Code?.Trim().ToUpperInvariant()).Distinct().Count()!=concepts.Count)throw new ArgumentException("No repitas conceptos en la importación.");
   foreach(var c in concepts){
    if(string.IsNullOrWhiteSpace(c.Code)||c.Code.Trim().Length>3||(c.DocumentNumber?.Length??0)>20)throw new ArgumentException("Revisa el concepto y su número de proforma.");
    if(new[]{c.Dollars,c.Soles,c.Other}.Any(v=>v<0||v>99999999999.99m||decimal.Round(v,2)!=v)||c.ExchangeRate<0||c.ExchangeRate>9999999.999m||decimal.Round(c.ExchangeRate,3)!=c.ExchangeRate)throw new ArgumentException("Revisa los importes y el tipo de cambio del concepto.");
    if((c.Dollars>0||c.Soles>0||c.Other>0)&&c.ExchangeRate<=0)throw new ArgumentException("Ingresa un tipo de cambio mayor que cero.");
   }
  }
  if(r.Conditions is {} conditions){
   if(conditions.Any(c=>c is null||c.Sequence is null))throw new ArgumentException("Revisa las condiciones de pago del registro.");
   var existing=conditions.Where(c=>!string.IsNullOrEmpty(c.Sequence)).Select(c=>c.Sequence).ToArray();
   if(conditions.Count>999||existing.Distinct().Count()!=existing.Length||existing.Any(s=>!System.Text.RegularExpressions.Regex.IsMatch(s,"^[0-9]{3}$")))throw new ArgumentException("Revisa las secuencias de condiciones de pago.");
   if(conditions.Any(c=>string.IsNullOrWhiteSpace(c.Description)||c.Description.Length>100))throw new ArgumentException("La condición de pago es obligatoria y admite hasta 100 caracteres.");
  }
 }
 public static void ValidateValues(ImportControlValues v){
  if(v.RegisteredAt.Year<2000||v.RegisteredAt.Year>2100||new[]{v.GuideDate,v.PaymentDate,v.DueDate}.Any(d=>d.HasValue&&(d.Value.Year<1900||d.Value.Year>2100)))throw new ArgumentException("Revisa las fechas del registro.");
  if(new[]{v.AnnexType,v.Annex,v.ItemType,v.Currency,v.Origin,v.Area}.Any(string.IsNullOrWhiteSpace))throw new ArgumentException("Completa los datos obligatorios de la importación.");
  if(v.Partial&&string.IsNullOrWhiteSpace(v.Principal))throw new ArgumentException("Indica la importación principal de la liquidación parcial.");
  if(v.Partial&&v.Principal.Trim()==v.Number.Trim()&&v.Number.Length>0)throw new ArgumentException("La importación principal debe ser diferente.");
 }
}
