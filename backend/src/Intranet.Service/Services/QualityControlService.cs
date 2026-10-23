using System.ComponentModel.DataAnnotations;
using Intranet.Entity;
namespace Intranet.Service;
public sealed class QualityControlService(IQualityControlRepository repository,GestionIdentity identity,RequestActor actor,ILogger<QualityControlService> logger):ApplicationService(actor),IQualityControlService
{
 public async Task<ServiceResult> Read(string batch,int sequence,CancellationToken ct){
  var user=identity.GetUser(User);if(identity.IsDemo)return BadRequest(new BusinessProblem{Title="Control de calidad no está disponible en demostración."});
  try { var screen=await repository.Read(batch,sequence,user,ct);return screen is null?NotFound(new BusinessProblem{Title="No se encontró el detalle de OT."}):Ok(screen with{Detail=screen.Detail with{CanEdit=(identity.IsGlobalUser(User)||screen.Detail.CanEdit)&&identity.CanRegister(User)}}); }
  catch(QualityControlPendingException ex){return Conflict(new BusinessProblem{Title=ex.Message});}
 }
 public async Task<ServiceResult> Save(string batch,int sequence,QualityControlUpdate request,CancellationToken ct){
  if(!identity.CanRegister(User)||!Actor.WriteRequested)throw new UnauthorizedAccessException();
  Validate(request);
  try {await repository.Save(batch,sequence,request,identity.GetUser(User),ct);logger.LogInformation("Datos Control Calidad actualizados: OT {Batch}, secuencia {Sequence}, usuario {User}, trace {Trace}",batch,sequence,identity.GetUser(User),Actor.TraceIdentifier);return Ok(new{saved=true});}
  catch(QualityControlPendingException ex){return Conflict(new BusinessProblem{Title=ex.Message});}
 }
 public async Task<ServiceResult> Fabrics(string search,CancellationToken ct){identity.GetUser(User);if(identity.IsDemo)return Ok(Array.Empty<DyeingCatalogOption>());if(search.Trim().Length<2)return Ok(Array.Empty<DyeingCatalogOption>());try{return Ok(await repository.Fabrics(search.Trim(),ct));}catch(QualityControlPendingException ex){return Conflict(new BusinessProblem{Title=ex.Message});}}
 public async Task<ServiceResult> Consumption(string batch,int sequence,CancellationToken ct){var user=identity.GetUser(User);if(identity.IsDemo)return BadRequest(new BusinessProblem{Title="Control de calidad no está disponible en demostración."});try{return Ok(await repository.Consumption(batch,sequence,user,ct));}catch(QualityControlPendingException ex){return Conflict(new BusinessProblem{Title=ex.Message});}}
 public static void Validate(QualityControlUpdate request){
  if(request.Values is null)throw new ArgumentException("Faltan datos de control de calidad.");
  var v=request.Values;var errors=new List<ValidationResult>();
  if(!Validator.TryValidateObject(v,new ValidationContext(v),errors,true))throw new ArgumentException(string.Join(" ",errors.Select(e=>e.ErrorMessage)));
  if(request.Recommendations is null||request.Recommendations.Length>200||request.Recommendations.Any(code=>code is null||code.Length!=3)||request.Recommendations.Distinct().Count()!=request.Recommendations.Length)throw new ArgumentException("Recomendaciones no válidas.");
  if(v.Approval=="D"&&v.RejectedWeight<=0)throw new ArgumentException("Ingresa los kilos rechazados para una desaprobación.");
  if(v.Approval=="A"&&v.RejectedWeight!=0)throw new ArgumentException("Los kilos rechazados deben ser cero para un aprobado.");
  static void Check(decimal value,int scale,string label){if(decimal.Round(value,scale)!=value)throw new ArgumentException(label+": revisa la cantidad de decimales.");}
        Check(v.Weight,0,"Gramaje (g/m²)");
        Check(v.Width,3,"Ancho (m)");
        Check(v.ShrinkWidth,2,"Encogimiento ancho");
        Check(v.ShrinkLength,2,"Encogimiento largo");
        Check(v.Twist,2,"Revirado");
        Check(v.ThirdShrinkWidth,2,"Encog. ancho 3ra. lavada");
        Check(v.ThirdShrinkLength,2,"Encog. largo 3ra. lavada");
        Check(v.ThirdTwist,2,"Revirado 3ra. lavada");
        Check(v.WashedWeight,0,"Gramaje lavado");
        Check(v.LaundryWeight,0,"Gramaje lavandería");
        Check(v.ElongationLength,2,"Elongación largo");
        Check(v.ElongationWidth,2,"Elongación ancho");
        Check(v.RejectedWeight,2,"Kilos rechazados");
        Check(v.PlantWidth,2,"Ancho planta");
        Check(v.Resistance,2,"Resistencia tela");
        Check(v.ResidualShrinkWidth,2,"Encogimiento ancho");
        Check(v.ResidualShrinkLength,2,"Encogimiento largo");
        Check(v.ResidualTwist,2,"Revirado");
        Check(v.RamaWeight,0,"Gramaje (g/m²)");
        Check(v.RamaWidth,2,"Ancho (m)");
        Check(v.RamaShrinkWidth,2,"Encogimiento ancho");
        Check(v.RamaShrinkLength,2,"Encogimiento largo");
        Check(v.RamaTwist,2,"Revirado");
        Check(v.RamaWashedWeight,0,"Gramaje lavado");
 }
}
