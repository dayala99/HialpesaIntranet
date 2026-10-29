using Intranet.Entity;
using Intranet.Service;
using Intranet.Api.Security;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using System.Security.Claims;
namespace Intranet.Api.Tests;
public sealed class AnalisisRbTests
{
 private static AnalisisRbService Service(Repo repo,bool authenticated=true){
  var user=authenticated?new ClaimsPrincipal(new ClaimsIdentity([new Claim(IntranetLogin.GestionUserClaim,"OPERARIO")],"Test")):new ClaimsPrincipal(new ClaimsIdentity());
  return new(repo,new GestionIdentity(Options.Create(new GestionOptions{Mode="SqlServer"}),new Env()),new RequestActor(user,false,false,false,true,"test"));
 }
 [Fact] public async Task NormalizesNpAndReturnsRepositoryQuantity(){var repo=new Repo();var result=await Service(repo).Search(" j1907 ",default);Assert.Equal("J1907",repo.Np);Assert.Equal(new AnalisisRbRow("J1907",438),Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<AnalisisRbRow>>(result.Value)));}
 [Fact] public async Task PreservesPriceAndSewingTimePrecision(){var repo=new Repo{Rows=[new("J1825",217,1.4040m,11.51m)]};var result=await Service(repo).Search("J1825",default);var row=Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<AnalisisRbRow>>(result.Value));Assert.Equal(1.4040m,row.CommercialPrice);Assert.Equal(11.51m,row.SewingTime);}
 [Fact] public async Task MissingQuotationRemainsNull(){var repo=new Repo{Rows=[new("Z9999",10)]};var result=await Service(repo).Search("Z9999",default);var row=Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<AnalisisRbRow>>(result.Value));Assert.Null(row.CommercialPrice);}
 [Fact]public void MapsExistingProcedureColumnsWithoutLosingPrice(){var table=new System.Data.DataTable();table.Columns.Add("Np",typeof(string));table.Columns.Add("ProgrammedQuantity",typeof(int));table.Columns.Add("CommercialPrice",typeof(decimal));table.Columns.Add("SewingTime",typeof(decimal));table.Rows.Add("J1825",217,1.404m,11.51m);using var reader=table.CreateDataReader();reader.Read();var row=Dapper.SqlMapper.GetRowParser<AnalisisRbRow>(reader)(reader);Assert.Equal(1.404m,row.CommercialPrice);Assert.Null(row.ClientName);}
 [Fact]public void MapsQuotationHeaderFromProcedure(){var table=new System.Data.DataTable();table.Columns.Add("Np",typeof(string));table.Columns.Add("ProgrammedQuantity",typeof(int));table.Columns.Add("ClientName",typeof(string));table.Columns.Add("LineName",typeof(string));table.Columns.Add("DispatchDate",typeof(DateTime));table.Columns.Add("ProductCategory",typeof(string));table.Columns.Add("FabricDescription",typeof(string));table.Rows.Add("J1825",217,"ROLLER RABBIT","FASHION PREMIUN MODA CHINCHA",new DateTime(2026,11,9),"KIDS","INTERLOCK");using var reader=table.CreateDataReader();reader.Read();var row=Dapper.SqlMapper.GetRowParser<AnalisisRbRow>(reader)(reader);Assert.Equal("ROLLER RABBIT",row.ClientName);Assert.Equal(new DateTime(2026,11,9),row.DispatchDate);Assert.Equal("INTERLOCK",row.FabricDescription);}
 [Theory][InlineData("")][InlineData("J190")][InlineData("J19077")][InlineData("J19%7")][InlineData("';-- ")]public async Task InvalidNpDoesNotReachDatabase(string np){var repo=new Repo();await Assert.ThrowsAsync<ArgumentException>(()=>Service(repo).Search(np,default));Assert.Null(repo.Np);}
 [Fact]public async Task UnknownNpReturnsEmptyResult(){var repo=new Repo{Rows=[]};var result=await Service(repo).Search("Z9999",default);Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<AnalisisRbRow>>(result.Value));}
 [Fact]public async Task RequiresAuthenticatedGestionIdentity(){var repo=new Repo();await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>Service(repo,false).Search("J1907",default));Assert.Null(repo.Np);}
 [Fact]public void UsesLogisticsQueryPermission()=>Assert.Equal(new[]{"orders.query"},PortalAccessFilter.Required("AnalisisRb","Search","GET"));
 [Fact]public async Task OtherFiltersRequireAtLeastOneValue(){await Assert.ThrowsAsync<ArgumentException>(()=>Service(new Repo()).Filter(null,null," ",null,default));}
 [Theory][InlineData(-1,1)][InlineData(1,-1)][InlineData(1.12345,1)][InlineData(1,1.123)]public async Task OtherFiltersRejectInvalidNumbers(decimal price,decimal time){await Assert.ThrowsAsync<ArgumentException>(()=>Service(new Repo()).Filter(price,time,null,null,default));}
 [Fact]public async Task OtherFiltersAllowZeroAndCombinedValues(){var result=await Service(new Repo()).Filter(0,0," TYPE ",null,default);Assert.Single(Assert.IsType<AnalisisRbFilterPage>(result.Value).Rows);}
 [Fact]public async Task OtherFiltersRejectInvalidCursor(){await Assert.ThrowsAsync<ArgumentException>(()=>Service(new Repo()).Filter(1,null,null,"';--",default));}
 [Fact]public async Task OtherFiltersAndTypesRequireAuthentication(){await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>Service(new Repo(),false).Filter(1,null,null,null,default));await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>Service(new Repo(),false).Types(default));}
 private sealed class Repo:IAnalisisRbRepository{public Task<AnalisisRbFilterPage> Filter(decimal? price,decimal? time,string? type,string? cursor,CancellationToken ct)=>Task.FromResult(new AnalisisRbFilterPage(Rows,false,null));public Task<IReadOnlyList<AnalisisRbType>> Types(CancellationToken ct)=>Task.FromResult<IReadOnlyList<AnalisisRbType>>([]);public string? Np;public IReadOnlyList<AnalisisRbRow> Rows=[new("J1907",438)];public Task<IReadOnlyList<AnalisisRbRow>> Search(string np,CancellationToken ct){Np=np;return Task.FromResult(Rows);}}
 private sealed class Env:IHostEnvironment{public string ApplicationName{get;set;}="Test";public string EnvironmentName{get;set;}="Production";public string ContentRootPath{get;set;}="";public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();}
}
