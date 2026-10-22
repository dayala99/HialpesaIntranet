using System.ComponentModel.DataAnnotations;
using Intranet.Entity;
using Intranet.Service;
using Microsoft.AspNetCore.Mvc;
namespace Intranet.Api.Controllers;

[ApiController, Route("api/v1/logistica/avios")]
public sealed class OrderRegistrationController(IOrderRegistrationApplicationService service) : ControllerBase
{
    [HttpGet("ordenes")]
    public async Task<IActionResult> List([FromQuery, Range(1, 100000)] int page = 1, [FromQuery, MaxLength(70)] string? search = null, CancellationToken ct = default)
    { return (await service.List(page, search, ct)).ToActionResult(); }
    [HttpGet("registro/proveedores/{code}/valores")]
    public async Task<IActionResult> SupplierDefaults([RegularExpression("^[A-Za-z0-9]{1,12}$")] string code, CancellationToken ct)
    { return (await service.SupplierDefaults(code, ct)).ToActionResult(); }
    [HttpGet("registro/fabricas")]
    public async Task<IActionResult> Factories(CancellationToken ct)
    { return (await service.Factories(ct)).ToActionResult(); }
    [HttpGet("registro/grupos-por-op")]
    public async Task<IActionResult> GroupsByOp([FromQuery, Required, StringLength(20)] string factory, [FromQuery, Required, RegularExpression("^[A-Za-z0-9]{5}$")] string productionOrder, CancellationToken ct)
    { return (await service.GroupsByOp(factory, productionOrder, ct)).ToActionResult(); }
    [HttpGet("registro/catalogos")]
    public async Task<ActionResult<RegistrationCatalogs>> Catalogs(CancellationToken ct)
    { return (await service.Catalogs(ct)).ToActionResult(); }
    [HttpGet("registro/catalogos/{kind}")]
    public async Task<ActionResult<IReadOnlyList<LookupOption>>> Search([RegularExpression("^(proveedores|grupos)$")] string kind, [FromQuery, MaxLength(70)] string? search, CancellationToken ct)
    { return (await service.Search(kind, search, ct)).ToActionResult(); }
    [HttpGet("ordenes/{series}/{number}/detalles/{sequence}/requerimientos")]
    public async Task<IActionResult> LinkedRequirements([RegularExpression("^[0-9]{3}$")] string series, [RegularExpression("^[0-9]{6}$")] string number, [RegularExpression("^[0-9]{3}$")] string sequence, CancellationToken ct)
    { return (await service.LinkedRequirements(series, number, sequence, ct)).ToActionResult(); }
    [HttpPost("ordenes/{series}/{number}/detalles/{sequence}/retirar-requerimientos")]
    public async Task<IActionResult> RemoveRequirements([RegularExpression("^[0-9]{3}$")] string series, [RegularExpression("^[0-9]{6}$")] string number, [RegularExpression("^[0-9]{3}$")] string sequence, [FromBody] RemoveRequirementLinksRequest request, CancellationToken ct)
    { return (await service.RemoveRequirements(series, number, sequence, request, ct)).ToActionResult(); }
    [HttpGet("ordenes/{series}/{number}/detalles/{sequence}")]
    public async Task<IActionResult> ItemEdit([RegularExpression("^[0-9]{3}$")] string series, [RegularExpression("^[0-9]{6}$")] string number, [RegularExpression("^[0-9]{3}$")] string sequence, CancellationToken ct)
    { return (await service.ItemEdit(series, number, sequence, ct)).ToActionResult(); }
    [HttpPut("ordenes/{series}/{number}/detalles/{sequence}")]
    public async Task<IActionResult> ItemUpdate([RegularExpression("^[0-9]{3}$")] string series, [RegularExpression("^[0-9]{6}$")] string number, [RegularExpression("^[0-9]{3}$")] string sequence, [FromBody] UpdateOrderItemRequest request, CancellationToken ct)
    { return (await service.ItemUpdate(series, number, sequence, request, ct)).ToActionResult(); }
    [HttpDelete("ordenes/{series}/{number}/detalles/{sequence}")]
    public async Task<IActionResult> ItemDelete([RegularExpression("^[0-9]{3}$")] string series, [RegularExpression("^[0-9]{6}$")] string number, [RegularExpression("^[0-9]{3}$")] string sequence, CancellationToken ct)
    { return (await service.ItemDelete(series, number, sequence, ct)).ToActionResult(); }
    [HttpGet("ordenes/{series}/{number}/edicion")]
    public async Task<IActionResult> EditHeader([RegularExpression("^[0-9]{3}$")] string series, [RegularExpression("^[0-9]{6}$")] string number, CancellationToken ct)
    { return (await service.EditHeader(series, number, ct)).ToActionResult(); }
    [HttpPut("ordenes/{series}/{number}")]
    public async Task<IActionResult> UpdateHeader([RegularExpression("^[0-9]{3}$")] string series, [RegularExpression("^[0-9]{6}$")] string number, [FromBody] UpdateOrderRequest request, CancellationToken ct)
    { return (await service.UpdateHeader(series, number, request, ct)).ToActionResult(); }
    [HttpGet("ordenes/{series}/{number}/grupos")]
    public async Task<IActionResult> OrderGroups([RegularExpression("^[0-9]{3}$")] string series, [RegularExpression("^[0-9]{6}$")] string number, CancellationToken ct)
    { return (await service.OrderGroups(series, number, ct)).ToActionResult(); }
    [HttpPost("ordenes/{series}/{number}/grupos")]
    public async Task<IActionResult> AddGroups([RegularExpression("^[0-9]{3}$")] string series, [RegularExpression("^[0-9]{6}$")] string number, [FromBody] AddManufacturingGroupsRequest request, CancellationToken ct)
    { return (await service.AddGroups(series, number, request, ct)).ToActionResult(); }
    [HttpPost("ordenes"), Consumes("application/json")]
    public async Task<ActionResult<OrderReference>> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
    { return (await service.Create(request, ct)).ToActionResult(); }
    [HttpGet("ordenes/{series}/{number}/selector-detalle")]
    public async Task<IActionResult> DetailSelector([RegularExpression("^[0-9]{3}$")] string series, [RegularExpression("^[0-9]{6}$")] string number, [FromQuery, StringLength(8)] string? group, [FromQuery, RegularExpression("^[A-Za-z0-9]{5}$")] string? productionOrder, CancellationToken ct)
    { return (await service.DetailSelector(series, number, group, productionOrder, ct)).ToActionResult(); }
    [HttpGet("ordenes/{series}/{number}/pendientes")]
    public async Task<ActionResult<IReadOnlyList<PendingRequirement>>> Pending([RegularExpression("^[0-9]{3}$")] string series, [RegularExpression("^[0-9]{6}$")] string number, [FromQuery, Required, RegularExpression("^[A-Za-z0-9]{5}$")] string productionOrder, [FromQuery, Required, RegularExpression("^[A-Za-z0-9]{8}$")] string itemCode, CancellationToken ct)
    { return (await service.Pending(series, number, productionOrder, itemCode, ct)).ToActionResult(); }
    [HttpPost("ordenes/{series}/{number}/detalles"), Consumes("application/json")]
    public async Task<ActionResult<DetailsSavedResponse>> Add([RegularExpression("^[0-9]{3}$")] string series, [RegularExpression("^[0-9]{6}$")] string number, [FromBody] AddOrderDetailsRequest request, CancellationToken ct)
    { return (await service.Add(series, number, request, ct)).ToActionResult(); }
    [HttpGet("ordenes/{series}/{number}/impresion")]
    public async Task<ActionResult<PrintOrder>> Print([RegularExpression("^[0-9]{3}$")] string series, [RegularExpression("^[0-9]{6}$")] string number, CancellationToken ct)
    { return (await service.Print(series, number, ct)).ToActionResult(); }
}
