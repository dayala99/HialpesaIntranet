/* Búsqueda histórica por procedimientos. Compatible SQL Server 2008 R2.
 Ejecutar en HIALPESA antes de actualizar Back. Solo crea/actualiza procedimientos;
 no cambia tablas, datos ni los procedimientos existentes de guardar/detalle.
 S0002: candidatos históricos por NP/precio/tiempo, cursor y fábrica.
 S0003: snapshots paginados para catálogo de tipos históricos.
 S0004: detección del esquema español/legado.
 Tipo continúa leyéndose del snapshot en C#: SQL 2008 R2 no interpreta JSON nativamente.
 Ningún tipo se obtiene de la NP actual. No requiere agregar columnas. */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID('dbo.PA_LG_ANALISIS_RB_HISTORIAL_S0002','P') IS NULL EXEC('CREATE PROCEDURE dbo.PA_LG_ANALISIS_RB_HISTORIAL_S0002 AS RETURN 0');
GO
ALTER PROCEDURE dbo.PA_LG_ANALISIS_RB_HISTORIAL_S0002
 @Cod_Fabrica char(3),@Cod_OrdPro char(5)=NULL,@Imp_Precio_Comercial decimal(18,4)=NULL,
 @Min_Costura decimal(18,2)=NULL,@Num_Registro_Anterior bigint=NULL
AS
BEGIN
 SET NOCOUNT ON;
 SET @Cod_OrdPro=NULLIF(LTRIM(RTRIM(@Cod_OrdPro)),'');
 IF @Num_Registro_Anterior<=0 OR @Imp_Precio_Comercial<0 OR @Min_Costura<0
 BEGIN RAISERROR('Filtros de historial invalidos.',16,1);RETURN;END;
 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Num_Registro') IS NOT NULL
 BEGIN
  EXEC sys.sp_executesql N'SELECT TOP(200) Num_Registro AS Id,Cod_OrdPro AS Np,Num_Version AS Revision,Tip_Accion AS Action,Num_Prendas_Programadas AS ProgrammedQuantity,Imp_Precio_Comercial AS CommercialPrice,Min_Costura AS SewingTime,Min_Considerados AS UsedTime,Imp_Precio_Servicio AS ServicePrice,Cod_Usuario AS GestionUser,Fec_Registro_UTC AS SavedAtUtc,Txt_Datos_Analisis AS SnapshotJson
  FROM dbo.LG_ANALISIS_RB_HISTORIAL
  WHERE Cod_Fabrica=@Cod_Fabrica AND (@Cod_OrdPro IS NULL OR Cod_OrdPro=@Cod_OrdPro)
  AND (@Imp_Precio_Comercial IS NULL OR Imp_Precio_Comercial=@Imp_Precio_Comercial)
  AND (@Min_Costura IS NULL OR Min_Costura=@Min_Costura)
  AND (@Num_Registro_Anterior IS NULL OR Num_Registro<@Num_Registro_Anterior)
  ORDER BY Num_Registro DESC OPTION(RECOMPILE);', N'@Cod_Fabrica char(3),@Cod_OrdPro char(5),@Imp_Precio_Comercial decimal(18,4),@Min_Costura decimal(18,2),@Num_Registro_Anterior bigint',@Cod_Fabrica,@Cod_OrdPro,@Imp_Precio_Comercial,@Min_Costura,@Num_Registro_Anterior;
 END
 ELSE
 BEGIN
  EXEC sys.sp_executesql N'SELECT TOP(200) Id,Np,Revision,Action,ProgrammedQuantity,CommercialPrice,SewingTime,UsedTime,ServicePrice,GestionUser,SavedAtUtc,SnapshotJson
  FROM dbo.LG_ANALISIS_RB_HISTORIAL
  WHERE Factory=@Cod_Fabrica AND (@Cod_OrdPro IS NULL OR Np=@Cod_OrdPro)
  AND (@Imp_Precio_Comercial IS NULL OR CommercialPrice=@Imp_Precio_Comercial)
  AND (@Min_Costura IS NULL OR SewingTime=@Min_Costura)
  AND (@Num_Registro_Anterior IS NULL OR Id<@Num_Registro_Anterior)
  ORDER BY Id DESC OPTION(RECOMPILE);', N'@Cod_Fabrica char(3),@Cod_OrdPro char(5),@Imp_Precio_Comercial decimal(18,4),@Min_Costura decimal(18,2),@Num_Registro_Anterior bigint',@Cod_Fabrica,@Cod_OrdPro,@Imp_Precio_Comercial,@Min_Costura,@Num_Registro_Anterior;
 END;
END;
GO
IF OBJECT_ID('dbo.PA_LG_ANALISIS_RB_HISTORIAL_S0003','P') IS NULL EXEC('CREATE PROCEDURE dbo.PA_LG_ANALISIS_RB_HISTORIAL_S0003 AS RETURN 0');
GO
ALTER PROCEDURE dbo.PA_LG_ANALISIS_RB_HISTORIAL_S0003
 @Cod_Fabrica char(3),@Num_Registro_Anterior bigint
AS
BEGIN
 SET NOCOUNT ON;
 IF @Num_Registro_Anterior<=0 BEGIN RAISERROR('Cursor invalido.',16,1);RETURN;END;
 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Num_Registro') IS NOT NULL
  EXEC sys.sp_executesql N'SELECT TOP(200) Num_Registro AS Id,Txt_Datos_Analisis AS Json FROM dbo.LG_ANALISIS_RB_HISTORIAL
  WHERE Cod_Fabrica=@Cod_Fabrica AND Num_Registro<@Num_Registro_Anterior ORDER BY Num_Registro DESC;', N'@Cod_Fabrica char(3),@Num_Registro_Anterior bigint',@Cod_Fabrica,@Num_Registro_Anterior;
 ELSE
  EXEC sys.sp_executesql N'SELECT TOP(200) Id,SnapshotJson AS Json FROM dbo.LG_ANALISIS_RB_HISTORIAL
  WHERE Factory=@Cod_Fabrica AND Id<@Num_Registro_Anterior ORDER BY Id DESC;', N'@Cod_Fabrica char(3),@Num_Registro_Anterior bigint',@Cod_Fabrica,@Num_Registro_Anterior;
END;
GO
IF OBJECT_ID('dbo.PA_LG_ANALISIS_RB_HISTORIAL_S0004','P') IS NULL EXEC('CREATE PROCEDURE dbo.PA_LG_ANALISIS_RB_HISTORIAL_S0004 AS RETURN 0');
GO
ALTER PROCEDURE dbo.PA_LG_ANALISIS_RB_HISTORIAL_S0004
AS
BEGIN
 SET NOCOUNT ON;
 SELECT CAST(CASE WHEN COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Num_Registro') IS NULL THEN 0 ELSE 1 END AS bit) AS Flg_Nombres_Espanol;
END;
GO
