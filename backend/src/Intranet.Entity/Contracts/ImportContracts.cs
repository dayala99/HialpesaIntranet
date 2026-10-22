using System.ComponentModel.DataAnnotations;
namespace Intranet.Entity;

public sealed class ImportRequest
{
    [Required, RegularExpression("^[A-Za-z0-9]{3}$")] public string ItemType { get; set; } = "";
    [Required, StringLength(3)] public string Origin { get; set; } = "";
    [StringLength(3)] public string Shipment { get; set; } = "";
    [StringLength(3)] public string Country { get; set; } = "";
    [Required, StringLength(2)] public string Area { get; set; } = "05";
    public DateTime RegisteredAt { get; set; } = DateTime.Today;
    public DateTime? GuideDate { get; set; }
    public DateTime? PaymentDate { get; set; }
    public DateTime? DueDate { get; set; }
    [StringLength(25)] public string Guide { get; set; } = "";
    [StringLength(200)] public string Observations { get; set; } = "";
}
public sealed record ImportSource(string SupplierCode, string Supplier, string AnnexType, string Annex, string Currency, string ExistingImport, string SupplierOrigin);

public sealed record ImportCreated(string Number, string Series, string OrderNumber);
