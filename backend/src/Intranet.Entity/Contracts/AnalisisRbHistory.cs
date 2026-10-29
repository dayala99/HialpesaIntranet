namespace Intranet.Entity;
public sealed record AnalisisRbSelectedOperation(string BlockCode,string BlockName,string Sequence,string Code,string Variant,string Description,decimal StandardTime);
public sealed record AnalisisRbPartialSelection(string Tariff,string Variant,string Sector,string SectorName,decimal SectorTotalTime,IReadOnlyList<AnalisisRbSelectedOperation> Operations){public decimal SelectedTime=>Operations.Sum(row=>row.StandardTime);}
public sealed record AnalisisRbRequotation(bool Enabled,decimal? BaseServicePrice,decimal? Percentage);
public sealed record AnalisisRbSectorAnalysis(string Tariff,string Variant,string Sector,string SectorName,decimal TotalTime,int TotalOperations,int SelectedOperations,bool IsComplete);
public sealed class AnalisisRbSave
{
 public AnalisisRbPartialSelection? PartialSelection {get;set;}
 public AnalisisRbRequotation? Requotation {get;set;}
 public AnalisisRbSectorAnalysis? SectorAnalysis {get;set;}
 public Guid RequestId {get;set;}
 public AnalisisRbRow Source {get;set;}=new();
 public decimal UsedTime {get;set;}
 public decimal ServicePrice {get;set;}
 public decimal OtherExpenses {get;set;}
}
public sealed record AnalisisRbCalculated(decimal? BaseVm,decimal CommercialTotal,decimal ServiceVm,decimal Difference,decimal? Profitability,decimal RemovedTime,string TimeLabel,string Observation,decimal ServiceWithExpenses);
public sealed record AnalisisRbSnapshot(AnalisisRbRow Source,decimal UsedTime,decimal ServicePrice,decimal OtherExpenses,AnalisisRbCalculated Calculated,DateTime AnalysisDate,string RuleVersion){public AnalisisRbPartialSelection? PartialSelection{get;init;}public AnalisisRbRequotation? Requotation{get;init;}public AnalisisRbSectorAnalysis? SectorAnalysis{get;init;}}
public sealed class AnalisisRbHistory
{
 public long Id {get;set;}
 public Guid RootId {get;set;}
 public int Revision {get;set;}
 public long? PreviousId {get;set;}
 public string Action {get;set;}="";
 public string Factory {get;set;}="";
 public string Np {get;set;}="";
 public int ProgrammedQuantity {get;set;}
 public decimal CommercialPrice {get;set;}
 public decimal SewingTime {get;set;}
 public decimal UsedTime {get;set;}
 public decimal ServicePrice {get;set;}
 public decimal OtherExpenses {get;set;}
 public string RuleVersion {get;set;}="";
 public string GestionUser {get;set;}="";
 public DateTime SavedAtUtc {get;set;}
 public string TraceId {get;set;}="";
 public string? ImagePath {get;set;}
 public string? SnapshotJson {get;set;}
}
public sealed record AnalisisRbHistoryPage(IReadOnlyList<AnalisisRbHistory> Rows,int TotalRows,int Page,int TotalPages);
public interface IAnalisisRbHistoryRepository
{
 Task<AnalisisRbHistory> Insert(Guid requestId,AnalisisRbSnapshot snapshot,string user,string traceId,CancellationToken ct);
 Task<AnalisisRbHistoryPage> Read(string np,long? id,int page,CancellationToken ct);
}
