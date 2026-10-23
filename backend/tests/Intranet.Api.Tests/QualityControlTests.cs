using Intranet.Entity;
using Intranet.Data;
using Intranet.Service;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
namespace Intranet.Api.Tests;
public sealed class QualityControlTests
{
    private static QualityControlUpdate Valid()=>new(){Values=new(){WidthType="T",ShrinkType="S",RamaType="S",Approval="A"},Recommendations=["001"]};
    [Fact] public void ValidZeroMeasurementFormIsAccepted()=>QualityControlService.Validate(Valid());
    [Fact] public void DisapprovalRequiresRejectedKilos(){var r=Valid();r.Values.Approval="D";Assert.Throws<ArgumentException>(()=>QualityControlService.Validate(r));r.Values.RejectedWeight=2.5m;QualityControlService.Validate(r);}
    [Fact] public void ApprovedCannotCarryRejectedKilos(){var r=Valid();r.Values.RejectedWeight=1;Assert.Throws<ArgumentException>(()=>QualityControlService.Validate(r));}
    [Fact] public void WidthKeepsThreeDecimals(){var r=Valid();r.Values.Width=1.235m;QualityControlService.Validate(r);r.Values.Width=1.2355m;Assert.Throws<ArgumentException>(()=>QualityControlService.Validate(r));}
    [Fact] public void WeightDoesNotSilentlyRound(){var r=Valid();r.Values.Weight=120.5m;Assert.Throws<ArgumentException>(()=>QualityControlService.Validate(r));}
    [Fact] public void RecommendationDuplicatesAreRejected(){var r=Valid();r.Recommendations=["001","001"];Assert.Throws<ArgumentException>(()=>QualityControlService.Validate(r));}
    [Fact] public void UnsupportedApprovalIsRejected(){var r=Valid();r.Values.Approval="X";Assert.Throws<ArgumentException>(()=>QualityControlService.Validate(r));}
    [Fact] public void LongObservationIsRejected(){var r=Valid();r.Values.Observations=new string('a',1001);Assert.Throws<ArgumentException>(()=>QualityControlService.Validate(r));}
    [Fact] public void SqlParametersUseServerIdentityAndExactLegacyNames(){
        var detail=new QualityControlDetail("Y2035",2,"JE00418","001","N33ARS","Tela",new(),new(0,0,0,0,0,"",""),null,true,[]);
        var values=Valid().Values;values.Width=1.235m;values.AlternativeFabric="JE00442";
        var parameters=QualityControlRepository.WriteParameters(detail,values,"USUARIO");
        Assert.Equal(41,parameters.ParameterNames.Count());Assert.Equal("TI",parameters.Get<string>("COD_TIPORDTRA"));Assert.Equal("JE00418",parameters.Get<string>("COD_TELA"));
        Assert.Equal("T",parameters.Get<string>("TIP_ANCHO"));Assert.Equal(1.235m,parameters.Get<decimal>("ANCHO_ACAB"));Assert.Equal("JE00442",parameters.Get<string>("COD_TELA_RELACIONDA"));Assert.Equal("USUARIO",parameters.Get<string>("COD_USUARIO"));
    }
    [Theory][InlineData(false,true)][InlineData(true,false)]
    public async Task MissingWritePermissionOrHeaderNeverCallsRepository(bool permission,bool header){
        var fake=new FakeRepository();var identity=new GestionIdentity(Options.Create(new GestionOptions{Mode="SqlServer",EnableOrderRegistration=permission,AllowAllAuthenticatedWrites=true}),new Environment());
        var principal=new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name,"gestion:TEST"),new Claim(IntranetLogin.GestionUserClaim,"TEST")],"Test"));
        var service=new QualityControlService(fake,identity,new RequestActor(principal,header,false,false,true,"test"),NullLogger<QualityControlService>.Instance);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.Save("Y2035",1,Valid(),default));Assert.Equal(0,fake.Saves);
    }
    private sealed class FakeRepository:IQualityControlRepository {
        public int Saves;
        public Task Save(string b,int s,QualityControlUpdate r,string u,CancellationToken c){Saves++;return Task.CompletedTask;}
        public Task<QualityControlScreen?> Read(string b,int s,string u,CancellationToken c)=>Task.FromResult<QualityControlScreen?>(null);
        public Task<IReadOnlyList<DyeingCatalogOption>> Fabrics(string s,CancellationToken c)=>Task.FromResult<IReadOnlyList<DyeingCatalogOption>>([]);
        public Task<IReadOnlyList<Dictionary<string,object?>>> Consumption(string b,int s,string u,CancellationToken c)=>Task.FromResult<IReadOnlyList<Dictionary<string,object?>>>([]);
    }
    private sealed class Environment:IWebHostEnvironment {
        public string ApplicationName{get;set;}="Test";public string EnvironmentName{get;set;}="Production";public string ContentRootPath{get;set;}="";public string WebRootPath{get;set;}="";public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();public IFileProvider WebRootFileProvider{get;set;}=new NullFileProvider();
    }
}
