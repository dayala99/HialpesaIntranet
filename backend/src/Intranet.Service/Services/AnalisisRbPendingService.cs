using Intranet.Entity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace Intranet.Service;
public sealed class AnalisisRbPendingCache(IServiceScopeFactory scopes,IHostApplicationLifetime lifetime,ILogger<AnalisisRbPendingCache> logger) {
 private readonly object gate=new();private bool loading;private AnalisisRbPendingResult? result;private string? error;private DateTime attempted;
 public AnalisisRbPendingStatus Read(bool refresh){lock(gate){if(!loading&&(refresh||attempted==default||DateTime.UtcNow-attempted>TimeSpan.FromMinutes(5))){loading=true;error=null;attempted=DateTime.UtcNow;_ = Task.Run(Load);}return loading?new("loading",null,null):error!=null?new("failed",null,error):new("ready",result,null);}}
 private async Task Load(){try{await using var scope=scopes.CreateAsyncScope();using var timeout=CancellationTokenSource.CreateLinkedTokenSource(lifetime.ApplicationStopping);timeout.CancelAfter(TimeSpan.FromMinutes(8));var value=await scope.ServiceProvider.GetRequiredService<IAnalisisRbPendingRepository>().Read(timeout.Token);lock(gate){result=value;error=null;attempted=DateTime.UtcNow;loading=false;}}catch(Exception e){logger.LogError(e,"No se pudo obtener el reporte Resumido de Planeamiento para Analíticas.");lock(gate){error=e is ArgumentException?e.Message:"No se pudo completar el reporte de Planeamiento. Intenta actualizar nuevamente.";attempted=DateTime.UtcNow;loading=false;}}}
}
public interface IAnalisisRbPendingService{ServiceResult Read(bool refresh);}
public sealed class AnalisisRbPendingService(AnalisisRbPendingCache cache,GestionIdentity identity,RequestActor actor):ApplicationService(actor),IAnalisisRbPendingService {public ServiceResult Read(bool refresh){identity.GetUser(User);return Ok(cache.Read(refresh));}}
