using Intranet.Entity;
namespace Intranet.Service;

public interface IProviderService
{
    Task<ServiceResult> Pos(string series, string number, CancellationToken ct);
    Task<ServiceResult> Prepare(ProviderSelection selection, CancellationToken ct);
    Task<ServiceResult> PreparePoLaunch(ProviderPoSelection selection, CancellationToken ct);
    Task<ServiceResult> PrepareLaunch(ProviderSelection selection, CancellationToken ct);
    ServiceResult Redeem(ProviderAgentTicket ticket);
    ServiceResult LaunchStatus(ProviderAgentTicket ticket);
    Task<ServiceResult> Login(ProviderSelection selection, CancellationToken ct);
}
