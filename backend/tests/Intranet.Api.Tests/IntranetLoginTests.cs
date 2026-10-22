using Intranet.Entity;
using Intranet.Service;
using Intranet.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
namespace Intranet.Api.Tests;
public sealed class IntranetLoginTests
{
 private sealed class Users:IGestionLoginRepository
 {
  public int Calls;public bool Unavailable;
  public Task<GestionLoginUser?> VerifyAsync(string username,string password,CancellationToken ct){Calls++;if(Unavailable)throw new InvalidOperationException("Database unavailable");return Task.FromResult(username=="operador01"&&password=="Correct-Test-Password"?new GestionLoginUser("operador01","Operador de prueba"):null);}
 }
 [Fact]public async Task ExceptionWorksWithoutAnyDatabaseAccess(){var users=new Users{Unavailable=true};var principal=await new IntranetLogin(users).AuthenticateAsync(" PROGRAMADOR04 ","programador04",default);Assert.NotNull(principal);Assert.Equal(@"HIALPESA\programador04",principal.Identity!.Name);Assert.Equal("programador04",principal.FindFirst(IntranetLogin.GestionUserClaim)!.Value);Assert.Equal(0,users.Calls);}
 [Theory][InlineData("")][InlineData("incorrect")][InlineData("Programador04")]
 public async Task IncorrectExceptionPasswordNeverFallsBackToDatabase(string password){var users=new Users{Unavailable=true};Assert.Null(await new IntranetLogin(users).AuthenticateAsync("programador04",password,default));Assert.Equal(0,users.Calls);}
 [Fact]public async Task GestionUserReceivesCanonicalCodeAndDisplayName(){var users=new Users();var principal=await new IntranetLogin(users).AuthenticateAsync(" operador01 ","Correct-Test-Password",default);Assert.NotNull(principal);Assert.True(principal.Identity!.IsAuthenticated);Assert.Equal("gestion:operador01",principal.Identity.Name);Assert.Equal("operador01",principal.FindFirst(IntranetLogin.GestionUserClaim)!.Value);Assert.Equal("Operador de prueba",principal.FindFirst("display_name")!.Value);Assert.Equal(1,users.Calls);}
 [Theory][InlineData("operador01","wrong")][InlineData("unknown","Correct-Test-Password")][InlineData("operador01","Correct-Test-Password ")]
 public async Task UnknownUserOrIncorrectPasswordIsRejected(string code,string password)=>Assert.Null(await new IntranetLogin(new Users()).AuthenticateAsync(code,password,default));
 [Theory][InlineData("", "test")][InlineData("abcdefghijklmnop", "test")][InlineData("operador01", "")]
 public async Task InvalidCredentialsDoNotQueryDatabase(string code,string password){var users=new Users();Assert.Null(await new IntranetLogin(users).AuthenticateAsync(code,password,default));Assert.Equal(0,users.Calls);}
 [Fact]public async Task GestionIdentityKeepsOrdinaryUserSeparateFromAdministrator(){var principal=await new IntranetLogin(new Users()).AuthenticateAsync("operador01","Correct-Test-Password",default);var identity=new GestionIdentity(Options.Create(new GestionOptions{Mode="SqlServer",ReadAllWindowsUsers=[@"HIALPESA\programador04"]}),new Environment());Assert.Equal("operador01",identity.GetUser(principal!));Assert.Null(identity.GetReadAllWindowsUser(principal!));Assert.False(identity.CanRegister(principal!));}
 [Fact]public void AnonymousIdentityCannotUseGestionClaim(){var identity=new GestionIdentity(Options.Create(new GestionOptions()),new Environment());Assert.Throws<UnauthorizedAccessException>(()=>identity.GetUser(new ClaimsPrincipal(new ClaimsIdentity([new Claim(IntranetLogin.GestionUserClaim,"programador04")]))));}
 [Fact]public async Task GlobalWriteSwitchAllowsGestionUsersButNotAnonymous(){var principal=await new IntranetLogin(new Users()).AuthenticateAsync("operador01","Correct-Test-Password",default);var configuration=new GestionOptions{Mode="SqlServer",EnableOrderRegistration=true,AllowAllAuthenticatedWrites=true};var identity=new GestionIdentity(Options.Create(configuration),new Environment());Assert.True(identity.CanRegister(principal!));Assert.False(identity.CanRegister(new ClaimsPrincipal(new ClaimsIdentity())));configuration.EnableOrderRegistration=false;Assert.False(identity.CanRegister(principal!));}
 private sealed class Environment:IWebHostEnvironment
 {
  public string ApplicationName{get;set;}="Test";public string EnvironmentName{get;set;}="Production";public string ContentRootPath{get;set;}="";public string WebRootPath{get;set;}="";public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();public IFileProvider WebRootFileProvider{get;set;}=new NullFileProvider();
 }
}

