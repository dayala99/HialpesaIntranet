using System.Data;using Dapper;using Intranet.Entity;using Microsoft.Data.SqlClient;
namespace Intranet.Data;
public sealed partial class ImportControlRepository {
 public Task<ImportControlProvisionResult> Provision(string number,ImportControlProvision request,string user,CancellationToken ct)=>Pending(async()=>{
 ImportControlProvisioning.ValidateRequest(request);await using var c=await factory.OpenAsync(ct);await using var tx=(SqlTransaction)await c.BeginTransactionAsync(IsolationLevel.Serializable,ct);
 try{
 var current=await Read(c,number,ct,tx)??throw new RegistrationConflictException("La importación ya no existe.");ImportControlProvisioning.ValidateSnapshot(current.Detail.Status,current.Detail.Version,request);
 // The portal uses Gestión company 01; FrmControlImportaciones initializes branch 01.
 const string company="01",branch="01";var station=Environment.MachineName;station=station[..Math.Min(station.Length,20)];
 var p=new DynamicParameters(new{NUM_IMPORTACION=number,COD_EMPRESA=company,COD_SUCURSAL=branch,Cod_Usuario=user,Cod_Estacion=station});
 if(request.Action=="reverse"){
 var journal=Text(current.Raw,"cod_tipodiario_provision");var year=Text(current.Raw,"ano_provision");var period=Text(current.Raw,"periodo_provision");if(!int.TryParse(Text(current.Raw,"registro_provision"),out var register)||register<=0||string.IsNullOrWhiteSpace(journal)||string.IsNullOrWhiteSpace(year)||string.IsNullOrWhiteSpace(period))throw new ArgumentException("No se encontró la referencia del asiento de provisión. Revisa el registro en Gestión.");
 p.Add("COD_TIPODIARIO",journal);p.Add("ANO",year);p.Add("PERIODO",period);p.Add("REGISTRO",register);
 await c.ExecuteAsync(Query("REVIERTE_ASIENTO_CONTABLE_PROVISION_IMPORTACION_VALID",p,ct,tx));
 }
 p.Add("ReturnValue",dbType:DbType.Int32,direction:ParameterDirection.ReturnValue);
 await c.ExecuteAsync(Query(request.Action=="provision"?"GENERA_ASIENTO_CONTABLE_PROVISION_IMPORTACION":"REVIERTE_ASIENTO_CONTABLE_PROVISION_IMPORTACION",p,ct,tx));
 if(p.Get<int>("ReturnValue")!=0)throw new RegistrationConflictException("Gestión no confirmó la operación contable. Vuelve a consultar la importación.");
 var expected=request.Action=="provision"?"P":"R";var updated=await Read(c,number,ct,tx);if(updated?.Detail.Status!=expected)throw new RegistrationConflictException("Gestión no confirmó el cambio de estado. No se guardó la operación.");
 await tx.CommitAsync(ct);return new ImportControlProvisionResult(number,request.Action,expected);
 }catch{if(tx.Connection is not null)await tx.RollbackAsync(CancellationToken.None);throw;}
 });
}
