/* Complemento del registro de Control de Importaciones.
   Registrar manualmente. SQL Server 2008 R2 / compatibilidad 80.
   No reemplaza procedimientos existentes de Gestión ni los cinco del script inicial. */
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE dbo.PA_LG_IMPORTACIONES_S0006 @Number varchar(10)=''
AS
BEGIN
 SET NOCOUNT ON;
 SELECT ISNULL(dbo.SM_OBTIENE_PROFORMAIMPORTACION(i.Num_Importacion),'') Proforma,
  ISNULL(dbo.SM_OBTIENE_CONFIRMACIONPEDIDOIMPORTACION(i.Num_Importacion),'') Confirmation,
  ISNULL(dbo.SM_OBTIENE_ORDENDECOMPRAIMPORTACION(i.Num_Importacion),'') Orders,
  ISNULL(dbo.SM_OBTIENE_FACTURACOMERCIALIMPORTACION(i.Num_Importacion),'') Invoice,
  ISNULL(dbo.SM_OBTIENE_POLIZAIMPORTACION(i.Num_Importacion),'') Policy
 FROM dbo.LG_IMPORTACIONES i WHERE @Number<>'' AND i.Num_Importacion=@Number;
 SELECT RTRIM(c.Cod_Concepto) Code,ISNULL(RTRIM(d.Des_Concepto),'') Description,
  c.Dol_Importe Dollars,c.Sol_Importe Soles,c.Tipo_Cambio ExchangeRate,ISNULL(c.Otros_Importe,0) Other,
  ISNULL(RTRIM(c.Num_Docum),'') DocumentNumber
 FROM dbo.LG_IMPORTACIONES_CONCEPTOS c LEFT JOIN dbo.CN_ConceptosImportaciones d ON d.Cod_Concepto=c.Cod_Concepto
 WHERE @Number<>'' AND c.Num_Importacion=@Number ORDER BY c.Cod_Concepto;
 SELECT RTRIM(Secuencia) Sequence,RTRIM(Des_Condicion) Description FROM dbo.LG_IMPORTACIONES_CONDICIONES WHERE @Number<>'' AND Num_Importacion=@Number ORDER BY Secuencia;
 SELECT 'concepts' Kind,RTRIM(Cod_Concepto) Code,RTRIM(Des_Concepto) Description FROM dbo.CN_ConceptosImportaciones
 UNION ALL SELECT 'accounts',RTRIM(Cod_TipIte),ISNULL(RTRIM(Cod_CtaCon),'') FROM dbo.CN_TIPOITEM;
END
GO
CREATE PROCEDURE dbo.PA_LG_IMPORTACIONES_CONDICIONES_U0001
 @Num_Importacion char(10), @Secuencia char(3), @Des_Condicion varchar(100)
AS
BEGIN
 SET NOCOUNT ON;
 SET XACT_ABORT ON;
 IF LEN(LTRIM(RTRIM(ISNULL(@Des_Condicion,''))))=0
 BEGIN RAISERROR('La condición de pago es obligatoria.',16,1); RETURN; END;
 IF NOT EXISTS(SELECT 1 FROM dbo.LG_IMPORTACIONES_CONDICIONES WITH(UPDLOCK,HOLDLOCK) WHERE Num_Importacion=@Num_Importacion AND Secuencia=@Secuencia)
 BEGIN RAISERROR('La condición de pago cambió o ya no existe.',16,1); RETURN; END;
 -- El procedimiento original UP_MAN_LG_Importaciones_Condiciones solo contempla I y D.
 UPDATE dbo.LG_IMPORTACIONES_CONDICIONES SET Des_Condicion=LTRIM(RTRIM(@Des_Condicion))
 WHERE Num_Importacion=@Num_Importacion AND Secuencia=@Secuencia;
END
GO
