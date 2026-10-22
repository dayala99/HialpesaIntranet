using Intranet.Entity;
using System.ComponentModel.DataAnnotations;

namespace Intranet.Entity;

public sealed record SessionResponse(string DisplayName, string GestionUser, bool Demo, bool CanWriteDatabase);
public sealed record PurchaseOrder(string Series, string Number, string Supplier, string Status, string Class, string Client, DateTime? CreatedAt);
public sealed record OrderItem(string Sequence, string ItemCode, string Description, string Size, string Style, decimal Required, decimal Purchased, decimal Received, string ProductionOrders = "");
public sealed record OrderDetail(PurchaseOrder Order, IReadOnlyList<OrderItem> Items, IReadOnlyList<LookupOption>? ManufacturingGroups = null);
public sealed record Requirement(string Factory, string ProductionOrder, string PresentationCode, string Presentation, string Component, string ItemCode, string Style, string Color, string Size, string Destination, decimal Quantity);

public sealed class MatrixRow
{
    [Required, StringLength(50)] public string ProductionOrder { get; set; } = "";
    [Required, StringLength(50)] public string Style { get; set; } = "";
    [Required, StringLength(100)] public string Color { get; set; } = "";
    [Required, StringLength(30)] public string Size { get; set; } = "";
    [StringLength(20)] public string Factory { get; set; } = "";
    [StringLength(30)] public string PresentationCode { get; set; } = "";
    [StringLength(30)] public string Destination { get; set; } = "";
    [Range(typeof(decimal), "0", "100000000")] public decimal Quantity { get; set; }
}
public sealed class ArticleRule
{
    [Required, StringLength(30)] public string ItemCode { get; set; } = "";
    [Required, StringLength(100)] public string Description { get; set; } = "";
    [Range(5, 5, ErrorMessage = "La regla automática utiliza múltiplos de 5.")] public int Multiple { get; set; } = 5;
}
public sealed class PreviewRequest
{
    [Required, MinLength(1), MaxLength(500)] public List<MatrixRow> Rows { get; set; } = [];
    [Required, MinLength(1), MaxLength(10)] public List<ArticleRule> Articles { get; set; } = [];
}
public sealed record CalculatedRow(MatrixRow Source, string ItemCode, string Article, int Multiple, decimal Required, decimal Purchase, decimal Extra);
public sealed record PreviewResponse(IReadOnlyList<CalculatedRow> Rows, decimal BaseUnits, decimal PurchaseUnits, decimal ExtraUnits);

public sealed record PoSummary(string Po, string ClientCode, string? Client, string? Description, DateTime? CreatedAt);

public sealed record PoDetailLine(string Lot, string Style, string? Description, string ColorCode, string? Color, string Size, decimal Required, decimal Shipped, string? Upc)
{
    public decimal Purchased => QuantityRules.RoundRequiredQuantity(Required);
}
