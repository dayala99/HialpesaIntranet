using System.ComponentModel.DataAnnotations;
namespace Intranet.Entity;

public sealed class ProviderColorConfirmation
{
    [Required, MaxLength(80)] public string Style { get; set; } = "";
    [Required, MaxLength(80)] public string ColorCode { get; set; } = "";
    [Required, MaxLength(300)] public string ProviderColor { get; set; } = "";
    [Required, MaxLength(600)] public string GestionColor { get; set; } = "";
}
