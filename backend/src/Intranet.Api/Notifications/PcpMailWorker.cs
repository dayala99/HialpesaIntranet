using Intranet.Entity;
using Intranet.Service;
using Microsoft.Data.SqlClient;
namespace Intranet.Api.Notifications;
public sealed class PcpMailWorker(IServiceScopeFactory scopes,ILogger<PcpMailWorker> logger,ILoggerFactory logs):BackgroundService {
 protected override async Task ExecuteAsync(CancellationToken stoppingToken){var missingLogged=false;
 while(!stoppingToken.IsCancellationRequested){try{await using var scope=scopes.CreateAsyncScope();var dispatcher=new PcpMailDispatcher(scope.ServiceProvider.GetRequiredService<IPcpMailRepository>(),logs.CreateLogger<PcpMailDispatcher>());await dispatcher.DispatchBatch(stoppingToken);missingLogged=false;}
 catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}
 catch(SqlException e)when(e.Number is 208 or 2812){if(!missingLogged){logger.LogWarning("Notificaciones PCP pendientes de instalación: ejecutar 13_PCP_Notificaciones_Correo.sql en HIALPESA.");missingLogged=true;}}
 catch(Exception e){logger.LogError(e,"No se pudo procesar la cola de correos de PCP. Los avisos permanecen registrados para reintento.");}
 try{await Task.Delay(TimeSpan.FromSeconds(30),stoppingToken);}catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}
 }
 }
}
