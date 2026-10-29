using System.ComponentModel.DataAnnotations;
namespace Intranet.Entity;
public sealed record ImportControlDocument {
 public string Kind {get;set;}="PR";
 [StringLength(12)] public string Correlative {get;set;}="";
 [Required,StringLength(2)] public string Type {get;set;}="PR";
 [Required,StringLength(4)] public string Series {get;set;}="";
 [Required,StringLength(15)] public string Number {get;set;}="";
 [Required,StringLength(1)] public string AnnexType {get;set;}="P";
 [Required,StringLength(4)] public string Annex {get;set;}="";
 public string AnnexName {get;set;}="";public string Ruc {get;set;}="";public string Sunat {get;set;}="";
 public DateTime IssuedAt {get;set;}=DateTime.Today;public DateTime RegisteredAt {get;set;}=DateTime.Today;public DateTime? DueDate {get;set;}
 [Required,StringLength(3)] public string ItemType {get;set;}="";
 [Required,StringLength(3)] public string Currency {get;set;}="USD";
 public decimal ExchangeRate {get;set;}public decimal Equivalence {get;set;}
 [Required,StringLength(100)] public string Description {get;set;}="";
 public decimal Soles {get;set;}public decimal Dollars {get;set;}public decimal Other {get;set;}
 public decimal TotalSoles {get;set;}public decimal TotalDollars {get;set;}public decimal TotalOther {get;set;}
}
public sealed record ImportControlOrder([param:Required,StringLength(1)] string Warehouse,[param:Required,StringLength(3)] string Series,[param:Required,StringLength(6)] string Number);
public sealed record ImportControlPolicy([param:Required,StringLength(12)] string Correlative);
public sealed record ImportControlShipment(string Code,string Description,string Active,string Version);
public sealed record ImportControlShipmentSave([param:RegularExpression("^[IUD]$")]string Action,[param:Required,StringLength(3)]string Code,[param:Required,StringLength(30)]string Description,string Version);
