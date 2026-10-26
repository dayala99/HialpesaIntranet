/* Control de Importaciones. SQL Server 2008 R2, compatibilidad 80.
   Script para revisión y registro manual. No altera los procedimientos de Gestión.
   Cinco procedimientos nuevos. Ejecutar cada bloque separado por GO. */
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE dbo.PA_LG_IMPORTACIONES_S0003
 @Mode char(1), @Number varchar(10)='', @AnnexType char(1)='', @Annex char(4)='',
 @Status char(1)='', @Guide varchar(25)='', @Origin char(3)='', @Item varchar(20)='',
 @Country varchar(3)='', @Associated varchar(10)='', @Policy varchar(18)='',
 @Year int=0, @Month int=0, @DocumentType varchar(3)='', @DocumentSeries varchar(4)='',
 @DocumentNumber varchar(20)='', @ItemType char(3)='', @Factory varchar(3)='',
 @ProductionOrder varchar(5)='', @From datetime=NULL, @To datetime=NULL, @Offset int=0
AS
BEGIN
 SET NOCOUNT ON;
 IF @Offset<0 OR @Mode NOT IN ('1','2','3','4','5','6','7','8','9','0','A','B','C','D')
 BEGIN RAISERROR('Filtro de importaciones inválido.',16,1); RETURN; END;
 SELECT i.Num_Importacion INTO #Filtered
 FROM dbo.LG_IMPORTACIONES i
 WHERE
  (@Mode='1' AND i.Num_Importacion=@Number)
  OR (@Mode='2' AND YEAR(i.Fec_RegDoc)=@Year AND MONTH(i.Fec_RegDoc)=@Month)
  OR (@Mode='3' AND (EXISTS(SELECT 1 FROM dbo.CN_DOCUM d WHERE d.Num_Importacion=i.Num_Importacion AND d.Cod_TipDoc=@DocumentType AND d.Ser_Docum LIKE '%'+RTRIM(@DocumentSeries)+'%' AND d.Num_Docum LIKE '%'+RTRIM(@DocumentNumber))
                    OR EXISTS(SELECT 1 FROM dbo.CN_DOCUM_AUXILIAR d WHERE d.Num_Importacion=i.Num_Importacion AND d.Cod_TipDoc=@DocumentType AND d.Ser_Docum LIKE '%'+RTRIM(@DocumentSeries)+'%' AND d.Num_Docum LIKE '%'+RTRIM(@DocumentNumber))))
  OR (@Mode='4' AND (EXISTS(SELECT 1 FROM dbo.CN_DOCUM d WHERE d.Num_Corre=i.Num_Corre_PolizaImportacion AND d.Num_Docum LIKE '%'+RTRIM(@Policy)+'%')
                     OR EXISTS(SELECT 1 FROM dbo.CN_DOCUM_AUXILIAR d WHERE d.Num_Corre=i.Num_Corre_PolizaImportacion AND d.Num_Docum LIKE '%'+RTRIM(@Policy)+'%')))
  OR (@Mode='5' AND i.Cod_TipIte=@ItemType)
  OR @Mode='6'
  OR (@Mode='7' AND i.Cod_TipAnex=@AnnexType AND i.Cod_Anxo=@Annex)
  OR (@Mode='8' AND i.Flg_Status=@Status)
  OR (@Mode='9' AND i.Num_Guia=@Guide)
  OR (@Mode='0' AND i.Cod_OrigenImportacion=@Origin)
  OR (@Mode='A' AND EXISTS(SELECT 1 FROM dbo.LG_MOVISTKITEM mi JOIN dbo.LG_MOVISTK m ON m.Cod_Almacen=mi.Cod_Almacen AND m.Num_MovStk=mi.Num_MovStk JOIN dbo.LG_IMPORTACIONES_DOCUM d ON d.Num_Corre=m.Num_Corre
      WHERE mi.Cod_Almacen='01' AND mi.Cod_Item=@Item AND mi.Valor_Import_Dolares+mi.Valor_Import_Soles>0 AND d.Num_Importacion=i.Num_Importacion))
  OR (@Mode='B' AND EXISTS(SELECT 1 FROM dbo.LG_ORDCOMPITEMREQ r JOIN dbo.LG_MOVISTKITEM mi ON mi.Ser_OrdComp=r.Ser_OrdComp AND mi.Cod_OrdComp=r.Cod_OrdComp
      JOIN dbo.LG_MOVISTK m ON m.Cod_Almacen=mi.Cod_Almacen AND m.Num_MovStk=mi.Num_MovStk JOIN dbo.LG_IMPORTACIONES_DOCUM d ON d.Num_Corre=m.Num_Corre
      WHERE r.Cod_Fabrica=@Factory AND r.Cod_OrdPro=@ProductionOrder AND mi.Cod_Almacen='01' AND mi.Valor_Import_Dolares+mi.Valor_Import_Soles>0 AND d.Num_Importacion=i.Num_Importacion))
  OR (@Mode='C' AND EXISTS(SELECT 1 FROM dbo.Lg_Importaciones_Asociadas_Liquidacion a WHERE a.Num_Importacion_Principal=i.Num_Importacion AND a.Num_Importacion_Asociada=@Associated))
  OR (@Mode='D' AND i.ID_PAIS_DUA=@Country);
 IF @Mode IN ('0','5','7','8','D')
 DELETE f FROM #Filtered f JOIN dbo.LG_IMPORTACIONES i ON i.Num_Importacion=f.Num_Importacion
 WHERE (@From IS NOT NULL AND i.Fec_RegDoc<@From) OR (@To IS NOT NULL AND i.Fec_RegDoc>=DATEADD(day,1,@To));
 SELECT COUNT(*) FROM #Filtered;
 ;WITH Pagina AS (
  SELECT i.Num_Importacion Number,i.Fec_RegDoc RegisteredAt,a.Des_Anexo AnnexName,i.Cod_TipAnex AnnexType,i.Cod_Anxo Annex,
   i.Flg_Status Status,s.Des_Status StatusName,i.Cod_Moneda Currency,i.Cod_TipIte ItemType,t.Des_TipIte ItemTypeName,
   i.Num_Guia Guide,i.Fecha_Guia GuideDate,g.Des_OrigenImportacion OriginName,i.ID_PAIS_DUA Country,p.Descripcion CountryName,
   i.Num_Corre Document,i.Num_Importacion_Inicial Principal,i.Flg_Liquidacion_Parcial Partial,
   dbo.SM_OBTIENE_ORDENDECOMPRAIMPORTACION(i.Num_Importacion) Orders,
   dbo.SM_OBTIENE_POLIZAIMPORTACION(i.Num_Importacion) Policy,
   dbo.CN_OBTIENE_VALOR_FOB_IMPORTACION(i.Num_Importacion) Fob,
   ROW_NUMBER() OVER(ORDER BY i.Fec_RegDoc DESC,i.Num_Importacion DESC) rn
  FROM #Filtered f JOIN dbo.LG_IMPORTACIONES i ON i.Num_Importacion=f.Num_Importacion
  LEFT JOIN dbo.CN_ANEXOSCONTABLES a ON a.Cod_TipAnex=i.Cod_TipAnex AND a.Cod_Anxo=i.Cod_Anxo
  LEFT JOIN dbo.LG_ESTADOS s ON s.Flg_Status=i.Flg_Status
  LEFT JOIN dbo.CN_TIPOITEM t ON t.Cod_TipIte=i.Cod_TipIte
  LEFT JOIN dbo.LG_ORIGEN_IMPORTACION g ON g.Cod_OrigenImportacion=i.Cod_OrigenImportacion
  LEFT JOIN dbo.CN_PAISES p ON p.Cod_Pais=i.ID_PAIS_DUA
 ) SELECT * FROM Pagina WHERE rn>@Offset AND rn<=@Offset+50 ORDER BY rn;
