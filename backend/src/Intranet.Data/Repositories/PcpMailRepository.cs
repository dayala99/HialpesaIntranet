using System.Data;
using Dapper;
using Intranet.Entity;
namespace Intranet.Data;
public sealed class PcpMailRepository(GestionConnectionFactory factory):IPcpMailRepository {
 public async Task<PcpMailDispatch> Dispatch(CancellationToken ct){await using var c=await factory.OpenAsync(ct);return await c.QuerySingleAsync<PcpMailDispatch>(new CommandDefinition("dbo.PA_LG_PCP_CORREO_U0001",commandType:CommandType.StoredProcedure,commandTimeout:30,cancellationToken:ct));}
}
