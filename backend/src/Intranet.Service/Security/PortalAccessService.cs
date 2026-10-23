using Intranet.Entity;
using System.Security.Claims;
namespace Intranet.Service;
public sealed class PortalAccessService(IGestionAccessRepository repository,GestionIdentity identity):IPortalAccessService {
 public static readonly string[] AllOptions=["orders.create","orders.query","imports","dyeing.batches","quality.events","quality.unfinished"];
 private Task<PortalAccess>? snapshot;private string? snapshotUser;
 public Task<PortalAccess> Read(ClaimsPrincipal user,CancellationToken ct){var code=identity.GetUser(user);if(identity.IsGlobalUser(user))return Task.FromResult(new PortalAccess(true,AllOptions,[]));if(identity.IsDemo)return Task.FromResult(new PortalAccess(false,AllOptions,[]));if(snapshot is not null&&snapshotUser==code)return snapshot;snapshotUser=code;return snapshot=Load(code,ct);}
 private async Task<PortalAccess> Load(string user,CancellationToken ct){var access=await repository.Read(user,"01",ct);return new(false,Map(access.Options),access.Profiles);}
 public static IReadOnlyList<string> Map(IEnumerable<GestionOptionAccess> options){var grants=options.Select(x=>x.Application.Trim()+":"+x.Option.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);var keys=new List<string>();
 if(grants.Contains("07:mnuOrdComp"))keys.Add("orders.create");
 if(grants.Contains("07:mnuOrdComp")||grants.Contains("29:frmrptgest"))keys.Add("orders.query");
 if(grants.Contains("07:mOImportac"))keys.Add("imports");
 if(grants.Contains("15:mnuPartOT"))keys.Add("dyeing.batches");
 if(grants.Contains("25:mCCTinto"))keys.Add("quality.events");
 if(grants.Contains("25:mEvTintoTe"))keys.Add("quality.unfinished");
 return keys;}
}
