using System.ComponentModel.DataAnnotations;
namespace Intranet.Entity;

public sealed class ImportControlFilter
{
 [RegularExpression("^[0-9ABCD]$")] public string Mode {get;set;}="1";
 [StringLength(10)] public string Number {get;set;}="";
 [StringLength(1)] public string AnnexType {get;set;}="P";
 [StringLength(4)] public string Annex {get;set;}="";
 [StringLength(1)] public string Status {get;set;}="";
 [StringLength(25)] public string Guide {get;set;}="";
 [StringLength(3)] public string Origin {get;set;}="";
 [StringLength(20)] public string Item {get;set;}="";
 [StringLength(3)] public string Country {get;set;}="";
 [StringLength(10)] public string Associated {get;set;}="";
 [StringLength(18)] public string Policy {get;set;}="";
 public int Year {get;set;}=DateTime.Today.Year;
 public int Month {get;set;}=DateTime.Today.Month;
 [StringLength(3)] public string DocumentType {get;set;}="FI";
 [StringLength(4)] public string DocumentSeries {get;set;}="";
 [StringLength(20)] public string DocumentNumber {get;set;}="";
 [StringLength(3)] public string ItemType {get;set;}="";
 [StringLength(3)] public string Factory {get;set;}="";
 [StringLength(5)] public string ProductionOrder {get;set;}="";
 public DateTime? From {get;set;}
 public DateTime? To {get;set;}
 [Range(1,100000)] public int Page {get;set;}=1;
}
public sealed class ImportControlValues
{
 [StringLength(10),RegularExpression("^[A-Za-z0-9-]*$")] public string Number {get;set;}="";
 [Required,StringLength(1)] public string AnnexType {get;set;}="P";
 [Required,StringLength(4)] public string Annex {get;set;}="";
 public DateTime RegisteredAt {get;set;}=DateTime.Today;
 [Required,StringLength(3)] public string ItemType {get;set;}="";
 [Required,StringLength(3)] public string Currency {get;set;}="";
 [Required,StringLength(3)] public string Origin {get;set;}="";
 [StringLength(3)] public string Shipment {get;set;}="";
 [StringLength(3)] public string Country {get;set;}="";
 [Required,StringLength(2)] public string Area {get;set;}="05";
 [StringLength(25)] public string Guide {get;set;}="";
 public DateTime? GuideDate {get;set;}
 public DateTime? PaymentDate {get;set;}
 public DateTime? DueDate {get;set;}
 [StringLength(200)] public string Observations {get;set;}="";
 public bool Partial {get;set;}
 [StringLength(10)] public string Principal {get;set;}="";
}
public sealed record ImportControlCatalog(string Kind,string Code,string Description);
public sealed record ImportControlCatalogs(IReadOnlyList<ImportControlCatalog> Options,bool AutomaticNumbering);
public sealed record ImportControlDocuments(string Proforma,string Confirmation,string Orders,string Invoice,string Policy);
public sealed record ImportControlConcept(string Code,string Description,decimal Dollars,decimal Soles,decimal ExchangeRate,decimal Other,string DocumentNumber);
public sealed record ImportControlCondition(string Sequence,string Description);
public sealed record ImportControlDetail(string Number,string Status,string AnnexName,ImportControlValues Values,string Version,bool CanEdit,bool CanDelete,
 ImportControlDocuments? Documents=null,IReadOnlyList<ImportControlConcept>? Concepts=null,IReadOnlyList<ImportControlCondition>? Conditions=null,IReadOnlyList<ImportControlDocument>? DocumentEntries=null,ImportControlOrder? Order=null);
public sealed class ImportControlSave
{
 [Required] public ImportControlValues Values {get;set;}=new();
 [StringLength(64)] public string Version {get;set;}="";
 public List<ImportControlConcept>? Concepts {get;set;}
 public List<ImportControlCondition>? Conditions {get;set;}
 public List<ImportControlDocument>? DocumentEntries {get;set;}
 public ImportControlOrder? Order {get;set;}
 public ImportControlPolicy? Policy {get;set;}
}
public sealed class ImportControlDelete
{
 [Required,RegularExpression("^[A-F0-9]{64}$")] public string Version {get;set;}="";
}
public sealed class ImportControlPendingException():Exception("Falta registrar los scripts PA_Control_Importaciones.sql y PA_Control_Importaciones_Registro.sql y PA_Control_Importaciones_Busqueda.sql en Gestión.");
