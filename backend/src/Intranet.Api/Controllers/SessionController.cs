using System.ComponentModel.DataAnnotations;
using Intranet.Api.Contracts;
using Intranet.Api.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
namespace Intranet.Api.Controllers;
[ApiController,Route("api/v1/sesion")]
public sealed class SessionController(GestionIdentity identity,IntranetLogin login):ControllerBase {
 public sealed class LoginRequest{
  [Required,MaxLength(80)]public string Username{get;set;}="";
  [Required,MaxLength(200)]public string Password{get;set;}="";
 }
 private SessionResponse Describe(System.Security.Claims.ClaimsPrincipal user)=>new(user.FindFirst("display_name")?.Value??user.Identity?.Name??"",identity.GetUser(user),identity.IsDemo,identity.CanRegister(user));
 [HttpGet]public ActionResult<SessionResponse> Get()=>Ok(Describe(User));
 [AllowAnonymous,HttpPost("login"),EnableRateLimiting("login")]
 public async Task<ActionResult<SessionResponse>> Login(LoginRequest request,CancellationToken ct){
  if(Request.Headers["X-Intranet-Session"]!="1")return BadRequest(new{title="Solicitud de inicio de sesion no valida."});
  var principal=await login.AuthenticateAsync(request.Username,request.Password,ct);
  if(principal is null)return Unauthorized(new{title="Usuario o contrasena incorrectos."});
  var response=Describe(principal);
  await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,principal,new AuthenticationProperties{IsPersistent=false});
  return Ok(response);
 }
 [HttpPost("logout")]
 public async Task<IActionResult> Logout(){
  if(Request.Headers["X-Intranet-Session"]!="1")return BadRequest();
  await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);return NoContent();
 }
}

