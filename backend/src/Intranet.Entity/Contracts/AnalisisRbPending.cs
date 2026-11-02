namespace Intranet.Entity;
public sealed record AnalisisRbPendingIndicators(bool PurchaseOrderReady,bool TariffReady,bool TechnicalSheetReady,string PurchaseOrderStatus,string TariffStatus,string TechnicalSheetStatus);
public sealed record AnalisisRbPendingResult(IReadOnlyList<AnalisisRbRow> Rows,int ReportRows,int DetailRows,DateTime GeneratedAtUtc){public IReadOnlyDictionary<string,AnalisisRbPendingIndicators> Statuses{get;init;}=new Dictionary<string,AnalisisRbPendingIndicators>();}
public sealed record AnalisisRbPendingStatus(string Status,AnalisisRbPendingResult? Result,string? Error);
public interface IAnalisisRbPendingRepository {Task<AnalisisRbPendingResult> Read(CancellationToken ct);}