END
GO
CREATE PROCEDURE dbo.PA_LG_IMPORTACIONES_S0004 @Number varchar(10), @Lock bit=0
AS
BEGIN
 SET NOCOUNT ON;
 -- La transacción del repositorio mantiene este bloqueo durante modificar/eliminar.
 SELECT * INTO #Import FROM dbo.LG_IMPORTACIONES WITH(UPDLOCK,HOLDLOCK) WHERE Num_Importacion=@Number;
 SELECT i.*,
  a.Des_Anexo AnnexName,
  CAST(CASE WHEN i.Flg_Status='R' OR EXISTS(SELECT 1 FROM dbo.LG_IMPORTACIONES_EXTORNOS e WHERE e.Num_Importacion=i.Num_Importacion AND e.Tipo_Extorno='P' AND e.Flg_Status='P') THEN 1 ELSE 0 END AS bit) CanEdit,
  CAST(CASE WHEN EXISTS(SELECT 1 FROM dbo.CN_DOCUM d WHERE d.Num_Importacion=i.Num_Importacion)
             OR EXISTS(SELECT 1 FROM dbo.LG_IMPORTACIONES_DOCUM d WHERE d.Num_Importacion=i.Num_Importacion)
             OR EXISTS(SELECT 1 FROM dbo.CN_DOCUMENTOS_PAGOS p WHERE p.Num_Corre=i.Num_Corre)
             OR EXISTS(SELECT 1 FROM dbo.LG_IMPORTACIONES l WHERE l.Num_Importacion_Inicial=i.Num_Importacion)
             OR EXISTS(SELECT 1 FROM dbo.Lg_Importaciones_Asociadas_Liquidacion l WHERE l.Num_Importacion_Principal=i.Num_Importacion OR l.Num_Importacion_Asociada=i.Num_Importacion)
       THEN 1 ELSE 0 END AS bit) DeleteBlocked
 FROM #Import i LEFT JOIN dbo.CN_ANEXOSCONTABLES a ON a.Cod_TipAnex=i.Cod_TipAnex AND a.Cod_Anxo=i.Cod_Anxo;
