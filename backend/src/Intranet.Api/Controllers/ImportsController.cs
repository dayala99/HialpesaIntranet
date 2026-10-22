using System.ComponentModel.DataAnnotations;
using System.Data;
using Dapper;
using Intranet.Api.Infrastructure;
using Intranet.Api.Services;
using Microsoft.AspNetCore.Mvc;
namespace Intranet.Api.Controllers;

public sealed class ImportRequest
{
    [Required,RegularExpression("^[A-Za-z0-9]{3}$")] public string ItemType {get;set;}="";
    [Required,StringLength(3)] public string Origin {get;set;}="";
    [StringLength(3)] public string Shipment {get;set;}="";
    [StringLength(3)] public string Country {get;set;}="";
    [Required,StringLength(2)] public string Area {get;set;}="05";
    public DateTime RegisteredAt {get;set;}=DateTime.Today;
    public DateTime? GuideDate {get;set;}
    public DateTime? PaymentDate {get;set;}
    public DateTime? DueDate {get;set;}
    [StringLength(25)] public string Guide {get;set;}="";
    [StringLength(200)] public string Observations {get;set;}="";
}
public sealed record ImportSource(string SupplierCode,string Supplier,string AnnexType,string Annex,string Currency,string ExistingImport,string SupplierOrigin);
[ApiController,Route("api/v1/logistica/importaciones")]
public sealed class ImportsController(GestionConnectionFactory factory,AviosRepository orders,GestionIdentity identity,ILogger<ImportsController> logger,Microsoft.Extensions.Options.IOptions<GestionOptions> options):ControllerBase
{
    private async Task Access(string series,string number,CancellationToken ct)
    {
        if(identity.IsDemo || await orders.FindOrder(series,number,identity.GetUser(User),ct,identity.GetReadAllWindowsUser(User)) is not {Class:"01"})throw new UnauthorizedAccessException();
    }
    private const string SourceSql="""
      SELECT RTRIM(o.Cod_Proveedor) SupplierCode,RTRIM(p.Des_Proveedor) Supplier,'P' AnnexType,
      ISNULL((SELECT CASE WHEN COUNT(*)=1 THEN RTRIM(MAX(a.Cod_Anxo)) ELSE '' END FROM CN_ANEXOSCONTABLES a WHERE a.Cod_TipAnex='P' AND RTRIM(a.Num_Ruc)=RTRIM(p.Num_Ruc) AND RTRIM(p.Num_Ruc)<>''),'') Annex,RTRIM(o.Cod_Moneda) Currency,RTRIM(ISNULL(o.Num_Importacion,'')) ExistingImport,RTRIM(p.Origen) SupplierOrigin
      FROM LG_ORDCOMP o {LOCK} JOIN LG_PROVEEDOR p ON p.Cod_Proveedor=o.Cod_Proveedor
      WHERE o.Ser_OrdComp=@Series AND o.Cod_OrdComp=@Number
      """;
    [HttpGet]
    public async Task<IActionResult> List([FromQuery,Range(1,100000)]int page=1,[FromQuery,MaxLength(70)]string? search=null,CancellationToken ct=default)
    {
        if(identity.IsDemo)return Ok(new{rows=Array.Empty<object>(),hasMore=false,page,totalRows=0,totalPages=1});
        var user=identity.GetUser(User);var windows=identity.GetReadAllWindowsUser(User);
        await using var c=await factory.OpenAsync(ct);
        var admin=windows is not null; // Read-all permission comes from the authenticated application identity.
        var term=search?.Trim()??"";
        var permission=admin ? "1=1" : "EXISTS(SELECT 1 FROM LG_ORDCOMP o JOIN LG_SEGORDCOMP s ON s.Cod_ClaOrdComp=o.Cod_ClaOrdComp WHERE o.Num_Importacion=i.Num_Importacion AND s.Cod_Usuario=@User)";
        var numeric=term.Length>0 && term.All(char.IsAsciiDigit);
        var reference=System.Text.RegularExpressions.Regex.IsMatch(term,@"^\d{3}-\d{6}$");
        var filter=term.Length==0 ? "1=1" : numeric && term.Length>=8 ? "i.Num_Importacion=@Search" : numeric ? "(i.Num_Importacion LIKE @Prefix OR EXISTS(SELECT 1 FROM LG_ORDCOMP o WHERE o.Num_Importacion=i.Num_Importacion AND o.Cod_OrdComp LIKE @Prefix))" : reference ? "EXISTS(SELECT 1 FROM LG_ORDCOMP o WHERE o.Num_Importacion=i.Num_Importacion AND o.Ser_OrdComp=@Series AND o.Cod_OrdComp=@OrderNumber)" : "a.Des_Anexo LIKE @Pattern";
        var sql="""
          ;WITH list AS (
            SELECT i.Num_Importacion Number,i.Fec_RegDoc RegisteredAt,i.Flg_Status Status,a.Des_Anexo Supplier,
              i.Cod_Moneda Currency,g.Des_OrigenImportacion Origin,i.Ser_OrdComp Series,i.Cod_OrdComp OrderNumber,
              ROW_NUMBER() OVER(ORDER BY i.Fec_Creacion DESC,i.Num_Importacion DESC) rn
            FROM LG_IMPORTACIONES i LEFT JOIN CN_ANEXOSCONTABLES a ON a.Cod_TipAnex=i.Cod_TipAnex AND a.Cod_Anxo=i.Cod_Anxo
            LEFT JOIN LG_ORIGEN_IMPORTACION g ON g.Cod_OrigenImportacion=i.Cod_OrigenImportacion
            WHERE {PERMISSION} AND {FILTER}
          ) SELECT Number,RegisteredAt,Status,Supplier,Currency,Origin,Series,OrderNumber FROM list WHERE rn>@Offset AND rn<=@End ORDER BY rn OPTION (RECOMPILE);
          SELECT COUNT(*) FROM LG_IMPORTACIONES i LEFT JOIN CN_ANEXOSCONTABLES a ON a.Cod_TipAnex=i.Cod_TipAnex AND a.Cod_Anxo=i.Cod_Anxo LEFT JOIN LG_ORIGEN_IMPORTACION g ON g.Cod_OrigenImportacion=i.Cod_OrigenImportacion WHERE {PERMISSION} AND {FILTER} OPTION (RECOMPILE);
          """;
        using var result=await c.QueryMultipleAsync(new CommandDefinition(sql.Replace("{PERMISSION}",permission).Replace("{FILTER}",filter),new{User=user,Search=term,Prefix=term+"%",Pattern="%"+term+"%",Series=reference?term[..3]:"",OrderNumber=reference?term[4..]:"",Offset=(page-1)*50,End=page*50+1},commandTimeout:60,cancellationToken:ct));
        var rows=(await result.ReadAsync()).ToList();var totalRows=await result.ReadSingleAsync<int>();var totalPages=Math.Max(1,(int)Math.Ceiling(totalRows/50d));
        return Ok(new{rows=rows.Take(50).Select(x=>new{number=((string)x.Number).Trim(),registeredAt=(DateTime?)x.RegisteredAt,status=((string?)x.Status)?.Trim(),supplier=((string?)x.Supplier)?.Trim(),currency=((string?)x.Currency)?.Trim(),origin=((string?)x.Origin)?.Trim(),series=((string?)x.Series)?.Trim(),orderNumber=((string?)x.OrderNumber)?.Trim()}),hasMore=page<totalPages,page,totalRows,totalPages});
    }
    [HttpGet("ordenes/{series}/{number}")]
    public async Task<IActionResult> Source([RegularExpression("^[0-9]{3}$")]string series,[RegularExpression("^[0-9]{6}$")]string number,CancellationToken ct)
    {
        await Access(series,number,ct);await using var c=await factory.OpenAsync(ct);
        var source=await c.QuerySingleAsync<ImportSource>(new CommandDefinition(SourceSql.Replace("{LOCK}",""),new{Series=series,Number=number},cancellationToken:ct));
        var mode=await c.ExecuteScalarAsync<string>(new CommandDefinition("SELECT Flg_NumImportacion_Manual FROM TG_CONTROL",cancellationToken:ct));
        var valid=await c.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(*) FROM CN_ANEXOSCONTABLES a JOIN LG_PROVEEDOR p ON RTRIM(p.Num_Ruc)=RTRIM(a.Num_Ruc) WHERE a.Cod_TipAnex=@AnnexType AND a.Cod_Anxo=@Annex AND p.Cod_Proveedor=@SupplierCode",source,cancellationToken:ct));
        return Ok(new{source,automaticNumbering=mode?.Trim()=="N",validAnnex=valid>0});
    }
    [HttpGet("catalogos")]
    public async Task<IActionResult> Catalogs(CancellationToken ct)
    {
        if(identity.IsDemo) return BadRequest(); identity.GetUser(User);await using var c=await factory.OpenAsync(ct);
        var rows=await c.QueryAsync(new CommandDefinition("""
          SELECT 'origins' Kind,RTRIM(Cod_OrigenImportacion) Code,RTRIM(Des_OrigenImportacion) Name FROM LG_ORIGEN_IMPORTACION
          UNION ALL SELECT 'shipments',RTRIM(Cod_Embarque),RTRIM(Des_Embarque) FROM TG_TIPEMB
          UNION ALL SELECT 'countries',RTRIM(Cod_Pais),RTRIM(Descripcion) FROM CN_PAISES
          UNION ALL SELECT 'itemTypes',RTRIM(Cod_TipIte),RTRIM(Des_TipIte) FROM CN_TIPOITEM WHERE Flg_Importacion='S'
          UNION ALL SELECT 'areas',RTRIM(Cod_AreaResponsable),RTRIM(Des_AreaResponsable) FROM CN_AreasResponsables
          """,cancellationToken:ct));return Ok(rows);
    }
    [HttpPost("ordenes/{series}/{number}"),Consumes("application/json")]
    public async Task<IActionResult> Create([RegularExpression("^[0-9]{3}$")]string series,[RegularExpression("^[0-9]{6}$")]string number,ImportRequest r,CancellationToken ct)
    {
        if(!identity.CanRegister(User)||Request.Headers["X-Intranet-Write"]!="1")throw new UnauthorizedAccessException();
        await Access(series,number,ct);
        if(r.RegisteredAt.Year<2000||r.RegisteredAt.Year>2100)throw new ArgumentException("Revisa la fecha de registro.");
        await using var c=await factory.OpenAsync(ct);
        // Global application write access is explicitly configured for authenticated Gestion users.
        if(!options.Value.AllowAllAuthenticatedWrites && await c.ExecuteScalarAsync<int>(new CommandDefinition("SELECT CASE WHEN ORIGINAL_LOGIN()=@Windows AND IS_SRVROLEMEMBER('sysadmin')=1 THEN 1 ELSE 0 END",new{Windows=User.Identity!.Name},cancellationToken:ct))!=1)throw new UnauthorizedAccessException();
        await using var t=await c.BeginTransactionAsync(ct);
        try
        {
            var source=await c.QuerySingleAsync<ImportSource>(new CommandDefinition(SourceSql.Replace("{LOCK}","WITH (UPDLOCK,HOLDLOCK)"),new{Series=series,Number=number},t,cancellationToken:ct));
            if(!string.IsNullOrWhiteSpace(source.ExistingImport)&&source.ExistingImport!="0")throw new RegistrationConflictException("La OC ya tiene una importación: "+source.ExistingImport);
            if(source.SupplierOrigin!="E")throw new ArgumentException("La OC debe corresponder a un proveedor extranjero.");
            if(await c.ExecuteScalarAsync<string>(new CommandDefinition("SELECT Flg_NumImportacion_Manual FROM TG_CONTROL WITH (UPDLOCK,HOLDLOCK)",transaction:t,cancellationToken:ct))!="N")throw new RegistrationConflictException("Gestión no está configurado para numeración automática.");
            var valid=await c.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(*) FROM CN_ANEXOSCONTABLES a JOIN LG_PROVEEDOR p ON RTRIM(p.Num_Ruc)=RTRIM(a.Num_Ruc) WHERE a.Cod_TipAnex=@AnnexType AND a.Cod_Anxo=@Annex AND p.Cod_Proveedor=@SupplierCode",source,t,cancellationToken:ct));
            if(valid!=1)throw new ArgumentException("No se pudo validar el anexo contable del proveedor por RUC.");
            if(await c.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(*) FROM CN_TIPOITEM WHERE Cod_TipIte=@ItemType AND Flg_Importacion='S'",r,t,cancellationToken:ct))!=1)throw new ArgumentException("Selecciona un tipo de ítem válido para importaciones.");
            await c.ExecuteAsync(new CommandDefinition("SELECT Ult_Importacion FROM TG_ANOCONTROL WITH (UPDLOCK,HOLDLOCK) WHERE Ano=@Year",new{Year=r.RegisteredAt.Year.ToString()},t,cancellationToken:ct));
            var p=ImportParameters.Create(r,source,identity.GetUser(User));
            var result=await c.QuerySingleAsync(new CommandDefinition("dbo.UP_MAN_LG_IMPORTACIONES",p,t,commandType:CommandType.StoredProcedure,commandTimeout:60,cancellationToken:ct));
            string importNumber=((string)result.NUM_IMPORTACION).Trim();
            if(string.IsNullOrWhiteSpace(importNumber))throw new RegistrationConflictException("Gestión no devolvió un número de importación.");
            await c.ExecuteAsync(new CommandDefinition("dbo.UP_MAN_ORDCOMP_IMPORTACION",new{NUM_IMPORTACION=importNumber,COD_TIPOALMACEN="C",SER_ORDCOMP=series,COD_ORDCOMP=number},t,commandType:CommandType.StoredProcedure,commandTimeout:60,cancellationToken:ct));
            var linked=await c.ExecuteScalarAsync<string>(new CommandDefinition("SELECT Num_Importacion FROM LG_ORDCOMP WHERE Ser_OrdComp=@Series AND Cod_OrdComp=@Number",new{Series=series,Number=number},t,cancellationToken:ct));
            if(linked?.Trim()!=importNumber)throw new RegistrationConflictException("No se completó la asociación de la OC.");
            await t.CommitAsync(ct);
            logger.LogInformation("Importación {Import} creada desde OC {Series}-{Number}, usuario {User}, trace {Trace}",importNumber,series,number,identity.GetUser(User),HttpContext.TraceIdentifier);
            return Ok(new{number=importNumber,series,orderNumber=number});
        }
        catch {if(t.Connection is not null)await t.RollbackAsync(CancellationToken.None);throw;}
    }
}
public static class ImportParameters
{
    public static DynamicParameters Create(ImportRequest r,ImportSource s,string user)
    {
        var p=new DynamicParameters();
        var values=new {Accion="I",Num_Importacion="",Ser_OrdComp="",Cod_OrdComp="",Num_Guia=r.Guide.Trim(),Fecha_Guia=r.GuideDate?.Date,
          Cod_Embarque=r.Shipment.Trim(),Cod_OrigenImportacion=r.Origin.Trim(),Observacion=r.Observations.Trim(),Cod_TipAnex=s.AnnexType,Cod_Anxo=s.Annex,
          Cod_TipPro="I",Fec_RegDoc=r.RegisteredAt.Date,ConfirmacionPedido="",Proforma="",Cod_TipIte=r.ItemType.Trim(),Cod_Moneda=s.Currency,cod_usuario=user,
          Flg_Liquidacion_Parcial="N",Num_Importacion_Inicial="",ID_PAIS_DUA=r.Country.Trim(),Fec_Pago=r.PaymentDate?.Date,fec_vendoc=r.DueDate?.Date,Cod_AreaResponsable=r.Area.Trim()};
        foreach(var prop in values.GetType().GetProperties())p.Add(prop.Name,prop.GetValue(values));return p;
    }
}




