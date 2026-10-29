using Intranet.Entity;
using System.Text.RegularExpressions;
namespace Intranet.Service;
public interface IAnalisisRbOperationsService {Task<ServiceResult> Read(string np,string? variant,CancellationToken ct,string? sector=null);}
public sealed class AnalisisRbOperationsService(IAnalisisRbOperationsRepository repository,GestionIdentity identity,RequestActor actor):ApplicationService(actor),IAnalisisRbOperationsService {
 public async Task<ServiceResult> Read(string np,string? variant,CancellationToken ct,string? sector=null){
  identity.GetUser(User);var value=(np??"").Trim().ToUpperInvariant();var selected=string.IsNullOrWhiteSpace(variant)?null:variant.Trim().ToUpperInvariant();var requestedSector=string.IsNullOrWhiteSpace(sector)?null:sector.Trim().ToUpperInvariant();
  if(!Regex.IsMatch(value,"^[A-Z0-9]{5}$")||(selected is not null&&!Regex.IsMatch(selected,"^[A-Z0-9]$"))||(requestedSector is not null&&!Regex.IsMatch(requestedSector,"^[A-Z0-9]{1,10}$")))throw new ArgumentException("Revisa la NP, la variante y el sector del tarifado.");
  var result=await repository.Read(value,selected,ct);if(result is null)return new(ResultKind.NotFound,new BusinessProblem{Title="No se encontró la NP."});
  var sectors=result.Rows.GroupBy(row=>row.Sector).Select(group=>new AnalisisRbSector(group.Key,string.IsNullOrWhiteSpace(group.First().SectorName)?group.Key:group.First().SectorName)).ToList();
  var selectedSector=requestedSector??(sectors.Any(item=>item.Code=="CS")?"CS":sectors.FirstOrDefault()?.Code??"CS");
  if(requestedSector is not null&&!sectors.Any(item=>item.Code==requestedSector))throw new ArgumentException("El sector no tiene operaciones en la variante seleccionada.");
  return Ok(result with{Rows=result.Rows.Where(row=>row.Sector==selectedSector).ToList(),Sectors=sectors,SelectedSector=selectedSector});
 }
}
