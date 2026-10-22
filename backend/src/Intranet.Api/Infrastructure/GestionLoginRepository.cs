using Dapper;
using Microsoft.Extensions.Options;
namespace Intranet.Api.Infrastructure;
public sealed record GestionLoginUser(string Code,string Name);
public interface IGestionLoginRepository
{
 Task<GestionLoginUser?> VerifyAsync(string username,string password,CancellationToken ct);
}
public sealed class GestionLoginRepository(GestionConnectionFactory factory,IOptions<GestionOptions> options):IGestionLoginRepository
{
 public async Task<GestionLoginUser?> VerifyAsync(string username,string password,CancellationToken ct)
 {
  await using var connection=await factory.OpenAsync(ct);
  const string sql="""
   SELECT TOP (2) RTRIM(Cod_Usuario) Code,LTRIM(RTRIM(ISNULL(Nom_Usuario,''))) Name
   FROM SEGURIDAD.dbo.SEG_Usuarios
   WHERE Cod_Usuario=@Username
     AND [Password] COLLATE Latin1_General_BIN2=@Password COLLATE Latin1_General_BIN2
     AND DATALENGTH([Password])=DATALENGTH(@Password)
     AND ISNULL(is_active,1)=1
   """;
  var users=(await connection.QueryAsync<GestionLoginUser>(new CommandDefinition(sql,new{
   Username=new DbString{Value=username,IsAnsi=true,Length=15},
   Password=new DbString{Value=password,IsAnsi=true,Length=128}
  },commandTimeout:options.Value.CommandTimeoutSeconds,cancellationToken:ct))).ToArray();
  return users.Length==1?users[0]:null;
 }
}
