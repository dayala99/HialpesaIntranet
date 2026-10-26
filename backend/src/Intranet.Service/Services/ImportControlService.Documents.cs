using Intranet.Entity;
namespace Intranet.Service;
public sealed partial class ImportControlService {
 public Task<ServiceResult> DocumentChoices(string kind,string annexType,string annex,string search,CancellationToken ct)=>Read(async()=>{
 if(!new[]{"catalogs","orders","policies","annex","exchangeRate","orderDetail","conceptEquiv","paymentForms"}.Contains(kind))throw new ArgumentException("Consulta de documentos inválida.");if((kind=="exchangeRate"||kind=="conceptEquiv")&&(!DateTime.TryParseExact(search,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out var date)||date.Year<1900||date.Year>2100))throw new ArgumentException("Indica una fecha de emisión válida para consultar el tipo de cambio.");if(kind=="orderDetail"&&!System.Text.RegularExpressions.Regex.IsMatch(search??"","^[A-Za-z0-9]\\|[A-Za-z0-9]{1,3}\\|[A-Za-z0-9]{1,6}$"))throw new ArgumentException("Completa el tipo de almacén, serie y número de orden de compra.");return Ok(await repository.DocumentChoices(kind,annexType,annex,search??"",ct));});
 public Task<ServiceResult> Shipments(CancellationToken ct)=>Read(async()=>Ok(await repository.Shipments(ct)));
 public Task<ServiceResult> SaveShipment(ImportControlShipmentSave r,CancellationToken ct)=>Read(async()=>{
 if(!identity.CanRegister(User)||!Actor.WriteRequested)throw new UnauthorizedAccessException();
 if(!new[]{"I","U","D"}.Contains(r.Action)||string.IsNullOrWhiteSpace(r.Code)||r.Code.Length>3||string.IsNullOrWhiteSpace(r.Description)||r.Description.Length>30)throw new ArgumentException("Revisa el código y descripción del embarque.");
 if(r.Action!="I"&&!System.Text.RegularExpressions.Regex.IsMatch(r.Version??"","^[A-F0-9]{64}$"))throw new ArgumentException("Vuelve a consultar el embarque.");
 return Ok(await repository.SaveShipment(r,identity.GetUser(User),ct));});
 public static void ValidateDocuments(ImportControlSave r){
 if(r.DocumentEntries is {} list){
 if(list.Count>3||list.Any(d=>d is null)||list.Select(d=>d.Kind).Distinct().Count()!=list.Count)throw new ArgumentException("Revisa los documentos del registro.");
 foreach(var d in list){
 if(!new[]{"PR","CP","FI"}.Contains(d.Kind)||d.Type!=d.Kind||string.IsNullOrWhiteSpace(d.Series)||d.Series.Length>(d.Kind=="FI"?4:3)||string.IsNullOrWhiteSpace(d.Number)||d.Number.Length>15||string.IsNullOrWhiteSpace(d.AnnexType)||d.AnnexType.Length!=1||string.IsNullOrWhiteSpace(d.Annex)||d.Annex.Length>4||string.IsNullOrWhiteSpace(d.ItemType)||d.ItemType.Length>3||string.IsNullOrWhiteSpace(d.Currency)||d.Currency.Length>3||string.IsNullOrWhiteSpace(d.Description)||d.Description.Length>100||d.Correlative.Length>12)throw new ArgumentException("Completa los datos obligatorios del documento.");
 if(d.IssuedAt.Year<1900||d.IssuedAt.Year>2100||d.RegisteredAt.Year<2000||d.RegisteredAt.Year>2100||d.DueDate is {} due&&(due.Year<1900||due.Year>2100))throw new ArgumentException("Revisa las fechas del documento.");
 if(new[]{d.Soles,d.Dollars,d.Other,d.TotalSoles,d.TotalDollars,d.TotalOther}.Any(v=>v<0||v>99999999999.99m||decimal.Round(v,2)!=v)||d.ExchangeRate<=0||d.ExchangeRate>9999999.999m||decimal.Round(d.ExchangeRate,3)!=d.ExchangeRate||d.Equivalence<0||d.Equivalence>99999.999999m||decimal.Round(d.Equivalence,6)!=d.Equivalence)throw new ArgumentException("Revisa los importes y el tipo de cambio del documento.");
 }
 }
 if(r.Order is {} o&&(string.IsNullOrWhiteSpace(o.Warehouse)||o.Warehouse.Length!=1||string.IsNullOrWhiteSpace(o.Series)||o.Series.Length>3||string.IsNullOrWhiteSpace(o.Number)||o.Number.Length>6))throw new ArgumentException("Selecciona una orden de compra válida.");
 if(r.Policy is {} p&&(string.IsNullOrWhiteSpace(p.Correlative)||!System.Text.RegularExpressions.Regex.IsMatch(p.Correlative,"^[0-9]{12}$")))throw new ArgumentException("Selecciona una póliza válida.");
 }
}
