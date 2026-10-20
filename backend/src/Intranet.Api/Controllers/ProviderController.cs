using System.Security.Cryptography;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Diagnostics;
using System.Net;
using Intranet.Api.Infrastructure;
using Microsoft.AspNetCore.Mvc;
namespace Intranet.Api.Controllers;
[ApiController,Route("api/v1/proveedor")]
public sealed class ProviderController(GestionIdentity identity,IWebHostEnvironment environment,AviosRepository orders,RegistrationReadRepository reads):ControllerBase
{
 public sealed class ColorConfirmation {
  [Required,MaxLength(80)]public string Style{get;set;}="";
  [Required,MaxLength(80)]public string ColorCode{get;set;}="";
  [Required,MaxLength(300)]public string ProviderColor{get;set;}="";
  [Required,MaxLength(600)]public string GestionColor{get;set;}="";
 }
 public sealed class Selection {
   [Required,RegularExpression("^[0-9]{3}$")] public string Series {get;set;}="";
   [Required,RegularExpression("^[0-9]{6}$")] public string Number {get;set;}="";
   [Required,RegularExpression("^PO-[0-9]{1,20}$")] public string Po {get;set;}="";
   [MaxLength(30)]public List<ColorConfirmation> ColorConfirmations{get;set;}=[];
 }
 private async Task<string[]> PurchaseOrders(string series,string number,CancellationToken ct){
   var order=await orders.FindOrder(series,number,identity.GetUser(User),ct,identity.GetReadAllWindowsUser(User));
   if(order is not {Class:"01"})throw new UnauthorizedAccessException();
   var report=await reads.Print(order,Array.Empty<Intranet.Api.Contracts.OrderItem>(),ct);
   return report.ReportRows.SelectMany(row=>Regex.Matches(row.Combination,@"\bPO\s*-\s*[0-9]+\b",RegexOptions.IgnoreCase).Select(m=>Regex.Replace(m.Value,@"\s+","").ToUpperInvariant())).Distinct().Order().ToArray();
 }
 [HttpGet("ordenes/{series}/{number}/pos")]
 public async Task<IActionResult> Pos([RegularExpression("^[0-9]{3}$")]string series,[RegularExpression("^[0-9]{6}$")]string number,CancellationToken ct)=>Ok(await PurchaseOrders(series,number,ct));
 private sealed record ProviderSecret(string Username,string Password,string LoginUrl);
 private IActionResult? Confirmation(string? response){
  try{
   using var json=JsonDocument.Parse(response??"{}");
   if(json.RootElement.TryGetProperty("status",out var status)&&status.GetString()=="confirmation-required" && json.RootElement.TryGetProperty("confirmations",out var values) && values.ValueKind==JsonValueKind.Array)
    return Ok(new{status="confirmation-required",message="Confirma la similitud de colores. No se cambiaron cantidades.",confirmations=values.Clone()});
  }catch(JsonException){}
  return null;
 }
 private static string Failure(string? response){
   string? stage=null;string? detail=null;
   try{using var json=JsonDocument.Parse(response??"{}");if(json.RootElement.TryGetProperty("stage",out var value))stage=value.GetString();if(json.RootElement.TryGetProperty("detail",out var mismatch)&&mismatch.ValueKind==JsonValueKind.String)detail=mismatch.GetString();}catch(JsonException){}
   if(stage=="quantity-matching" && !string.IsNullOrWhiteSpace(detail))return "No se cambiaron cantidades. "+detail[..Math.Min(detail.Length,2000)];
   return stage switch {
     "login-rejected"=>"El portal mantiene el formulario de acceso. Revisa si muestra un rechazo o una verificacion adicional; no se repetira el intento automaticamente.",
     "login"=>"El portal no confirmó el inicio de sesión. El acceso está guardado; revisa si solicita verificación adicional.",
     "product-ordering" or "vince"=>"La sesión inició, pero no se pudo abrir PRODUCT ORDERING > VINCE.",
     "session"=>"No se pudo preparar la pantalla de búsqueda del proveedor. La pestaña anterior se conserva.",
     "quantity-matching"=>"No se completaron cantidades: existen filas sin coincidencia única por PO, artículo, estilo, color y talla.",
     "quantity-fill"=>"El llenado de cantidades no terminó. Revisa los campos del proveedor antes de continuar; no se guardó ni envió el pedido.",
     "po-field"=>"No se pudo identificar o completar PO Number en el portal.",
     "search"=>"No se pudo ejecutar Search en el proveedor.",
     "saved-result"=>"No se encontró un resultado Saved para el PO seleccionado o la búsqueda no terminó a tiempo.",
     "opening-saved" or "loading-saved"=>"El resultado Saved se seleccionó, pero su pantalla no terminó de cargar. Revisa el navegador del proveedor.",
     _=>"No se pudo completar la automatización del proveedor. Revisa el navegador y vuelve a intentarlo."
   };
 }
 private static readonly SemaphoreSlim Gate=new(1,1);
 private static Process? browserProcess;
 [HttpPost("iniciar-sesion")]
 public async Task<IActionResult> Login([FromBody]Selection selection,CancellationToken ct)
 {
   if(!environment.IsDevelopment() || HttpContext.Connection.RemoteIpAddress is not {} ip || !IPAddress.IsLoopback(ip) || identity.GetReadAllWindowsUser(User) is null || Request.Headers["X-Intranet-Provider"]!="1")throw new UnauthorizedAccessException();
   identity.GetUser(User);
   var selectedOrder=await orders.FindOrder(selection.Series,selection.Number,identity.GetUser(User),ct,identity.GetReadAllWindowsUser(User));
   if(selectedOrder is not {Class:"01"})throw new UnauthorizedAccessException();
   var report=await reads.Print(selectedOrder,Array.Empty<Intranet.Api.Contracts.OrderItem>(),ct);
   var payload=JsonSerializer.Serialize(new{po=selection.Po,rows=report.ReportRows,colorConfirmations=selection.ColorConfirmations},new JsonSerializerOptions(JsonSerializerDefaults.Web));
   if(!report.ReportRows.Any(row=>Regex.Matches(row.Combination,@"\bPO\s*-\s*[0-9]+\b",RegexOptions.IgnoreCase).Any(m=>Regex.Replace(m.Value,@"\s+","").Equals(selection.Po,StringComparison.OrdinalIgnoreCase))))throw new ArgumentException("El PO seleccionado no pertenece a la OC.");
   await Gate.WaitAsync(ct);
   try {
     if(browserProcess is {HasExited:false}){
       string? reply;
       try{
        await browserProcess.StandardInput.WriteLineAsync(payload);
        await browserProcess.StandardInput.FlushAsync();
        reply=await browserProcess.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(180),ct);
       }catch{
        if(!browserProcess.HasExited)browserProcess.Kill(true);
        browserProcess=null;
        throw new ArgumentException("Se interrumpio la comunicacion con el navegador. Vuelve a intentarlo desde la intranet.");
       }
       if(Confirmation(reply) is {} pending)return pending;
       if(reply?.Contains("po-filled")==true)return Ok(new{status="po-filled",message="Cantidades de RFID y UPC Stickers completadas para "+selection.Po+". No se ha pulsado Save Order ni se ha enviado el pedido."});
       throw new ArgumentException(Failure(reply));
     }
     if(!OperatingSystem.IsWindows())throw new ArgumentException("El acceso local requiere Windows.");
     var secretPath=Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),"Hialpesa","Intranet","avery.protected");
     if(!System.IO.File.Exists(secretPath))return BadRequest(new{title="Configura el acceso cifrado del proveedor para este usuario Windows."});
     var encrypted=await System.IO.File.ReadAllBytesAsync(secretPath,ct);
     var clear=ProtectedData.Unprotect(encrypted,null,DataProtectionScope.CurrentUser);
     ProviderSecret credentials;
     try{credentials=JsonSerializer.Deserialize<ProviderSecret>(clear)??throw new InvalidOperationException("Configuración de proveedor no válida.");}finally{CryptographicOperations.ZeroMemory(clear);}
     var username=credentials.Username;var password=credentials.Password;
     var loginUrl=new Uri(credentials.LoginUrl);
     if(loginUrl.Scheme!="https"||loginUrl.Host!="webservices.averydennison.com"||loginUrl.AbsolutePath!="/specstar/login.aspx")throw new ArgumentException("La URL configurada no es el acceso autorizado del proveedor.");
     var root=Path.GetFullPath(Path.Combine(environment.ContentRootPath,"../../.."));
     var start=new ProcessStartInfo("node"){WorkingDirectory=Path.Combine(root,"frontend"),UseShellExecute=false,RedirectStandardInput=true,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true};
     start.ArgumentList.Add(Path.Combine(root,"frontend","tools","provider-login.cjs"));
     start.Environment["AVERY_LOGIN_URL"]=loginUrl.AbsoluteUri;
     start.Environment["AVERY_USERNAME"]=username;start.Environment["AVERY_PASSWORD"]=password;
     var process=Process.Start(start)??throw new InvalidOperationException("No se pudo iniciar el navegador.");
     browserProcess=process;
     await process.StandardInput.WriteLineAsync(payload);
     await process.StandardInput.FlushAsync();
     _=process.StandardError.ReadToEndAsync();
     string? result;
     try{result=await process.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(180),ct);}
     catch{if(!process.HasExited)process.Kill(true);browserProcess=null;throw new ArgumentException("La automatizacion excedio el tiempo de espera o se interrumpio. Vuelve a intentarlo desde la intranet.");}
     if(Confirmation(result) is {} pendingInitial)return pendingInitial;
     if(result?.Contains("signed-in")!=true){if(process.HasExited)browserProcess=null;return BadRequest(new{title=Failure(result)});}
     return Ok(new{status="signed-in",message="VINCE: cantidades de RFID y UPC Stickers completadas para "+selection.Po+". No se ha pulsado Save Order ni se ha enviado el pedido."});
   }finally{Gate.Release();}
 }
}
