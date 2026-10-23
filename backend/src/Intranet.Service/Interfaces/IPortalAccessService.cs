using Intranet.Entity;
using System.Security.Claims;
namespace Intranet.Service;
public interface IPortalAccessService {Task<PortalAccess> Read(ClaimsPrincipal user,CancellationToken ct);}
