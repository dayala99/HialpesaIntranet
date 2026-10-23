using System.ComponentModel.DataAnnotations;
namespace Intranet.Entity;

public sealed class DyeingFilter
{
    [Range(1, 7)] public int Mode { get; set; } = 4;
    [StringLength(5)] public string Client { get; set; } = "";
    [StringLength(3)] public string Series { get; set; } = "";
    [StringLength(6)] public string OrderNumber { get; set; } = "";
    [StringLength(6)] public string Color { get; set; } = "";
    [StringLength(5)] public string Batch { get; set; } = "";
    [StringLength(1)] public string Status { get; set; } = "";
    [StringLength(3)] public string Factory { get; set; } = "";
    [StringLength(5)] public string ProductionOrder { get; set; } = "";
    [StringLength(1)] public string Fiber { get; set; } = "";
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    [Range(1, 100000)] public int Page { get; set; } = 1;
}
public sealed record DyeingCatalogOption(string Kind, string Code, string Description);
public sealed record DyeingPage(IReadOnlyList<Dictionary<string, object?>> Rows, int TotalRows, int Page, int PageSize);
public sealed class DyeingProcedurePendingException : Exception
{
    public DyeingProcedurePendingException() : base("El filtro está pendiente de habilitación: falta crear PA_TI_ORDTRA_TINTORERIA_ITEMS_S0001 en HIALPESA.") { }
}
