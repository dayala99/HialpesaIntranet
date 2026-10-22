using System.ComponentModel.DataAnnotations;
namespace Intranet.Entity;

public sealed class ProviderPoSelection
{
    [Required, RegularExpression("^PO-[0-9]{1,20}$")] public string Po { get; set; } = "";
    [Required, StringLength(20)] public string ClientCode { get; set; } = "";
}
