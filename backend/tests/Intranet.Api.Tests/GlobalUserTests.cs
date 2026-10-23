using System.Security.Claims;
using Intranet.Entity;
using Intranet.Service;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
namespace Intranet.Api.Tests;
public sealed class GlobalUserTests
{
    private static ClaimsPrincipal Principal(string code,bool authenticated=true)=>new(new ClaimsIdentity([new Claim(ClaimTypes.Name,"gestion:"+code),new Claim(IntranetLogin.GestionUserClaim,code)],authenticated?"Cookies":null));
    private static GestionIdentity Identity(bool writes=true)=>new(Options.Create(new GestionOptions{Mode="SqlServer",EnableOrderRegistration=writes}),new TestEnvironment());
    [Theory][InlineData("programador04")][InlineData("PROGRAMADOR04")]
    public void AuthenticatedGlobalUserHasApplicationReadAndWriteAccess(string code){var p=Principal(code);var identity=Identity();Assert.True(identity.IsGlobalUser(p));Assert.True(identity.CanRegister(p));Assert.NotNull(identity.GetReadAllWindowsUser(p));Assert.Equal(code,identity.GetUser(p));}
    [Fact] public void OrdinaryUsersDoNotBecomeGlobal(){var p=Principal("USUARIO");var identity=Identity();Assert.False(identity.IsGlobalUser(p));Assert.False(identity.CanRegister(p));Assert.Null(identity.GetReadAllWindowsUser(p));}
    [Fact] public void UnauthenticatedGlobalClaimDoesNotGrantAccess(){var p=Principal("programador04",false);var identity=Identity();Assert.False(identity.IsGlobalUser(p));Assert.False(identity.CanRegister(p));Assert.Null(identity.GetReadAllWindowsUser(p));}
    [Fact] public void DisplayNameAloneCannotGrantGlobalAccess(){var p=new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name,"programador04"),new Claim("display_name","programador04")],"Cookies"));Assert.False(Identity().IsGlobalUser(p));}
    [Fact] public void GlobalUserStillHonorsApplicationWriteSwitch(){Assert.True(Identity(false).IsGlobalUser(Principal("programador04")));Assert.False(Identity(false).CanRegister(Principal("programador04")));}
    [Fact] public async Task GlobalUserCanEditFormWhenGestionReadPermissionIsFalse(){
        var fake=new FakeRepository();var principal=Principal("programador04");var service=new QualityControlService(fake,Identity(),new RequestActor(principal,false,false,false,true,"test"),NullLogger<QualityControlService>.Instance);
        var result=await service.Read("Y2035",1,default);var screen=Assert.IsType<QualityControlScreen>(result.Value);Assert.True(screen.Detail.CanEdit);Assert.Equal("programador04",fake.User);
    }
    [Fact] public async Task OrdinaryUserRetainsQualityRestriction(){
        var fake=new FakeRepository();var principal=Principal("USUARIO");var identity=new GestionIdentity(Options.Create(new GestionOptions{Mode="SqlServer",EnableOrderRegistration=true,AllowAllAuthenticatedWrites=true}),new TestEnvironment());
        var service=new QualityControlService(fake,identity,new RequestActor(principal,false,false,false,true,"test"),NullLogger<QualityControlService>.Instance);
        var result=await service.Read("Y2035",1,default);Assert.False(Assert.IsType<QualityControlScreen>(result.Value).Detail.CanEdit);
    }
    private sealed class FakeRepository:IQualityControlRepository{
        public string User="";
        public Task<QualityControlScreen?> Read(string b,int s,string u,CancellationToken c){User=u;return Task.FromResult<QualityControlScreen?>(new(new(b,s,"TELA","","COLOR","Tela",new(),new(0,0,0,0,0,"",""),null,false,[]),[]));}
        public Task Save(string b,int s,QualityControlUpdate r,string u,CancellationToken c)=>throw new NotImplementedException();
        public Task<IReadOnlyList<DyeingCatalogOption>> Fabrics(string s,CancellationToken c)=>throw new NotImplementedException();
        public Task<IReadOnlyList<Dictionary<string,object?>>> Consumption(string b,int s,string u,CancellationToken c)=>throw new NotImplementedException();
    }
    private sealed class TestEnvironment:IWebHostEnvironment{
        public string ApplicationName{get;set;}="Test";public string EnvironmentName{get;set;}="Production";public string ContentRootPath{get;set;}="";public string WebRootPath{get;set;}="";public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();public IFileProvider WebRootFileProvider{get;set;}=new NullFileProvider();
    }
}
