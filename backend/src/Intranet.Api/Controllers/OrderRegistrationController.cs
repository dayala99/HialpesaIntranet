using System.ComponentModel.DataAnnotations;
using Intranet.Api.Contracts;
using Intranet.Api.Infrastructure;
using Intranet.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Intranet.Api.Controllers;

[ApiController,Route("api/v1/logistica/avios")]
public sealed class OrderRegistrationController(OrderRegistrationService service,RegistrationReadRepository reads,AviosRepository orders,GestionIdentity identity,ILogger<OrderRegistrationController> logger):ControllerBase
{
    private (string Gestion,string Windows) WriteActor()
    {
        if(!identity.CanRegister(User) || Request.Headers["X-Intranet-Write"]!="1")throw new UnauthorizedAccessException();
        return (identity.GetUser(User),User.Identity!.Name!);
    }
    private async Task<PurchaseOrder?> Accessible(string series,string number,CancellationToken ct)=>identity.IsDemo?null:await orders.FindOrder(series,number,identity.GetUser(User),ct,identity.GetReadAllWindowsUser(User));

    [HttpGet("ordenes")]
    public async Task<IActionResult> List([FromQuery,Range(1,100000)]int page=1,[FromQuery,MaxLength(70)]string? search=null,CancellationToken ct=default)
    {
        if(identity.IsDemo)return Ok(new{rows=Array.Empty<object>(),hasMore=false,page});
        return Ok(await reads.List(identity.GetUser(User),identity.GetReadAllWindowsUser(User),page,search??"",ct));
    }
    [HttpGet("registro/catalogos")]
    public async Task<ActionResult<RegistrationCatalogs>> Catalogs(CancellationToken ct)=>identity.IsDemo?BadRequest(new ProblemDetails{Title="Los catálogos de registro requieren conexión real."}):Ok(await reads.Catalogs(ct));
    [HttpGet("registro/catalogos/{kind}")]
    public async Task<ActionResult<IReadOnlyList<LookupOption>>> Search([RegularExpression("^(proveedores|grupos)$")]string kind,[FromQuery,MaxLength(70)]string? search,CancellationToken ct)
    {
        if(identity.IsDemo)return BadRequest();
        if(kind=="proveedores" && (search?.Trim().Length??0)<2)return BadRequest(new ProblemDetails{Title="Ingresa al menos dos caracteres para buscar proveedores."});
        return Ok(await reads.Search(kind,search??"",ct));
    }

    [HttpPost("ordenes"),Consumes("application/json")]
    public async Task<ActionResult<OrderReference>> Create([FromBody]CreateOrderRequest request,CancellationToken ct)
    {
        var actor=WriteActor();
        var order=await service.Create(request,actor.Gestion,actor.Windows,ct);
        logger.LogInformation("OC creada {Series}-{Number}, usuario {GestionUser}, identidad {WindowsUser}, trace {Trace}",order.Series,order.Number,actor.Gestion,actor.Windows,HttpContext.TraceIdentifier);
        return Created($"/api/v1/logistica/avios/ordenes/{order.Series}/{order.Number}",order);
    }
    [HttpGet("ordenes/{series}/{number}/pendientes")]
    public async Task<ActionResult<IReadOnlyList<PendingRequirement>>> Pending([RegularExpression("^[0-9]{3}$")]string series,[RegularExpression("^[0-9]{6}$")]string number,[FromQuery,Required,RegularExpression("^[A-Za-z0-9]{5}$")]string productionOrder,[FromQuery,Required,RegularExpression("^[A-Za-z0-9]{8}$")]string itemCode,CancellationToken ct)
    {
        if(await Accessible(series,number,ct) is not {Class:"01"})return NotFound();
        return Ok(await reads.Pending(new(series,number),productionOrder,itemCode,ct));
    }
    [HttpPost("ordenes/{series}/{number}/detalles"),Consumes("application/json")]
    public async Task<ActionResult<DetailsSavedResponse>> Add([RegularExpression("^[0-9]{3}$")]string series,[RegularExpression("^[0-9]{6}$")]string number,[FromBody]AddOrderDetailsRequest request,CancellationToken ct)
    {
        var actor=WriteActor();
        if(await Accessible(series,number,ct) is not {Class:"01"})return NotFound();
        var result=await service.Add(new(series,number),request,actor.Gestion,actor.Windows,ct);
        logger.LogInformation("Detalle agregado a {Series}-{Number}, {Count} requerimientos, usuario {User}, trace {Trace}",series,number,result.RequirementsAdded,actor.Gestion,HttpContext.TraceIdentifier);
        return Ok(result);
    }
    [HttpGet("ordenes/{series}/{number}/impresion")]
    public async Task<ActionResult<PrintOrder>> Print([RegularExpression("^[0-9]{3}$")]string series,[RegularExpression("^[0-9]{6}$")]string number,CancellationToken ct)
    {
        var order=await Accessible(series,number,ct);
        if(order is not {Class:"01"})return NotFound();
        return Ok(await reads.Print(order,await orders.Items(series,number,ct),ct));
    }
}
