using System.ComponentModel.DataAnnotations;
namespace Intranet.Entity;
public sealed class QualityEventFilter {
 [Range(1,4)] public int Mode {get;set;}=1;
 public DateTime From {get;set;}=DateTime.Today; public DateTime To {get;set;}=DateTime.Today;
 [DisplayFormat(ConvertEmptyStringToNull=false),RegularExpression("^[A-Za-z0-9]{0,5}$")] public string Batch {get;set;}="";
 [DisplayFormat(ConvertEmptyStringToNull=false),StringLength(3)] public string EventCode {get;set;}="";
}
public sealed class QualityEventItem { [Range(1,int.MaxValue)] public int Sequence {get;set;} [Range(0,99999999999.99)] public decimal Kilos {get;set;} }
public sealed class QualityEventUpdate {
 [RegularExpression("^[IUD]$")] public string Action {get;set;}="I";
 [Required,RegularExpression("^[A-Za-z0-9]{1,5}$")] public string Batch {get;set;}="";
 [Required,StringLength(3)] public string EventCode {get;set;}="";
 [Range(0,int.MaxValue)] public int EventSequence {get;set;}
 public DateTime Date {get;set;}=DateTime.Today;
 [Required,StringLength(1)] public string Status {get;set;}="A";
 [Required,StringLength(6)] public string Auditor {get;set;}="";
 [StringLength(100)] public string Observations {get;set;}="";
 [StringLength(5)] public string Reason {get;set;}="";
 [StringLength(1)] public string ExternalResult {get;set;}="";
 [Required,StringLength(3)] public string Location {get;set;}="";
 [StringLength(100)] public string ProductionObservations {get;set;}="";
 [StringLength(4)] public string Deviation {get;set;}="";
 [StringLength(4)] public string Cause {get;set;}="";
 public bool ForcedError {get;set;} public bool Dispatched {get;set;} public bool ConfirmApproved {get;set;}
 [Required,MinLength(1),MaxLength(500)] public QualityEventItem[] Items {get;set;}=[];
 public string? ExpectedVersion {get;set;}
}
public sealed class QualityEventConflict(string message):Exception(message);

public sealed record QualityEventCustomer(string Code,string Abbreviation,string Name);
public sealed record QualityEventBatch(QualityEventCustomer? Customer,IReadOnlyList<Dictionary<string,object?>> Items);
