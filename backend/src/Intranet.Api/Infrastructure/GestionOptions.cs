namespace Intranet.Api.Infrastructure;

public sealed class GestionOptions
{
    public bool EnableOrderRegistration { get; set; }
    public string[] WriteWindowsUsers { get; set; } = [];
    public string Mode { get; set; } = "Demo";
    public int CommandTimeoutSeconds { get; set; } = 60;
    public Dictionary<string, string> WindowsUsers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string[] ReadAllWindowsUsers { get; set; } = [];
}
