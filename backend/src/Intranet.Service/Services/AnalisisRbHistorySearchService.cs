using Intranet.Entity;
using System.Text.RegularExpressions;
namespace Intranet.Service;
public interface IAnalisisRbHistorySearchService {Task<ServiceResult> Search(string? np,decimal? price,decimal? time,string? type,long? cursor,CancellationToken ct);Task<ServiceResult> Types(CancellationToken ct);}
public sealed class AnalisisRbHistorySearchService(IAnalisisRbHistorySearchRepository repository,GestionIdentity identity,RequestActor actor):ApplicationService(actor),IAnalisisRbHistorySearchService {
 public async Task<ServiceResult> Search(string? np,decimal? price,decimal? time,string? type,long? cursor,CancellationToken ct){identity.GetUser(User);np=string.IsNullOrWhiteSpace(np)?null:np.Trim().ToUpperInvariant();type=string.IsNullOrWhiteSpace(type)?null:type.Trim();if(np!=null&&!Regex.IsMatch(np,"^[A-Z0-9]{5}$")||cursor<=0||type?.Length>4000)throw new ArgumentException("Revisa la NP y los filtros del historial.");if(price<0||time<0||price>100000000m||time>100000000m||price.HasValue&&decimal.Round(price.Value,4)!=price.Value||time.HasValue&&decimal.Round(time.Value,2)!=time.Value)throw new ArgumentException("El precio admite hasta 4 decimales y el tiempo hasta 2, con valores mayores o iguales a cero.");return Ok(await repository.Search(np,price,time,type,cursor,ct));}
 public async Task<ServiceResult> Types(CancellationToken ct){identity.GetUser(User);return Ok(await repository.Types(ct));}
}
