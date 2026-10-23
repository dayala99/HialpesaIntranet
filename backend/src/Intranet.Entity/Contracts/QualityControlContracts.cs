using System.ComponentModel.DataAnnotations;
namespace Intranet.Entity;

public sealed class QualityControlValues
{
    [Range(typeof(decimal), "0", "9999")] public decimal Weight { get; set; }
    [Range(typeof(decimal), "0", "99.999")] public decimal Width { get; set; }
    [Range(typeof(decimal), "-9999.99", "9999.99")] public decimal ShrinkWidth { get; set; }
    [Range(typeof(decimal), "-9999.99", "9999.99")] public decimal ShrinkLength { get; set; }
    [Range(typeof(decimal), "-999.99", "999.99")] public decimal Twist { get; set; }
    [Range(typeof(decimal), "-9999.99", "9999.99")] public decimal ThirdShrinkWidth { get; set; }
    [Range(typeof(decimal), "-9999.99", "9999.99")] public decimal ThirdShrinkLength { get; set; }
    [Range(typeof(decimal), "-999.99", "999.99")] public decimal ThirdTwist { get; set; }
    [Range(typeof(decimal), "0", "9999")] public decimal WashedWeight { get; set; }
    [Range(typeof(decimal), "0", "9999")] public decimal LaundryWeight { get; set; }
    [Range(typeof(decimal), "-9999.99", "9999.99")] public decimal ElongationLength { get; set; }
    [Range(typeof(decimal), "-9999.99", "9999.99")] public decimal ElongationWidth { get; set; }
    [Range(typeof(decimal), "0", "99999999999.99")] public decimal RejectedWeight { get; set; }
    [Range(typeof(decimal), "0", "9999.99")] public decimal PlantWidth { get; set; }
    [Range(typeof(decimal), "0", "999.99")] public decimal Resistance { get; set; }
    [Range(typeof(decimal), "-9999.99", "9999.99")] public decimal ResidualShrinkWidth { get; set; }
    [Range(typeof(decimal), "-9999.99", "9999.99")] public decimal ResidualShrinkLength { get; set; }
    [Range(typeof(decimal), "-999.99", "999.99")] public decimal ResidualTwist { get; set; }
    [Range(typeof(decimal), "0", "9999")] public decimal RamaWeight { get; set; }
    [Range(typeof(decimal), "0", "9999.99")] public decimal RamaWidth { get; set; }
    [Range(typeof(decimal), "-9999.99", "9999.99")] public decimal RamaShrinkWidth { get; set; }
    [Range(typeof(decimal), "-9999.99", "9999.99")] public decimal RamaShrinkLength { get; set; }
    [Range(typeof(decimal), "-999.99", "999.99")] public decimal RamaTwist { get; set; }
    [Range(typeof(decimal), "0", "9999")] public decimal RamaWashedWeight { get; set; }
    [Required, StringLength(1)] public string WidthType { get; set; } = "";
    [Required, StringLength(1)] public string ShrinkType { get; set; } = "";
    [Required, StringLength(1)] public string RamaType { get; set; } = "";
    [Required, RegularExpression("^[AD]$")] public string Approval { get; set; } = "A";
    [StringLength(1000)] public string Observations { get; set; } = "";
    [StringLength(3)] public string RejectionReason { get; set; } = "";
    [StringLength(8)] public string AlternativeFabric { get; set; } = "";
    [StringLength(15)] public string Skew { get; set; } = "";
    [StringLength(15)] public string Inclination { get; set; } = "";
    [StringLength(1000)] public string WashType { get; set; } = "";
}
public sealed class QualityControlUpdate
{
    [Required] public QualityControlValues Values { get; set; } = new();
    public DateTime? ExpectedModifiedAt { get; set; }
    [MaxLength(200)] public string[] Recommendations { get; set; } = [];
}
public sealed record QualityRecommendation(string Code, string Description, bool Selected);
public sealed record QualityReference(decimal Weight, decimal Width, decimal ShrinkWidth, decimal ShrinkLength, decimal FabricTwist, string WidthType, string AlternativeDescription);
public sealed record QualityControlDetail(string Batch, int Sequence, string Fabric, string Combination, string Color, string Description, QualityControlValues Values, QualityReference Programmed, DateTime? ModifiedAt, bool CanEdit, IReadOnlyList<QualityRecommendation> Recommendations);
public sealed record QualityControlScreen(QualityControlDetail Detail, IReadOnlyList<DyeingCatalogOption> Catalogs);
public sealed class QualityControlPendingException : Exception { public QualityControlPendingException() : base("Falta crear PA_TI_ORDTRA_TINTORERIA_ITEMS_S0002 para habilitar Datos Control Calidad.") {} }
