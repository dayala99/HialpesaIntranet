using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using Microsoft.Data.SqlClient;
using Intranet.Api.Contracts;
namespace Intranet.Api.Infrastructure;
public static class OrderRequirementLinks
{
 public static readonly string[] Fields=["Cod_Fabrica","Cod_OrdPro","Cod_Present","Cod_CompEst","Cod_Item","Cod_Comb","Cod_Color","Cod_Talla","Cod_Destino","Cod_EstCli","Can_Requerida"];
 public static string Key(IDictionary<string,object> row)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(Fields.Select(f=>RegistrationQueries.Text(row,f))))));
 public static async Task<IReadOnlyList<IDictionary<string,object>>> Read(SqlConnection c,SqlTransaction? tx,OrderReference order,string sequence,CancellationToken ct)
 {
  var sql="SELECT r.*,ep.Des_Present Presentation FROM dbo.LG_ORDCOMPITEMREQ r "+(tx is null?"":"WITH(UPDLOCK,HOLDLOCK) ")+"LEFT JOIN dbo.ES_ORDPRO op ON op.Cod_Fabrica=r.Cod_Fabrica AND op.Cod_OrdPro=r.Cod_OrdPro LEFT JOIN dbo.ES_ESTPROPRE ep ON ep.Cod_EstPro=op.Cod_EstPro AND ep.Cod_Present=r.Cod_Present WHERE r.Ser_OrdComp=@Series AND r.Cod_OrdComp=@Number AND r.Sec_OrdComp=@Sequence ORDER BY r.Cod_OrdPro,r.Cod_Present,r.Cod_Talla";
  return (await c.QueryAsync(new CommandDefinition(sql,new{order.Series,order.Number,Sequence=sequence},tx,commandTimeout:60,cancellationToken:ct))).Select(r=>(IDictionary<string,object>)r).ToList();
 }
}
