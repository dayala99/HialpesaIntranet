using Intranet.Entity;
namespace Intranet.Data;
// Contrato de almacenamiento SQL; los nombres publicos de la API no cambian.
public sealed class AnalisisRbHistorialRegistro {
 public long Num_Registro {get;set;}
 public Guid Cod_Grupo_Historial {get;set;}
 public int Num_Version {get;set;}
 public long? Num_Registro_Anterior {get;set;}
 public string Tip_Accion {get;set;}="";
 public string Cod_Fabrica {get;set;}="";
 public string Cod_OrdPro {get;set;}="";
 public int Num_Prendas_Programadas {get;set;}
 public decimal Imp_Precio_Comercial {get;set;}
 public decimal Min_Costura {get;set;}
 public decimal Min_Considerados {get;set;}
 public decimal Imp_Precio_Servicio {get;set;}
 public decimal Imp_Otros_Gastos {get;set;}
 public string? Des_Ruta_Imagen {get;set;}
 public string? Txt_Datos_Analisis {get;set;}
 public string Cod_Version_Reglas {get;set;}="";
 public string Cod_Usuario {get;set;}="";
 public DateTime Fec_Registro_UTC {get;set;}
 public string Cod_Traza {get;set;}="";
 public AnalisisRbHistory ToContract()=>new(){Id=Num_Registro,RootId=Cod_Grupo_Historial,Revision=Num_Version,PreviousId=Num_Registro_Anterior,Action=Tip_Accion,Factory=Cod_Fabrica,Np=Cod_OrdPro,ProgrammedQuantity=Num_Prendas_Programadas,CommercialPrice=Imp_Precio_Comercial,SewingTime=Min_Costura,UsedTime=Min_Considerados,ServicePrice=Imp_Precio_Servicio,OtherExpenses=Imp_Otros_Gastos,ImagePath=Des_Ruta_Imagen,SnapshotJson=Txt_Datos_Analisis,RuleVersion=Cod_Version_Reglas,GestionUser=Cod_Usuario,SavedAtUtc=DateTime.SpecifyKind(Fec_Registro_UTC,DateTimeKind.Utc),TraceId=Cod_Traza};
}
