using System.ComponentModel.DataAnnotations;
namespace Intranet.Entity;

public sealed class LoginRequest
{
    [Required, MaxLength(80)] public string Username { get; set; } = "";
    [Required, MaxLength(200)] public string Password { get; set; } = "";
}
