using Intranet.Entity;
using System.Text.RegularExpressions;
namespace Intranet.Service;
public sealed class AnalisisRbHistoryService(IAnalisisRbHistoryRepository repository,GestionIdentity identity,RequestActor actor):ApplicationService(actor),IAnalisisRbHistoryService
{
 public static AnalisisRbSnapshot BuildSnapshot(AnalisisRbSave request,DateTime analysisDate)
 {
  var source=request.Source??throw new ArgumentException("Consulta una NP antes de guardar.");
  var np=source.Np?.Trim().ToUpperInvariant()??"";
  if(!Regex.IsMatch(np,"^[A-Z0-9]{5}$"))throw new ArgumentException("La NP debe tener 5 caracteres.");
  if(request.RequestId==Guid.Empty)throw new ArgumentException("Falta el identificador del guardado.");
  if(source.ProgrammedQuantity<0||source.CommercialPrice is null||source.SewingTime is null||source.CommercialPrice<0||source.SewingTime<=0||request.UsedTime<=0||request.ServicePrice<0||request.OtherExpenses<0)throw new ArgumentException("Revisa precio comercial, tiempos, precio por servicio y otros gastos antes de guardar.");
  var price=source.CommercialPrice.Value;var total=source.SewingTime.Value;
  if(new[]{price,total,request.UsedTime,request.ServicePrice,request.OtherExpenses}.Any(n=>n>100000000m||decimal.Round(n,6)!=n)||decimal.Round(price,4)!=price)throw new ArgumentException("Los valores exceden la precision admitida para guardar el analisis.");
  if(new[]{source.ClientName,source.LineName,source.ProductCategory,source.FabricDescription,source.ColorName}.Any(s=>s?.Length>4000))throw new ArgumentException("La cabecera del analisis es demasiado extensa.");
  if(source.ImagePath?.Length>1024)throw new ArgumentException("La ruta de imagen supera los 1024 caracteres permitidos.");
  var partial=request.PartialSelection;
  if(partial is not null){
   if(!Regex.IsMatch(partial.Tariff??"","^[A-Z0-9]{5}$")||!Regex.IsMatch(partial.Variant??"","^[A-Z0-9]$")||!Regex.IsMatch(partial.Sector??"","^[A-Z0-9]{1,10}$")||string.IsNullOrWhiteSpace(partial.SectorName)||partial.SectorName.Length>100||partial.SectorTotalTime<=0||partial.SectorTotalTime>100000000m||partial.Operations is null||partial.Operations.Count is <1 or >3000)throw new ArgumentException("Revisa el sector y las operaciones del análisis parcial.");
   if(partial.Operations.Any(row=>row is null||row.StandardTime<0||row.StandardTime>100000000m||decimal.Round(row.StandardTime,6)!=row.StandardTime||string.IsNullOrWhiteSpace(row.BlockCode)||string.IsNullOrWhiteSpace(row.Sequence)||string.IsNullOrWhiteSpace(row.Code)||new[]{row.BlockCode,row.Sequence,row.Code,row.Variant}.Any(value=>value?.Length>100)||new[]{row.BlockName,row.Description}.Any(value=>value?.Length>4000)))throw new ArgumentException("Revisa las operaciones seleccionadas.");
   if(partial.SelectedTime<=0||partial.SelectedTime>partial.SectorTotalTime||partial.Operations.Select(row=>(row.BlockCode,row.Sequence,row.Code,row.Variant)).Distinct().Count()!=partial.Operations.Count)throw new ArgumentException("La suma o selección de operaciones no es válida.");
   partial=partial with{Operations=partial.Operations.ToArray()};
  }
  var sector=request.SectorAnalysis;
  if(sector is not null){
   if(!Regex.IsMatch(sector.Tariff??"","^[A-Z0-9]{5}$")||!Regex.IsMatch(sector.Variant??"","^[A-Z0-9]$")||!Regex.IsMatch(sector.Sector??"","^[A-Z0-9]{1,10}$")||string.IsNullOrWhiteSpace(sector.SectorName)||sector.SectorName.Length>100||sector.TotalTime<=0||sector.TotalTime>100000000m||decimal.Round(sector.TotalTime,6)!=sector.TotalTime||sector.TotalOperations is <1 or >3000||sector.SelectedOperations<1||sector.SelectedOperations>sector.TotalOperations||sector.IsComplete!=(sector.SelectedOperations==sector.TotalOperations))throw new ArgumentException("Revisa el sector y el tipo de análisis.");
   if(sector.IsComplete){if(partial is not null||request.UsedTime!=sector.TotalTime)throw new ArgumentException("El análisis completo debe incluir todo el tiempo del sector y no un listado parcial.");}
   else if(partial is null||partial.Operations.Count!=sector.SelectedOperations||partial.SelectedTime!=request.UsedTime||partial.Tariff!=sector.Tariff||partial.Variant!=sector.Variant||partial.Sector!=sector.Sector||partial.SectorTotalTime!=sector.TotalTime)throw new ArgumentException("La selección parcial no coincide con el sector del análisis.");
   total=sector.TotalTime;
  }
  var requotation=request.Requotation is {Enabled:false}?new AnalisisRbRequotation(false,null,null):request.Requotation;
  if(requotation is not null){
   if(requotation.BaseServicePrice is {} baseValue&&(Math.Abs(baseValue)>100000000m||decimal.Round(baseValue,6)!=baseValue)||requotation.Percentage is {} rateValue&&(Math.Abs(rateValue)>100000000m||decimal.Round(rateValue,16)!=rateValue))throw new ArgumentException("Revisa la precisión de Precio Servicio y Porcentaje.");
   if(requotation.Enabled){if(requotation.BaseServicePrice is not {} basePrice||requotation.Percentage is not {} rate||basePrice<0||rate<0)throw new ArgumentException("Completa Precio Servicio y Porcentaje para la recotización.");
    var expected=decimal.Round(basePrice*rate,6,MidpointRounding.AwayFromZero);if(Math.Abs(expected-request.ServicePrice)>.000001m)throw new ArgumentException("Precio por Servicio no coincide con Precio Servicio multiplicado por Porcentaje.");
   }
  }
  var commercial=request.UsedTime*price/total;var difference=commercial-request.ServicePrice;
  var complete=sector?.IsComplete??(partial is null&&total==request.UsedTime);
  var calculated=new AnalisisRbCalculated(price/total,commercial,request.ServicePrice/request.UsedTime,difference,commercial==0?null:difference/commercial,total-request.UsedTime,complete?"TIEMPO COMPLETO":"TIEMPO PARCIAL",complete?(sector is null?"COSTURA COMPLETA":sector.SectorName.ToUpperInvariant()+(sector.Sector=="CS"?" COMPLETA":" COMPLETO")):(sector?.SectorName.ToUpperInvariant()??partial?.SectorName.ToUpperInvariant()??"COSTURA")+" PARCIAL",request.ServicePrice+request.OtherExpenses);
  return new(source with{Np=np},request.UsedTime,request.ServicePrice,request.OtherExpenses,calculated,analysisDate.Date,sector is not null?"Libro1-2026-10-v3-sector":partial is null?"Libro1-2026-10-v1":"Libro1-2026-10-v2-parcial"){PartialSelection=partial,Requotation=requotation,SectorAnalysis=sector};
 }
 public async Task<ServiceResult> Save(AnalisisRbSave request,CancellationToken ct)
 {
  var user=identity.GetUser(User);
  if(!identity.CanRegister(User)||!Actor.WriteRequested)throw new UnauthorizedAccessException();
  var snapshot=BuildSnapshot(request,DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-5)).Date);
  return Ok(await repository.Insert(request.RequestId,snapshot,user,Actor.TraceIdentifier,ct));
 }
 public async Task<ServiceResult> History(string np,long? id,int page,CancellationToken ct)
 {
  identity.GetUser(User);var normalized=(np??"").Trim().ToUpperInvariant();
  if(!Regex.IsMatch(normalized,"^[A-Z0-9]{5}$")||page<1||page>100000||id<=0)throw new ArgumentException("Revisa la NP y la pagina del historial.");
  return Ok(await repository.Read(normalized,id,page,ct));
 }
}

