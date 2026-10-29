using Intranet.Data;
using Intranet.Entity;
using Intranet.Service;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using System.Security.Claims;
namespace Intranet.Api.Tests;
public sealed class AnalisisRbImageTests {
 [Theory][InlineData(@"\\hialpesa103\EstPro$\EP99863.jpg",true)][InlineData(@"\\hialpesa103\EstPro$\..\secret.jpg",false)][InlineData(@"\\hialpesa103\EstPro$-other\x.jpg",false)][InlineData(@"C:\Windows\x.jpg",false)][InlineData(@"relative.jpg",false)]public void RestrictsImagesToConfiguredDirectory(string path,bool expected)=>Assert.Equal(expected,AnalisisRbImageRepository.IsAllowedPath(path,[@"\\hialpesa103\EstPro$"]));
 private static AnalisisRbImageService Service(Repo repo,bool authenticated=true)=>new(repo,new GestionIdentity(Options.Create(new GestionOptions{Mode="SqlServer"}),new Env()),new RequestActor(new ClaimsPrincipal(new ClaimsIdentity(authenticated?[new Claim(IntranetLogin.GestionUserClaim,"OPERARIO")]:[],authenticated?"Test":null)),false,false,false,true,"test"));
 [Fact]public async Task ReturnsImageAndNormalizesNp(){var repo=new Repo();var result=await Service(repo).Read(" j1825 ",default);Assert.Equal("J1825",repo.Np);Assert.Equal("image/jpeg",result.ContentType);Assert.Equal(new byte[]{1,2,3},result.Content);}
 [Fact]public async Task ReturnsNotFoundForMissingImage(){var repo=new Repo{Image=null};Assert.Equal(ResultKind.NotFound,(await Service(repo).Read("J1825",default)).Kind);}
 [Fact]public async Task RequiresAuthentication(){var repo=new Repo();await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>Service(repo,false).Read("J1825",default));Assert.Null(repo.Np);}
 [Fact]public async Task RejectsInvalidNpWithoutReadingFiles(){var repo=new Repo();await Assert.ThrowsAsync<ArgumentException>(()=>Service(repo).Read("../x",default));Assert.Null(repo.Np);}
 [Fact]public async Task HistoricalImageUsesSavedRecord(){var repo=new Repo();var result=await Service(repo).ReadHistory(" j1825 ",42,default);Assert.Equal(42L,repo.HistoryId);Assert.Equal("J1825",repo.Np);Assert.Equal("image/jpeg",result.ContentType);}
 [Fact]public async Task MissingHistoricalImageDoesNotFallBackToCurrent(){var repo=new Repo{Image=null};Assert.Equal(ResultKind.NotFound,(await Service(repo).ReadHistory("J1825",42,default)).Kind);Assert.Equal(42L,repo.HistoryId);}
 [Fact]public async Task HistoricalImageRequiresAuthentication(){var repo=new Repo();await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>Service(repo,false).ReadHistory("J1825",42,default));Assert.Null(repo.HistoryId);}
 [Theory][InlineData(0)][InlineData(-1)]public async Task RejectsInvalidHistoryId(long id){var repo=new Repo();await Assert.ThrowsAsync<ArgumentException>(()=>Service(repo).ReadHistory("J1825",id,default));Assert.Null(repo.HistoryId);}
 private sealed class Repo:IAnalisisRbImageRepository{public string? Np;public long? HistoryId;public AnalisisRbImage? Image=new([1,2,3],"image/jpeg");public Task<AnalisisRbImage?> Read(string np,CancellationToken ct){Np=np;return Task.FromResult(Image);}public Task<AnalisisRbImage?> ReadHistory(string np,long id,CancellationToken ct){Np=np;HistoryId=id;return Task.FromResult(Image);}}
 private sealed class Env:IHostEnvironment{public string ApplicationName{get;set;}="Test";public string EnvironmentName{get;set;}="Production";public string ContentRootPath{get;set;}="";public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();}
}
