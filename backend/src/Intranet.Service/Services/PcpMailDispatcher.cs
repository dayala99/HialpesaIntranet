using Intranet.Entity;
using Microsoft.Extensions.Logging;
namespace Intranet.Service;
public sealed class PcpMailDispatcher(IPcpMailRepository repository,ILogger<PcpMailDispatcher> logger){
 public async Task<int> DispatchBatch(CancellationToken ct){var processed=0;for(var i=0;i<20;i++){ct.ThrowIfCancellationRequested();var result=await repository.Dispatch(ct);if(!result.Processed)break;processed++;if(!result.Queued)logger.LogError("No se pudo poner en cola la notificación de PCP: {Error}",result.Error);}return processed;}
}
