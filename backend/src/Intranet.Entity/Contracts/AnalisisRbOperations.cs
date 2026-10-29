namespace Intranet.Entity;
public sealed record AnalisisRbSector(string Code,string Name);
public sealed record AnalisisRbVariant(string Code,string Description,string Status);
public sealed record AnalisisRbOperation(string Sector,string BlockCode,string BlockName,string Sequence,string Code,string Variant,string Description,string MachineFamily,string MachineGroup,string MachineGroupName,string Category,string FamilyType,decimal StandardTime){public string SectorName{get;init;}="";}
public sealed record AnalisisRbOperations(string Np,string Tariff,string StyleName,string AssignedVariant,string SelectedVariant,IReadOnlyList<AnalisisRbVariant> Variants,IReadOnlyList<AnalisisRbOperation> Rows){public IReadOnlyList<AnalisisRbSector> Sectors{get;init;}=[];public string SelectedSector{get;init;}="CS";public decimal TotalTime=>Rows.Sum(row=>row.StandardTime);public decimal MachineTime=>Rows.Where(row=>row.FamilyType!="M").Sum(row=>row.StandardTime);}
public interface IAnalisisRbOperationsRepository {Task<AnalisisRbOperations?> Read(string np,string? variant,CancellationToken ct);}
