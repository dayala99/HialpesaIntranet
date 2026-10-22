namespace Intranet.Entity;

public sealed class ApplicationLoginOptions
{
    public bool Enabled { get; set; }
    public string Username { get; set; } = "programador04";
    public string PermissionIdentity { get; set; } = "HIALPESA\\programador04";
    public string PasswordHash { get; set; } = "";
}
