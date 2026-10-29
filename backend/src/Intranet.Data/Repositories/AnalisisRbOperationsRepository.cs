using System.Data;
using Dapper;
using Intranet.Entity;
namespace Intranet.Data;
public sealed class AnalisisRbOperationsRepository(GestionConnectionFactory factory,IConfiguration configuration):IAnalisisRbOperationsRepository {
 public async Task<AnalisisRbOperations?> Read(string np,string? variant,CancellationToken ct){
  await using var connection=await factory.OpenAsync(ct);
  var context=await connection.QuerySingleOrDefaultAsync<TariffContext>(new CommandDefinition("SELECT RTRIM(op.Cod_EstPro) AS Tariff,RTRIM(op.Cod_Variante_Tarifado) AS AssignedVariant,RTRIM(e.Des_EstPro) AS StyleName FROM dbo.ES_OrdPro op LEFT JOIN dbo.ES_EstPro e ON e.Cod_EstPro=op.Cod_EstPro WHERE op.Cod_Fabrica=@Factory AND op.Cod_OrdPro=@Np",new{Factory=configuration["AnalisisRb:Factory"]??"001",Np=np},commandTimeout:30,cancellationToken:ct));
  if(context is null)return null;
  var variants=(await connection.QueryAsync<AnalisisRbVariant>(new CommandDefinition("SELECT RTRIM(Cod_Variante_Tarifado) AS Code,RTRIM(Des_Tarifado) AS Description,RTRIM(Flg_Status_Tarifado) AS Status FROM dbo.CF_TARIFADO_VARIANTES WHERE Cod_Tarifado=@Tariff ORDER BY Cod_Variante_Tarifado",new{context.Tariff},commandTimeout:30,cancellationToken:ct))).ToList();
  var selected=variant??context.AssignedVariant;
  if(!variants.Any(v=>v.Code==selected))throw new ArgumentException("La variante no pertenece al tarifado de la NP o no tiene una variante asignada válida.");
  var raw=await connection.QueryAsync<OperationRow>(new CommandDefinition("dbo.UP_REP_CFTARIFADO_OPERACIONES",new{COD_TARIFADO=context.Tariff,COD_VARIANTE_TARIFADO=selected},commandType:CommandType.StoredProcedure,commandTimeout:30,cancellationToken:ct));
  var rows=raw.Select(row=>new AnalisisRbOperation(T(row.COD_SECCONF),T(row.COD_BLQPRE),T(row.DES_BLQPRE),T(row.SECUENCIA),T(row.COD_OPERACION_COSTURA),T(row.COD_VARIANTE_OPERACION),T(row.DES_OPERACION_COSTURA),T(row.COD_FAMMAQ_COSTURA),T(row.COD_GRUFAMMAQ_COSTURA),T(row.DES_GRUFAM_COSTURA),T(row.CATEGORIA),T(row.COD_TIPOFAMOPE),row.TIE_ESTANDAR_OPE){SectorName=T(row.DES_SECCONF)}).ToList();
  return new(np,context.Tariff,context.StyleName,context.AssignedVariant,selected,variants,rows);
 }
 private static string T(string? value)=>value?.Trim()??"";
 private sealed class TariffContext {public string Tariff{get;set;}="";public string AssignedVariant{get;set;}="";public string StyleName{get;set;}="";}
 private sealed class OperationRow {public string? COD_SECCONF{get;set;}public string? DES_SECCONF{get;set;}public string? COD_BLQPRE{get;set;}public string? DES_BLQPRE{get;set;}public string? SECUENCIA{get;set;}public string? COD_OPERACION_COSTURA{get;set;}public string? COD_VARIANTE_OPERACION{get;set;}public string? DES_OPERACION_COSTURA{get;set;}public string? COD_FAMMAQ_COSTURA{get;set;}public string? COD_GRUFAMMAQ_COSTURA{get;set;}public string? DES_GRUFAM_COSTURA{get;set;}public string? CATEGORIA{get;set;}public string? COD_TIPOFAMOPE{get;set;}public decimal TIE_ESTANDAR_OPE{get;set;}}
}
