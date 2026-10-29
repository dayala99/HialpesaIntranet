using Intranet.Entity;
using System.Text.RegularExpressions;
namespace Intranet.Service;
public interface IAnalisisRbImageService { Task<ServiceResult> Read(string np,CancellationToken ct); Task<ServiceResult> ReadHistory(string np,long id,CancellationToken ct); }
public sealed class AnalisisRbImageService(IAnalisisRbImageRepository repository,GestionIdentity identity,RequestActor actor):ApplicationService(actor),IAnalisisRbImageService {
 private string Normalize(string np){identity.GetUser(User);var value=(np??"").Trim().ToUpperInvariant();if(!Regex.IsMatch(value,"^[A-Z0-9]{5}$"))throw new ArgumentException("La NP debe tener 5 caracteres.");return value;}
 private static ServiceResult Result(AnalisisRbImage? image)=>image is null?new(ResultKind.NotFound):new(ResultKind.Success,Content:image.Content,ContentType:image.ContentType);
 public async Task<ServiceResult> Read(string np,CancellationToken ct)=>Result(await repository.Read(Normalize(np),ct));
 public async Task<ServiceResult> ReadHistory(string np,long id,CancellationToken ct){var value=Normalize(np);if(id<=0)throw new ArgumentException("El registro histórico no es válido.");return Result(await repository.ReadHistory(value,id,ct));}
}
