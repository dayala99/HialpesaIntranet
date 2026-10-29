using Intranet.Entity;
using System.Text.RegularExpressions;
namespace Intranet.Service;
public sealed class AnalisisRbService(IAnalisisRbRepository repository,GestionIdentity identity,RequestActor actor):ApplicationService(actor),IAnalisisRbService
{
 public async Task<ServiceResult> Types(CancellationToken ct){identity.GetUser(User);return Ok(await repository.Types(ct));}
 public async Task<ServiceResult> Filter(decimal? price,decimal? time,string? type,string? cursor,CancellationToken ct){
  identity.GetUser(User);type=string.IsNullOrWhiteSpace(type)?null:type.Trim();cursor=string.IsNullOrWhiteSpace(cursor)?null:cursor.Trim().ToUpperInvariant();
  if(price==null&&time==null&&type==null)throw new ArgumentException("Indica al menos un filtro de búsqueda.");
  if(price<0||time<0||price>99999999999999m||time>9999999999999999m)throw new ArgumentException("Precio y tiempo deben ser valores positivos dentro del rango permitido.");
  if(price.HasValue&&decimal.Round(price.Value,4)!=price.Value||time.HasValue&&decimal.Round(time.Value,2)!=time.Value)throw new ArgumentException("El precio admite hasta 4 decimales y el tiempo hasta 2.");
  if(type?.Length>20||cursor!=null&&!Regex.IsMatch(cursor,"^[A-Z0-9]{5}$"))throw new ArgumentException("Filtro de búsqueda inválido.");
  return Ok(await repository.Filter(price,time,type,cursor,ct));
 }
 public async Task<ServiceResult> Search(string np,CancellationToken ct)
 {
  identity.GetUser(User);
  var value=(np??"").Trim().ToUpperInvariant();
  if(!Regex.IsMatch(value,"^[A-Z0-9]{5}$"))throw new ArgumentException("Ingresa una NP de 5 caracteres, por ejemplo J1907.");
  return Ok(await repository.Search(value,ct));
 }
}
