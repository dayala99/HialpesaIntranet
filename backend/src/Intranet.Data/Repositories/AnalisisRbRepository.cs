using System.Data;
using Dapper;
using Intranet.Entity;
using Microsoft.Data.SqlClient;
namespace Intranet.Data;
public sealed class AnalisisRbRepository(GestionConnectionFactory factory, IConfiguration configuration):IAnalisisRbRepository
{
 public async Task<IReadOnlyList<AnalisisRbRow>> Search(string np,CancellationToken ct)
 {
  await using var connection=await factory.OpenAsync(ct);
  var plant=configuration["AnalisisRb:Factory"]??"001";
  try { var rows=(await connection.QueryAsync<AnalisisRbRow>(new CommandDefinition("dbo.PA_ES_ORDPRO_S0003",new{Np=np,Factory=plant},commandType:CommandType.StoredProcedure,commandTimeout:30,cancellationToken:ct))).ToList();
   if(rows.Count==0)return rows;
   var header=await connection.QuerySingleOrDefaultAsync<HeaderExtra>(new CommandDefinition(@"
SET ARITHABORT ON;
SELECT NULLIF(RTRIM(e.icono1),'') AS ImagePath,
 RTRIM(garment.Des_TipPre) AS GarmentType,RTRIM(op.Cod_EstCli) AS ClientStyle,op.Num_PrenReq AS RequiredQuantity,RTRIM(division.Nom_DivCli) AS DivisionName,
 STUFF((SELECT DISTINCT N' / '+RTRIM(p.Des_Present)
        FROM dbo.ES_OrdProColTal colors
        INNER JOIN dbo.ES_EstProPre p ON p.Cod_EstPro=op.Cod_EstPro AND p.Cod_Present=colors.Cod_Present
        WHERE colors.Cod_Fabrica=op.Cod_Fabrica AND colors.Cod_OrdPro=op.Cod_OrdPro
          AND NULLIF(RTRIM(p.Des_Present),'') IS NOT NULL
        ORDER BY N' / '+RTRIM(p.Des_Present)
        FOR XML PATH(''),TYPE).value('.','nvarchar(max)'),1,3,N'') AS ColorName,
 (SELECT MIN(work.Fec_Salida_Inicio) FROM dbo.PL_TRABAJOS work WHERE work.Cod_OrdPro=op.Cod_OrdPro) AS SewingStartDate,
 (SELECT MAX(work.Fec_Salida_Fin) FROM dbo.PL_TRABAJOS work WHERE work.Cod_OrdPro=op.Cod_OrdPro) AS SewingEndDate
FROM dbo.ES_OrdPro op
LEFT JOIN dbo.ES_EstPro e ON e.Cod_EstPro=op.Cod_EstPro
LEFT JOIN dbo.TG_TipPre garment ON garment.Cod_TipPre=e.Cod_TipPre
LEFT JOIN dbo.TG_PurOrd po ON po.Cod_Cliente=op.Cod_Cliente AND po.Cod_PurOrd=op.Cod_PurOrd AND po.Cod_ClaPurOrd=op.Cod_ClaPurOrd
LEFT JOIN dbo.TG_DivCli division ON division.Cod_Cliente=po.Cod_Cliente AND division.Cod_DivCli=po.Cod_DivCli
WHERE op.Cod_Fabrica=@Factory AND op.Cod_OrdPro=@Np",new{Factory=plant,Np=np},commandTimeout:30,cancellationToken:ct));
   return rows.Select(row=>row with{GarmentType=header?.GarmentType,ClientStyle=header?.ClientStyle,DivisionName=header?.DivisionName,RequiredQuantity=header?.RequiredQuantity,ImagePath=header?.ImagePath?.Trim(),ColorName=header?.ColorName,SewingStartDate=header?.SewingStartDate,SewingEndDate=header?.SewingEndDate}).ToList(); }
  catch(SqlException e) when(e.Number==2812){throw new ArgumentException("Falta registrar PA_ES_ORDPRO_S0003 en la base de datos. Contacta a soporte.",e);}
 }
 public async Task<AnalisisRbFilterPage> Filter(decimal? price,decimal? time,string? type,string? cursor,CancellationToken ct){
  await using var connection=await factory.OpenAsync(ct);
  try{var rows=(await connection.QueryAsync<AnalisisRbRow>(new CommandDefinition("dbo.PA_ES_ORDPRO_S0004",new{Imp_Precio_Comercial=price,Min_Tiempo_Total=time,Cod_Tipo=type,Cod_Np_Anterior=cursor,Cod_Fabrica=configuration["AnalisisRb:Factory"]??"001"},commandType:CommandType.StoredProcedure,commandTimeout:60,cancellationToken:ct))).ToList();var more=rows.Count>50;var page=rows.Take(50).ToList();return new(page,more,more?page.Last().Np:null);}
  catch(SqlException e) when(e.Number==2812){throw new ArgumentException("Falta registrar PA_ES_ORDPRO_S0004_Otros.sql en Gestión.",e);}
 }
 public async Task<IReadOnlyList<AnalisisRbType>> Types(CancellationToken ct){await using var connection=await factory.OpenAsync(ct);try{return (await connection.QueryAsync<AnalisisRbType>(new CommandDefinition("dbo.PA_ES_ORDPRO_S0005",commandType:CommandType.StoredProcedure,cancellationToken:ct))).ToList();}catch(SqlException e) when(e.Number==2812){throw new ArgumentException("Falta registrar PA_ES_ORDPRO_S0004_Otros.sql en Gestión.",e);}}
 private sealed class HeaderExtra {public string? GarmentType{get;set;}public string? ClientStyle{get;set;}public string? DivisionName{get;set;}public int? RequiredQuantity{get;set;}public string? ImagePath{get;set;}public string? ColorName{get;set;}public DateTime? SewingStartDate{get;set;}public DateTime? SewingEndDate{get;set;}}
}
