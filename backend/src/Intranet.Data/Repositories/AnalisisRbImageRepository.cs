using Dapper;
using Intranet.Entity;
namespace Intranet.Data;
public sealed class AnalisisRbImageRepository(GestionConnectionFactory factory,IConfiguration configuration):IAnalisisRbImageRepository
{
 public static bool IsAllowedPath(string path,IEnumerable<string> roots){
  if(string.IsNullOrWhiteSpace(path)||!Path.IsPathFullyQualified(path)||path.Split(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar).Contains(".."))return false;
  try{var full=Path.GetFullPath(path);return roots.Any(root=>Path.IsPathFullyQualified(root)&&full.StartsWith(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase));}catch(ArgumentException){return false;}catch(NotSupportedException){return false;}
 }
 public async Task<AnalisisRbImage?> Read(string np,CancellationToken ct){
  await using var connection=await factory.OpenAsync(ct);
  var path=(await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition("SELECT NULLIF(RTRIM(e.icono1),'') FROM dbo.ES_OrdPro op INNER JOIN dbo.ES_EstPro e ON e.Cod_EstPro=op.Cod_EstPro WHERE op.Cod_Fabrica=@Factory AND op.Cod_OrdPro=@Np",new{Factory=configuration["AnalisisRb:Factory"]??"001",Np=np},commandTimeout:30,cancellationToken:ct)))?.Trim();
  return await Load(path,ct);
 }
 public async Task<AnalisisRbImage?> ReadHistory(string np,long id,CancellationToken ct){
  await using var connection=await factory.OpenAsync(ct);
  var spanish=await AnalisisRbHistorySchema.UsesSpanish(connection,ct);
  var sql=spanish?"SELECT Des_Ruta_Imagen FROM dbo.LG_ANALISIS_RB_HISTORIAL WHERE Cod_Fabrica=@Factory AND Cod_OrdPro=@Np AND Num_Registro=@Id":"SELECT ImagePath FROM dbo.LG_ANALISIS_RB_HISTORIAL WHERE Factory=@Factory AND Np=@Np AND Id=@Id";
  var path=await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(sql,new{Factory=configuration["AnalisisRb:Factory"]??"001",Np=np,Id=id},commandTimeout:30,cancellationToken:ct));
  return await Load(path?.Trim(),ct);
 }
 private async Task<AnalisisRbImage?> Load(string? path,CancellationToken ct){
  if(string.IsNullOrEmpty(path))return null;
  var roots=configuration.GetSection("AnalisisRb:ImageRoots").GetChildren().Select(section=>section.Value).Where(value=>!string.IsNullOrWhiteSpace(value)).Cast<string>().ToArray();
  if(roots.Length==0)roots=[ @"\\hialpesa103\EstPro$" ];
  if(!IsAllowedPath(path,roots))throw new ArgumentException("La carpeta de imágenes referenciales no está habilitada en el backend.");
  var mime=Path.GetExtension(path).ToLowerInvariant() switch{".jpg" or ".jpeg"=>"image/jpeg",".png"=>"image/png",".gif"=>"image/gif",".webp"=>"image/webp",".bmp"=>"image/bmp",_=>null};
  if(mime is null)return null;
  try{await using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read,81920,FileOptions.Asynchronous|FileOptions.SequentialScan);if(stream.Length>10*1024*1024)throw new ArgumentException("La imagen referencial supera el tamaño permitido de 10 MB.");if(stream.Length==0)return null;var bytes=new byte[(int)stream.Length];await stream.ReadExactlyAsync(bytes,ct);return new(bytes,mime);}
  catch(FileNotFoundException){return null;}catch(DirectoryNotFoundException){return null;}
 }
}