END
GO
CREATE PROCEDURE dbo.PA_LG_IMPORTACIONES_S0005
AS
BEGIN
 SET NOCOUNT ON;
 SELECT 'origins' Kind,RTRIM(Cod_OrigenImportacion) Code,RTRIM(Des_OrigenImportacion) Description FROM dbo.LG_ORIGEN_IMPORTACION
 UNION ALL SELECT 'shipments',RTRIM(Cod_Embarque),RTRIM(Des_Embarque) FROM dbo.TG_TIPEMB
 UNION ALL SELECT 'countries',RTRIM(Cod_Pais),RTRIM(Descripcion) FROM dbo.CN_PAISES
 UNION ALL SELECT 'itemTypes',RTRIM(Cod_TipIte),RTRIM(Des_TipIte) FROM dbo.CN_TIPOITEM WHERE Flg_Importacion='S'
 UNION ALL SELECT 'areas',RTRIM(Cod_AreaResponsable),RTRIM(Des_AreaResponsable) FROM dbo.CN_AreasResponsables
 UNION ALL SELECT 'currencies',RTRIM(Cod_Moneda),RTRIM(Nom_Moneda) FROM dbo.TG_MONEDA
 UNION ALL SELECT 'annexTypes',RTRIM(Cod_TipAnex),RTRIM(Des_TipAnex) FROM dbo.CN_TipoAnexoContable
 UNION ALL SELECT 'statuses',RTRIM(Flg_Status),RTRIM(Des_Status) FROM dbo.LG_ESTADOS
 UNION ALL SELECT 'factories',RTRIM(Cod_Fabrica),RTRIM(Nom_Fabrica) FROM dbo.TG_FABRICA;
 SELECT CAST(CASE WHEN Flg_NumImportacion_Manual='N' THEN 1 ELSE 0 END AS bit) AutomaticNumbering FROM dbo.TG_CONTROL;
END
GO
CREATE PROCEDURE dbo.PA_CN_ANEXOSCONTABLES_S0002 @Type char(1), @Search nvarchar(70)
AS
BEGIN
 SET NOCOUNT ON;
 IF LEN(LTRIM(RTRIM(@Search)))<2 RETURN;
 DECLARE @Pattern nvarchar(145);
 SET @Pattern=N'%'+REPLACE(REPLACE(REPLACE(REPLACE(LTRIM(RTRIM(@Search)),N'~',N'~~'),N'%',N'~%'),N'_',N'~_'),N'[',N'~[')+N'%';
 SELECT TOP 30 RTRIM(Cod_Anxo) Code,RTRIM(Des_Anexo) Description FROM dbo.CN_ANEXOSCONTABLES
 WHERE Cod_TipAnex=@Type AND (Cod_Anxo LIKE @Pattern ESCAPE '~' OR Des_Anexo LIKE @Pattern ESCAPE '~') ORDER BY Des_Anexo,Cod_Anxo;
END
GO
CREATE PROCEDURE dbo.PA_LG_IMPORTACIONES_D0001 @Number varchar(10)
AS
BEGIN
 SET NOCOUNT ON;
 -- Solo después de eliminar mediante UP_MAN_LG_IMPORTACIONES, en la misma transacción.
 IF EXISTS(SELECT 1 FROM dbo.LG_IMPORTACIONES WHERE Num_Importacion=@Number)
 BEGIN RAISERROR('La importación no fue eliminada. No se retiraron asociaciones.',16,1); RETURN; END;
 UPDATE dbo.LG_ORDCOMP SET Num_Importacion='' WHERE Num_Importacion=@Number;
END
GO
