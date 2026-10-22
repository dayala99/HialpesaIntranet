using System.ComponentModel.DataAnnotations;
namespace Intranet.Entity;

public sealed class ProviderAgentTicket { [Required, RegularExpression("^[A-F0-9]{64}$")] public string Token { get; set; } = ""; }
