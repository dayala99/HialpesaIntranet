using Intranet.Api.Contracts;
using Intranet.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Intranet.Api.Controllers;

[ApiController, Route("api/v1/sesion")]
public sealed class SessionController(GestionIdentity identity) : ControllerBase
{
    [HttpGet] public ActionResult<SessionResponse> Get() => Ok(new SessionResponse(identity.IsDemo ? "Demostración" : User.Identity?.Name ?? "", identity.GetUser(User), identity.IsDemo, identity.CanRegister(User)));
}
