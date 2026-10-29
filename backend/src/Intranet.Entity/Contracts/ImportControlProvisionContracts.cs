using System.ComponentModel.DataAnnotations;
namespace Intranet.Entity;
public sealed record ImportControlProvision([param:Required,RegularExpression("^[A-F0-9]{64}$")]string Version,[param:Required,RegularExpression("^(provision|reverse)$")]string Action);
public sealed record ImportControlProvisionResult(string Number,string Action,string Status);
public static class ImportControlProvisioning {
 public static string ActionForStatus(string status)=>status.Trim() switch{"R"=>"provision","P"=>"reverse",_=>throw new ArgumentException("El estado de la importación no permite provisionar ni revertir la provisión.")};
 public static void ValidateRequest(ImportControlProvision request){if(request is null||!System.Text.RegularExpressions.Regex.IsMatch(request.Version??"","^[A-F0-9]{64}$")||request.Action is not ("provision" or "reverse"))throw new ArgumentException("Vuelve a consultar la importación y confirma la operación.");}
 public static void ValidateSnapshot(string status,string version,ImportControlProvision request){ValidateRequest(request);if(version!=request.Version)throw new RegistrationConflictException("La importación cambió. Vuelve a consultarla antes de confirmar la provisión.");if(ActionForStatus(status)!=request.Action)throw new RegistrationConflictException("El estado de la importación cambió. Vuelve a consultar y confirmar la operación.");}
}
