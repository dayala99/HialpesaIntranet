using System.ComponentModel.DataAnnotations;
namespace Intranet.Entity;

public sealed class ProviderSelection
{
    [Required, RegularExpression("^[0-9]{3}$")] public string Series { get; set; } = "";
    [Required, RegularExpression("^[0-9]{6}$")] public string Number { get; set; } = "";
    [Required, RegularExpression("^PO-[0-9]{1,20}$")] public string Po { get; set; } = "";
    [MaxLength(30)] public List<ProviderColorConfirmation> ColorConfirmations { get; set; } = [];
}
