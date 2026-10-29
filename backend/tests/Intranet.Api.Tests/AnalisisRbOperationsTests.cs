using Intranet.Entity;
using Intranet.Service;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using System.Security.Claims;
namespace Intranet.Api.Tests;
public sealed class AnalisisRbOperationsTests {
 private static AnalisisRbOperationsService Service(Repo repo,bool authenticated=true)=>new(repo,new GestionIdentity(Options.Create(new GestionOptions{Mode="SqlServer"}),new Env()),new RequestActor(new ClaimsPrincipal(new ClaimsIdentity(authenticated?[new Claim(IntranetLogin.GestionUserClaim,"OPERARIO")]:[],authenticated?"Test":null)),false,false,false,true,"test"));
 [Fact]public async Task FiltersOnlyCosturaAndComputesTotals(){var repo=new Repo();var result=await Service(repo).Read(" j1825 ",null,default);var data=Assert.IsType<AnalisisRbOperations>(result.Value);Assert.Equal("J1825",repo.Np);Assert.Null(repo.Variant);Assert.Equal(2,data.Rows.Count);Assert.All(data.Rows,row=>Assert.Equal("CS",row.Sector));Assert.Equal(1.5m,data.TotalTime);Assert.Equal(1m,data.MachineTime);Assert.Equal("001",data.Rows[0].Sequence);}
 [Fact]public async Task NormalizesExplicitVariant(){var repo=new Repo();await Service(repo).Read("J1825"," c ",default);Assert.Equal("C",repo.Variant);}
 [Theory][InlineData("J182",null)][InlineData("J18%5",null)][InlineData("J1825","CC")][InlineData("J1825","'")]public async Task InvalidInputDoesNotReachDatabase(string np,string? variant){var repo=new Repo();await Assert.ThrowsAsync<ArgumentException>(()=>Service(repo).Read(np,variant,default));Assert.Null(repo.Np);}
 [Fact]public async Task RequiresAuthenticatedGestionIdentity(){var repo=new Repo();await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>Service(repo,false).Read("J1825",null,default));Assert.Null(repo.Np);}
 [Fact]public async Task ReturnsNotFoundForUnknownNp(){var repo=new Repo{Missing=true};Assert.Equal(ResultKind.NotFound,(await Service(repo).Read("Z9999",null,default)).Kind);}
 [Fact]public async Task DefaultsToAvailableSectorWhenCosturaIsMissing(){var repo=new Repo{OnlyCorte=true};var data=Assert.IsType<AnalisisRbOperations>((await Service(repo).Read("J1825",null,default)).Value);Assert.Single(data.Rows);Assert.Equal("CO",data.SelectedSector);Assert.Equal(7,data.TotalTime);}
 [Fact]public async Task SelectsCorteAndKeepsAllSectorOptions(){var repo=new Repo();var data=Assert.IsType<AnalisisRbOperations>((await Service(repo).Read("J1825",null,default," co ")).Value);Assert.Equal("CO",data.SelectedSector);Assert.Single(data.Rows);Assert.Equal(7,data.TotalTime);Assert.Equal(new[]{"CS","CO"},data.Sectors.Select(item=>item.Code));}
 [Fact]public async Task RejectsUnavailableSector(){await Assert.ThrowsAsync<ArgumentException>(()=>Service(new Repo()).Read("J1825",null,default,"AC"));}
 [Fact]public async Task RejectsMalformedSectorBeforeDatabase(){var repo=new Repo();await Assert.ThrowsAsync<ArgumentException>(()=>Service(repo).Read("J1825",null,default,"CS'"));Assert.Null(repo.Np);}
 private sealed class Repo:IAnalisisRbOperationsRepository {public string? Np,Variant;public bool Missing,OnlyCorte;public Task<AnalisisRbOperations?> Read(string np,string? variant,CancellationToken ct){Np=np;Variant=variant;AnalisisRbOperation Op(string sector,string seq,decimal time,string family)=>new(sector,"948","PANTALON",seq,"369595","0","UNIR","01","01","RECTA","A",family,time);IReadOnlyList<AnalisisRbOperation> rows=OnlyCorte?[Op("CO","001",7,"C")]:[Op("CS","001",1,"C"),Op("CO","001",7,"C"),Op("CS","002",.5m,"M")];return Task.FromResult<AnalisisRbOperations?>(Missing?null:new(np,"99863","PANTALON","0",variant??"0",[new("0","PANTALON","D")],rows));}}
 private sealed class Env:IHostEnvironment{public string ApplicationName{get;set;}="Test";public string EnvironmentName{get;set;}="Production";public string ContentRootPath{get;set;}="";public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();}
}
