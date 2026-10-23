using System.Data;
using Dapper;
using Intranet.Entity;
namespace Intranet.Data;
public sealed class GestionAccessRepository(GestionConnectionFactory factory):IGestionAccessRepository {
 public async Task<GestionAccess> Read(string user,string company,CancellationToken ct){
 await using var c=await factory.OpenAsync(ct);
 var raw=await c.QueryAsync(new CommandDefinition("SEGURIDAD.dbo.Sg_Muestra_PerfilesUsuarios",new{COD_USUARIO=user,SW_GUI=false},commandType:CommandType.StoredProcedure,cancellationToken:ct));
 var profiles=raw.Select(x=>(IDictionary<string,object>)x).Where(x=>Convert.ToString(x["CODIGO"])?.Trim()==company).Select(x=>new GestionProfile(Convert.ToString(x["COD. PERFIL"])!.Trim(),Convert.ToString(x["DES. PERFIL"])!.Trim())).Distinct().ToArray();
 var options=new List<GestionOptionAccess>();if(profiles.Length==0)return new(profiles,options);
 var rows=await c.QueryAsync(new CommandDefinition("SEGURIDAD.dbo.SM_MUESTRA_OPCIONES_PERFIL_EMPRESA_multiperfil",new{Cod_Perfil=(string?)null,Cod_Empresa=company,Cod_Usuario=user},commandType:CommandType.StoredProcedure,commandTimeout:90,cancellationToken:ct));
 options.AddRange(rows.Select(x=>(IDictionary<string,object>)x).Select(x=>new GestionOptionAccess(Convert.ToString(x["Cod_Aplicacion"])!.Trim(),Convert.ToString(x["Cod_Opcion"])!.Trim())));
 return new(profiles,options.Distinct().ToArray());
 }
}
