using System.ComponentModel.DataAnnotations;

namespace Intranet.Api.Contracts;

public sealed record LookupOption(string Code, string Description);
public sealed record RegistrationCatalogs(IReadOnlyList<LookupOption> Conditions, IReadOnlyList<LookupOption> Discounts, IReadOnlyList<LookupOption> Currencies, IReadOnlyList<LookupOption> DeliveryLocations, IReadOnlyList<LookupOption> CostCenters, IReadOnlyList<LookupOption>? Classes = null);
public sealed class CreateOrderRequest : IValidatableObject
{
    [Required, RegularExpression("^01$", ErrorMessage="El registro web admite actualmente Compra Avíos Producción (clase 01).")] public string ClassCode { get; set; } = "01";
    [Required, StringLength(12)] public string SupplierCode { get; set; } = "";
    [Required, StringLength(3)] public string ConditionCode { get; set; } = "";
    [Required, StringLength(3)] public string DiscountCode { get; set; } = "";
    [Required, StringLength(3)] public string CurrencyCode { get; set; } = "";
    [Required, StringLength(3)] public string DeliveryLocationCode { get; set; } = "";
    [Required, StringLength(16)] public string CostCenterCode { get; set; } = "";
    [StringLength(8)] public string ManufacturingGroup { get; set; } = "";
    [MaxLength(50)] public List<string> AdditionalManufacturingGroups { get; set; } = [];
    [Range(typeof(decimal), "0", "100")] public decimal TaxPercent { get; set; } = 18;
    public DateTime DeliveryStart { get; set; }
    public DateTime DeliveryEnd { get; set; }
    [StringLength(2000)] public string Observations { get; set; } = "";
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (AdditionalManufacturingGroups.Any(g=>string.IsNullOrWhiteSpace(g)||g.Trim().Length>8))
            yield return new("Cada código de grupo debe tener entre 1 y 8 caracteres.", [nameof(AdditionalManufacturingGroups)]);
        if (DeliveryStart == default || DeliveryEnd == default || DeliveryEnd.Date < DeliveryStart.Date)
            yield return new("Revisa el intervalo de fechas de entrega.", [nameof(DeliveryStart),nameof(DeliveryEnd)]);
    }
}
public sealed record OrderReference(string Series, string Number);
public sealed record OrderRegistrationHeader(string Series, string Number, string SupplierCode, string Status, string ClassCode, string ManufacturingGroup);
public sealed record PendingRequirement(string Key, string Factory, string ProductionOrder, int PresentationCode, string Presentation, string Component, string ItemCode, string Description, string Combination, string Color, string Size, string Destination, string Style, string ProviderReference, decimal Required, decimal Purchase, decimal UnitPrice);
public sealed class AddOrderDetailsRequest
{
    [Required, StringLength(5)] public string ProductionOrder { get; set; } = "";
    [Required, StringLength(8), MinLength(3)] public string ItemCode { get; set; } = "";
    [Required, MinLength(1), MaxLength(500)] public List<string> RequirementKeys { get; set; } = [];
    [Required, Range(typeof(decimal), "0", "99999999")] public decimal? UnitPrice { get; set; }
}
public sealed record SavedItem(string Sequence, decimal Required, decimal UnitPrice);
public sealed record DetailsSavedResponse(string Series, string Number, int RequirementsAdded, int ItemsAdded, decimal RequiredTotal, decimal PurchaseTotal);
public sealed record PrintOrder(PurchaseOrder Order, IReadOnlyList<OrderItem> Items, string Currency, string Conditions, string DeliveryLocation, string ManufacturingGroup, string Observations, decimal TaxPercent, DateTime? DeliveryStart, DateTime? DeliveryEnd, IReadOnlyList<PrintLine> Lines, IReadOnlyList<PrintBreakdown> Breakdown, IReadOnlyList<GestionPrintLine> ReportRows);
public sealed record PrintLine(string Sequence, string Code, string Description, string Size, decimal Quantity, decimal UnitPrice, decimal Amount);

public sealed record PrintBreakdown(string Sequence,string Item,string Combination,string Measure,string CustomerStyle,string ProductionOrder,decimal Required,decimal? Purchased,bool Calculated);

public sealed record GestionPrintLine(string Item,string Combination,string Color,string Measure,string Destination,string CustomerStyle,string Unit,decimal UnitPrice,decimal Required,decimal Purchased,decimal Received,decimal Conversion,string SupplierItem,string ProviderReference,string ProductionOrders);

public sealed class AddManufacturingGroupsRequest { [Required,MaxLength(50)] public List<string> Groups {get;set;}=[]; [Required,MaxLength(50)] public List<string> RemovedGroups {get;set;}=[]; }

public sealed class UpdateOrderRequest : IValidatableObject
{
 [Required,StringLength(12)]public string SupplierCode{get;set;}="";
 [Required,StringLength(3)]public string ConditionCode{get;set;}="";
 [Required,StringLength(3)]public string DiscountCode{get;set;}="";
 [Required,StringLength(3)]public string CurrencyCode{get;set;}="";
 [Required,StringLength(3)]public string DeliveryLocationCode{get;set;}="";
 [Required,StringLength(16)]public string CostCenterCode{get;set;}="";
 [Range(typeof(decimal),"0","100")]public decimal TaxPercent{get;set;}
 public DateTime DeliveryStart{get;set;}public DateTime DeliveryEnd{get;set;}
 [StringLength(2000)]public string Observations{get;set;}="";
 public IEnumerable<ValidationResult> Validate(ValidationContext context){if(DeliveryStart==default||DeliveryEnd==default||DeliveryEnd.Date<DeliveryStart.Date)yield return new("Revisa las fechas de entrega.");}
}

public sealed class UpdateOrderItemRequest : IValidatableObject {
 [Range(typeof(decimal),"0","100000000")]public decimal Purchased{get;set;}
 [Range(typeof(decimal),"0","99999999")]public decimal UnitPrice{get;set;}
 [Required,StringLength(3)]public string DiscountCode{get;set;}="";
 [Range(typeof(decimal),"0","100")]public decimal TaxPercent{get;set;}
 public DateTime DeliveryStart{get;set;}public DateTime DeliveryEnd{get;set;}
 [StringLength(2000)]public string Observations{get;set;}="";
 public IEnumerable<ValidationResult> Validate(ValidationContext context){if(DeliveryStart==default||DeliveryEnd==default||DeliveryEnd.Date<DeliveryStart.Date)yield return new("Revisa las fechas de entrega.");}
}

public sealed class RemoveRequirementLinksRequest { [Required,MinLength(1),MaxLength(500)] public List<string> Keys{get;set;}=[]; }

public sealed record SupplierDefaults(string ConditionCode,string DiscountCode,string Origin,decimal? TaxPercent);
