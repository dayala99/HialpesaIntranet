-- NUEVO procedimiento de búsqueda web. SQL Server 2008 R2.
-- Sin criterios: 100 importaciones más recientes. Con criterios: todas las coincidencias en páginas de 100.
-- No reemplaza ni altera PA_LG_IMPORTACIONES_S0003 ni los procedimientos de Gestión.
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE dbo.PA_LG_IMPORTACIONES_S0009
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
 IF (@From IS NOT NULL OR @To IS NOT NULL) AND @Mode NOT IN ('0','5','7','8','D')
 BEGIN RAISERROR('El filtro seleccionado no admite rango de fechas.',16,1); RETURN; END;
 IF @From IS NOT NULL AND @To IS NOT NULL AND @From>@To
 BEGIN RAISERROR('Revisa el rango de fechas.',16,1); RETURN; END;
 DECLARE @Empty bit;
 SET @Empty=CASE
 WHEN @Mode='1' AND LTRIM(RTRIM(@Number))='' THEN 1
 WHEN @Mode='2' AND @Year=0 AND @Month=0 THEN 1
 WHEN @Mode='3' AND LTRIM(RTRIM(@DocumentNumber))='' AND LTRIM(RTRIM(@DocumentSeries))='' THEN 1
 WHEN @Mode='4' AND LTRIM(RTRIM(@Policy))='' THEN 1
 WHEN @Mode='5' AND RTRIM(@ItemType)='' THEN 1
 WHEN @Mode='6' THEN 1
 WHEN @Mode='7' AND RTRIM(@Annex)='' THEN 1
 WHEN @Mode='8' AND RTRIM(@Status)='' THEN 1
 WHEN @Mode='9' AND LTRIM(RTRIM(@Guide))='' THEN 1
 WHEN @Mode='0' AND RTRIM(@Origin)='' THEN 1
 WHEN @Mode='A' AND LTRIM(RTRIM(@Item))='' THEN 1
 WHEN @Mode='B' AND RTRIM(@Factory)='' AND RTRIM(@ProductionOrder)='' THEN 1
 WHEN @Mode='C' AND LTRIM(RTRIM(@Associated))='' THEN 1
 WHEN @Mode='D' AND RTRIM(@Country)='' THEN 1 ELSE 0 END;
 IF @From IS NOT NULL OR @To IS NOT NULL SET @Empty=0;
 SELECT TOP 0 Num_Importacion INTO #Filtered FROM dbo.LG_IMPORTACIONES;
 IF @Empty=1
  INSERT INTO #Filtered SELECT TOP 100 Num_Importacion FROM dbo.LG_IMPORTACIONES ORDER BY Fec_RegDoc DESC,Num_Importacion DESC;
 ELSE
 INSERT INTO #Filtered SELECT i.Num_Importacion FROM dbo.LG_IMPORTACIONES i
 WHERE
  (@Mode='1' AND (LTRIM(RTRIM(@Number))='' OR i.Num_Importacion LIKE '%'+LTRIM(RTRIM(@Number))+'%'))
  OR (@Mode='2' AND YEAR(i.Fec_RegDoc)=@Year AND MONTH(i.Fec_RegDoc)=@Month)
  OR (@Mode='3' AND (EXISTS(SELECT 1 FROM dbo.CN_DOCUM d WHERE d.Num_Importacion=i.Num_Importacion AND d.Cod_TipDoc=@DocumentType AND d.Ser_Docum LIKE '%'+RTRIM(@DocumentSeries)+'%' AND d.Num_Docum LIKE '%'+RTRIM(@DocumentNumber))
                    OR EXISTS(SELECT 1 FROM dbo.CN_DOCUM_AUXILIAR d WHERE d.Num_Importacion=i.Num_Importacion AND d.Cod_TipDoc=@DocumentType AND d.Ser_Docum LIKE '%'+RTRIM(@DocumentSeries)+'%' AND d.Num_Docum LIKE '%'+RTRIM(@DocumentNumber))))
  OR (@Mode='4' AND (EXISTS(SELECT 1 FROM dbo.CN_DOCUM d WHERE d.Num_Corre=i.Num_Corre_PolizaImportacion AND d.Num_Docum LIKE '%'+RTRIM(@Policy)+'%')
                     OR EXISTS(SELECT 1 FROM dbo.CN_DOCUM_AUXILIAR d WHERE d.Num_Corre=i.Num_Corre_PolizaImportacion AND d.Num_Docum LIKE '%'+RTRIM(@Policy)+'%')))
  OR (@Mode='5' AND (RTRIM(@ItemType)='' OR i.Cod_TipIte=@ItemType))
  OR @Mode='6'
  OR (@Mode='7' AND (RTRIM(@Annex)='' OR (i.Cod_TipAnex=@AnnexType AND i.Cod_Anxo=@Annex)))
  OR (@Mode='8' AND (RTRIM(@Status)='' OR i.Flg_Status=@Status))
  OR (@Mode='9' AND (LTRIM(RTRIM(@Guide))='' OR i.Num_Guia LIKE '%'+LTRIM(RTRIM(@Guide))+'%'))
  OR (@Mode='0' AND (RTRIM(@Origin)='' OR i.Cod_OrigenImportacion=@Origin))
  OR (@Mode='A' AND EXISTS(SELECT 1 FROM dbo.LG_MOVISTKITEM mi JOIN dbo.LG_MOVISTK m ON m.Cod_Almacen=mi.Cod_Almacen AND m.Num_MovStk=mi.Num_MovStk JOIN dbo.LG_IMPORTACIONES_DOCUM d ON d.Num_Corre=m.Num_Corre
      WHERE mi.Cod_Almacen='01' AND mi.Cod_Item=@Item AND mi.Valor_Import_Dolares+mi.Valor_Import_Soles>0 AND d.Num_Importacion=i.Num_Importacion))
  OR (@Mode='B' AND EXISTS(SELECT 1 FROM dbo.LG_ORDCOMPITEMREQ r JOIN dbo.LG_MOVISTKITEM mi ON mi.Ser_OrdComp=r.Ser_OrdComp AND mi.Cod_OrdComp=r.Cod_OrdComp
      JOIN dbo.LG_MOVISTK m ON m.Cod_Almacen=mi.Cod_Almacen AND m.Num_MovStk=mi.Num_MovStk JOIN dbo.LG_IMPORTACIONES_DOCUM d ON d.Num_Corre=m.Num_Corre
      WHERE r.Cod_Fabrica=@Factory AND r.Cod_OrdPro=@ProductionOrder AND mi.Cod_Almacen='01' AND mi.Valor_Import_Dolares+mi.Valor_Import_Soles>0 AND d.Num_Importacion=i.Num_Importacion))
  OR (@Mode='C' AND EXISTS(SELECT 1 FROM dbo.Lg_Importaciones_Asociadas_Liquidacion a WHERE a.Num_Importacion_Principal=i.Num_Importacion AND a.Num_Importacion_Asociada=@Associated))
  OR (@Mode='D' AND (RTRIM(@Country)='' OR i.ID_PAIS_DUA=@Country));
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
 ) SELECT * FROM Pagina WHERE rn>@Offset AND rn<=@Offset+100 ORDER BY rn;
END
GO
