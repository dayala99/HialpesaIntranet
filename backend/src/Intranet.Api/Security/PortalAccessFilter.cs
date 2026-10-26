using Intranet.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Authorization;
namespace Intranet.Api.Security;
public sealed class PortalAccessFilter(IPortalAccessService access):IAsyncAuthorizationFilter {
 public async Task OnAuthorizationAsync(AuthorizationFilterContext context){
 if(context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any()||context.HttpContext.User.Identity?.IsAuthenticated!=true)return;
 if(context.ActionDescriptor is not ControllerActionDescriptor action)return;
 var required=Required(action.ControllerName,action.ActionName,context.HttpContext.Request.Method);if(required.Length==0)return;
 var permissions=await access.Read(context.HttpContext.User,context.HttpContext.RequestAborted);
 if(!permissions.IsGlobalUser&&!required.Any(permissions.AllowedOptions.Contains))context.Result=new ObjectResult(new ProblemDetails{Status=403,Title="Tu perfil de Gestión no tiene acceso a esta opción."}){StatusCode=403};
 }
 public static string[] Required(string controller,string action,string method)=>controller switch{
 "Dyeing" or "QualityControl"=>["dyeing.batches"],
 "QualityEvents"=>["quality.events"],"UnfinishedQualityEvents"=>["quality.unfinished"],
 "Imports" or "ImportControl"=>["imports"],"Avios" or "Provider"=>["orders.query","orders.create"],
 "OrderRegistration" when method=="GET"=>["orders.query","orders.create"],
 "OrderRegistration"=>["orders.create"],_=>[]};
}
