namespace Intranet.Entity;
public sealed record AnalisisRbRow
{
 public string Np {get;init;}="";
 public int ProgrammedQuantity {get;init;}
 public decimal? CommercialPrice {get;init;}
 public decimal? SewingTime {get;init;}
 public string? ImagePath {get;init;}
 public string? ClientName {get;init;}
 public string? LineName {get;init;}
 public string? GarmentType {get;init;}
 public string? ClientStyle {get;init;}
 public string? DivisionName {get;init;}
 public int? RequiredQuantity {get;init;}
 public string? ColorName {get;init;}
 public DateTime? SewingStartDate {get;init;}
 public DateTime? SewingEndDate {get;init;}
 public DateTime? DispatchDate {get;init;}
 public string? ProductCategory {get;init;}
 public string? FabricDescription {get;init;}
 public AnalisisRbRow(){}
 public AnalisisRbRow(string np,int programmedQuantity,decimal? commercialPrice=null,decimal? sewingTime=null){Np=np;ProgrammedQuantity=programmedQuantity;CommercialPrice=commercialPrice;SewingTime=sewingTime;}
}
