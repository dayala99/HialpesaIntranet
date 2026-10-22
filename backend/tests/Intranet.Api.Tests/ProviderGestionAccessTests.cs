using Intranet.Entity;
using Intranet.Service;
using Intranet.Data;
using System.Security.Claims;
using Dapper;
using Intranet.Api.Controllers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
namespace Intranet.Api.Tests;
public sealed class ProviderGestionAccessTests
{
 [Fact] public async Task OrdinaryGestionUserCanPrepareAuthorizedPoWithoutAdministratorRole()
 {
  var path=System.Environment.GetEnvironmentVariable("INTRANET_TEST_SETTINGS");if(string.IsNullOrEmpty(path))return;
  var configuration=new ConfigurationBuilder().AddJsonFile(path).Build();
  var options=Options.Create(configuration.GetSection("Gestion").Get<GestionOptions>()!);
  var factory=new GestionConnectionFactory(configuration);
  await using var connection=await factory.OpenAsync(default);
  var code=await connection.QueryFirstOrDefaultAsync<string>("""
   SELECT TOP (1) RTRIM(s.Cod_Usuario)
   FROM dbo.LG_SEGORDCOMP s JOIN SEGURIDAD.dbo.SEG_Usuarios u ON u.Cod_Usuario=s.Cod_Usuario
   WHERE s.Cod_ClaOrdComp='01' AND u.Cod_Usuario<>'programador04' AND ISNULL(u.is_active,1)=1
   ORDER BY s.Cod_Usuario
   """);
  Assert.False(string.IsNullOrWhiteSpace(code));
  var principal=new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.Name,"gestion:"+code),new Claim(IntranetLogin.GestionUserClaim,code!)},CookieAuthenticationDefaults.AuthenticationScheme));
  var environment=new TestEnvironment();var identity=new GestionIdentity(options,environment);
  Assert.Null(identity.GetReadAllWindowsUser(principal));
  var repository=new AviosRepository(factory,options);var tickets=new ProviderAgentTickets();
  var controller=new ProviderController(new ProviderService(identity,environment,repository,new RegistrationReadRepository(factory,options),configuration,tickets,new RequestActor(principal,false,true,false,false,"test"))){ControllerContext=new(){HttpContext=new DefaultHttpContext{User=principal}}};
  controller.Request.Headers["X-Intranet-Provider"]="1";
  var order=await repository.FindOrder("002","350000",code!,default);Assert.NotNull(order);
  var report=await new RegistrationReadRepository(factory,options).Print(order!,[],default);
  var po=report.ReportRows.SelectMany(r=>System.Text.RegularExpressions.Regex.Matches(r.Combination,@"\bPO\s*-\s*[0-9]+\b").Select(m=>System.Text.RegularExpressions.Regex.Replace(m.Value,@"\s+",""))).First();
  var result=await controller.PrepareLaunch(new(){Series="002",Number="350000",Po=po},default);
  Assert.IsType<OkObjectResult>(result);
 }
 private sealed class TestEnvironment:IWebHostEnvironment
 {
  public string ApplicationName{get;set;}="Test";public string EnvironmentName{get;set;}="Production";public string ContentRootPath{get;set;}="";public string WebRootPath{get;set;}="";public IFileProvider ContentRootFileProvider{get;set;}=new NullFileProvider();public IFileProvider WebRootFileProvider{get;set;}=new NullFileProvider();
 }
}
