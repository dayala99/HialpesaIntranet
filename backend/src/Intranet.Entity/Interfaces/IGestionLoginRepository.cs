namespace Intranet.Entity;

public sealed record GestionLoginUser(string Code, string Name);
public interface IGestionLoginRepository
{
    Task<GestionLoginUser?> VerifyAsync(string username, string password, CancellationToken ct);
}
