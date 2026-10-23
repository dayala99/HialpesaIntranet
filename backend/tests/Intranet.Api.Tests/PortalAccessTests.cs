using Microsoft.Extensions.Hosting;
using System.Security.Claims;
using Intranet.Entity;
using Intranet.Service;
using Intranet.Api.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
namespace Intranet.Api.Tests;
public sealed class PortalAccessTests {
 static ClaimsPrincipal User(string code)=>new(new ClaimsIdentity([new Claim(ClaimTypes.Name,"gestion:"+code),new Claim(IntranetLogin.GestionUserClaim,code)],"Test"));
 static GestionIdentity Identity()=>new(Options.Create(new GestionOptions{Mode="SqlServer"}),new Env());
 [Fact]public void NoOptionsMeansNoAccess()=>Assert.Empty(PortalAccessService.Map([]));
 [Fact]public void ApplicationAndOptionMustMatch(){Assert.Empty(PortalAccessService.Map([new("15","mCCTinto")]));Assert.Equal(["quality.events"],PortalAccessService.Map([new("25","mCCTinto")]));}
 [Fact]public void UnionOfProfilesAllowsBothEventScreens(){Assert.Equal(["quality.events","quality.unfinished"],PortalAccessService.Map([new("25","mCCTinto"),new("25","mEvTintoTe"),new("25","mCCTinto")]));}
 [Fact]public void SpacesAndCaseAreNormalized()=>Assert.Equal(["dyeing.batches"],PortalAccessService.Map([new("15 "," MNUPARTOT ")]));
 [Fact]public async Task GlobalUserDoesNotDependOnDatabaseProfiles(){var repo=new Repo();var permissions=await new PortalAccessService(repo,Identity()).Read(User("PROGRAMADOR04"),default);Assert.True(permissions.IsGlobalUser);Assert.Equal(6,permissions.AllowedOptions.Count);Assert.Equal(0,repo.Reads);}
 [Fact]public async Task OrdinaryUserCannotClaimGlobalByDisplayName(){var repo=new Repo();var user=User("OPERARIO");((ClaimsIdentity)user.Identity!).AddClaim(new("display_name","programador04"));var permissions=await new PortalAccessService(repo,Identity()).Read(user,default);Assert.False(permissions.IsGlobalUser);Assert.Empty(permissions.AllowedOptions);Assert.Equal("OPERARIO",repo.User);Assert.Equal("01",repo.Company);}
 [Fact]public async Task AnonymousCannotUseGlobalClaim(){var user=new ClaimsPrincipal(new ClaimsIdentity([new Claim(IntranetLogin.GestionUserClaim,"programador04")]));await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>new PortalAccessService(new Repo(),Identity()).Read(user,default));}
 [Fact]public async Task SnapshotIsScopedToRequestUser(){var repo=new Repo();var service=new PortalAccessService(repo,Identity());await service.Read(User("A"),default);await service.Read(User("A"),default);Assert.Equal(1,repo.Reads);await service.Read(User("B"),default);Assert.Equal(2,repo.Reads);}
 [Theory][InlineData("QualityEvents","quality.events")][InlineData("UnfinishedQualityEvents","quality.unfinished")][InlineData("QualityControl","dyeing.batches")][InlineData("Imports","imports")]
 public async Task ApiDeniesUnauthorizedOption(string controller,string option){var ctx=Context(controller);await new PortalAccessFilter(new Access(new(false,[],[]))).OnAuthorizationAsync(ctx);Assert.Equal(403,Assert.IsType<ObjectResult>(ctx.Result).StatusCode);var allowed=Context(controller);await new PortalAccessFilter(new Access(new(false,[option],[]))).OnAuthorizationAsync(allowed);Assert.Null(allowed.Result);}
 [Fact]public async Task ApiAllowsGlobalUser(){var ctx=Context("UnfinishedQualityEvents");await new PortalAccessFilter(new Access(new(true,[],[]))).OnAuthorizationAsync(ctx);Assert.Null(ctx.Result);}
 [Fact]public void AgentRedemptionRemainsAnonymous(){var method=typeof(Intranet.Api.Controllers.ProviderController).GetMethod("Redeem")!;Assert.NotNull(method.GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute),true).SingleOrDefault());}
 [Fact]public void EveryBusinessControllerHasAccessMapping(){var controllers=typeof(Intranet.Api.Controllers.AviosController).Assembly.GetTypes().Where(t=>t.Name.EndsWith("Controller")&&typeof(ControllerBase).IsAssignableFrom(t)&&t.Name!="SessionController");foreach(var controller in controllers)Assert.NotEmpty(PortalAccessFilter.Required(controller.Name[..^10],"Read","GET"));}
 [Fact]public void ReadOnlyOrderProfileCannotWriteOrders(){Assert.Contains("orders.query",PortalAccessFilter.Required("OrderRegistration","List","GET"));Assert.Equal(["orders.create"],PortalAccessFilter.Required("OrderRegistration","UpdateHeader","PUT"));}
 static AuthorizationFilterContext Context(string controller){var http=new DefaultHttpContext{User=User("OPERARIO")};http.Request.Method="GET";return new(new ActionContext(http,new RouteData(),new ControllerActionDescriptor{ControllerName=controller,ActionName="Search"}),[]);}
 sealed class Access(PortalAccess access):IPortalAccessService{public Task<PortalAccess> Read(ClaimsPrincipal user,CancellationToken ct)=>Task.FromResult(access);}
 sealed class Repo:IGestionAccessRepository{public int Reads;public string User="",Company="";public Task<GestionAccess> Read(string user,string company,CancellationToken ct){Reads++;User=user;Company=company;return Task.FromResult(new GestionAccess([],[]));}}
 sealed class Env:IHostEnvironment{public string EnvironmentName{get;set;}="Development";public string ApplicationName{get;set;}="Test";public string ContentRootPath{get;set;}="";public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();}
}
