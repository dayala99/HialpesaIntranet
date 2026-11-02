/* PCP -> Logística. SQL Server 2008 R2. Ejecutar en HIALPESA antes de Back.
 Nueva tabla independiente; no cambia Gestión ni las tablas existentes.
 E=Enviado a Logística, A=Finalizado, S=Reemplazado por nuevo envío.
 Historial de envíos por NP+sector. La selección completa se guarda como snapshot. */
SET ARITHABORT ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET NUMERIC_ROUNDABORT OFF;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID('dbo.LG_PCP_ANALITICAS_ENVIO','U') IS NULL
BEGIN
 CREATE TABLE dbo.LG_PCP_ANALITICAS_ENVIO(
 Num_Envio bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_LG_PCP_ANALITICAS_ENVIO PRIMARY KEY,
 Cod_Solicitud uniqueidentifier NOT NULL CONSTRAINT UQ_LG_PCP_ANALITICAS_SOLICITUD UNIQUE,
 Cod_Fabrica char(3) NOT NULL,Cod_OrdPro char(5) NOT NULL,Cod_Sector varchar(10) NOT NULL,
 Cod_Tarifado char(5) NOT NULL,Cod_Variante char(1) NOT NULL,Num_Version int NOT NULL,
 Tip_Costura char(1) NOT NULL,Tip_Estado char(1) NOT NULL,
 Num_Operaciones_Total int NOT NULL,Num_Operaciones_Seleccionadas int NOT NULL,
 Min_Total decimal(18,6) NOT NULL,Min_Seleccionados decimal(18,6) NOT NULL,
 Txt_Seleccion nvarchar(max) NOT NULL,Cod_Usuario_Envio varchar(100) NOT NULL,
 Fec_Envio_UTC datetime NOT NULL,Num_Registro_Historial bigint NULL,
 Cod_Usuario_Atencion varchar(100) NULL,Fec_Atencion_UTC datetime NULL,
 CONSTRAINT CK_LG_PCP_ANALITICAS_ESTADO CHECK(Tip_Estado IN ('E','A','S')),
 CONSTRAINT CK_LG_PCP_ANALITICAS_SELECCION CHECK(Num_Operaciones_Seleccionadas>0 AND Num_Operaciones_Seleccionadas<=Num_Operaciones_Total AND Min_Seleccionados>0 AND Min_Seleccionados<=Min_Total AND ((Tip_Costura='C' AND Num_Operaciones_Total=Num_Operaciones_Seleccionadas) OR (Tip_Costura='P' AND Num_Operaciones_Seleccionadas<Num_Operaciones_Total)))
 );
 CREATE INDEX IX_LG_PCP_ANALITICAS_VERSION ON dbo.LG_PCP_ANALITICAS_ENVIO(Cod_Fabrica,Cod_OrdPro,Cod_Sector,Num_Version DESC);
 CREATE UNIQUE INDEX UX_LG_PCP_ANALITICAS_ACTIVO ON dbo.LG_PCP_ANALITICAS_ENVIO(Cod_Fabrica,Cod_OrdPro,Cod_Sector) WHERE Tip_Estado='E';
END;
GO
IF OBJECT_ID('dbo.PA_LG_PCP_ANALITICAS_S0002','P') IS NULL EXEC('CREATE PROCEDURE dbo.PA_LG_PCP_ANALITICAS_S0002 AS RETURN 0');
GO
ALTER PROCEDURE dbo.PA_LG_PCP_ANALITICAS_S0002 @Cod_Fabrica char(3),@Cod_OrdPro char(5)
AS
BEGIN
 SET NOCOUNT ON;SET ARITHABORT ON;
 SELECT CAST(CASE WHEN COALESCE(NULLIF(assigned.Price,0),quoted.Price)>0 THEN 1 ELSE 0 END AS bit) AS Flg_Precio_Comercial,
 CAST(CASE WHEN UPPER(LTRIM(RTRIM(dbo.CF_STATUS_TARIFADO(op.Cod_OrdPro))))='D' THEN 1 ELSE 0 END AS bit) AS Flg_Tarifado,
 CAST(CASE WHEN UPPER(LTRIM(RTRIM(dbo.CF_STATUS_FICHA_TECNICA(op.Cod_Fabrica,op.Cod_OrdPro))))='SI' THEN 1 ELSE 0 END AS bit) AS Flg_Ficha_Tecnica,
 CAST(COALESCE(NULLIF(assigned.Price,0),quoted.Price) AS decimal(18,4)) AS Imp_Precio_Comercial
 FROM dbo.ES_OrdPro op
    OUTER APPLY (
        SELECT MAX(f.MObra_Confec) AS Price
        FROM dbo.TG_LOTESTPRO a
        INNER JOIN dbo.TG_COTIZACION_ESTILOSPROPIOS f
            ON f.Num_Solicitud_Cons=a.NUM_SOLICITUD_COSTEO_ASIGNADA
           AND f.Cod_EstPro=a.COD_ESTPRO_ASIGNADA
           AND f.Cod_Version=a.COD_VERSION_ASIGNADA
        WHERE a.Cod_Fabrica=op.cod_fabrica AND a.Cod_OrdPro=op.cod_ordpro
          AND f.Flg_Status='C'
    ) assigned
    OUTER APPLY (
        SELECT MAX(f.MObra_Confec) AS Price
        FROM dbo.TG_LOTESTPRO a
        INNER JOIN dbo.TG_PURORD c ON a.Cod_Cliente=c.Cod_Cliente AND a.Cod_Purord=c.Cod_Purord
        INNER JOIN dbo.TG_ESTCLIEST d ON a.Cod_Cliente=d.Cod_Cliente AND c.Cod_TemCli=d.Cod_TemCli
            AND a.Cod_EstCli=d.Cod_EstCli AND a.Cod_EstPro=d.Cod_EstPro
        INNER JOIN dbo.TG_ESTCLITEM e ON a.Cod_Cliente=e.Cod_Cliente AND c.Cod_TemCli=e.Cod_TemCli
            AND a.Cod_EstCli=e.Cod_EstCli
        INNER JOIN dbo.TG_COTIZACION_ESTILOSPROPIOS f ON e.Num_Solicitud_Cons=f.Num_Solicitud_Cons
            AND a.Cod_EstPro=f.Cod_EstPro AND d.Cod_Version_Costeo=f.Cod_Version
        WHERE a.Cod_Fabrica=op.cod_fabrica AND a.Cod_OrdPro=op.cod_ordpro AND f.Flg_Status='C'
    ) quoted

 WHERE op.Cod_Fabrica=@Cod_Fabrica AND op.Cod_OrdPro=@Cod_OrdPro;
END;
GO
IF OBJECT_ID('dbo.PA_LG_PCP_ANALITICAS_I0001','P') IS NULL EXEC('CREATE PROCEDURE dbo.PA_LG_PCP_ANALITICAS_I0001 AS RETURN 0');
GO
ALTER PROCEDURE dbo.PA_LG_PCP_ANALITICAS_I0001
 @Cod_Solicitud uniqueidentifier,@Cod_Fabrica char(3),@Cod_OrdPro char(5),@Cod_Sector varchar(10),@Cod_Tarifado char(5),@Cod_Variante char(1),
 @Num_Operaciones_Total int,@Num_Operaciones_Seleccionadas int,@Min_Total decimal(18,6),@Min_Seleccionados decimal(18,6),@Txt_Seleccion nvarchar(max),@Cod_Usuario varchar(100)
AS
BEGIN
 SET NOCOUNT ON;
 SET ARITHABORT ON;SET ANSI_NULLS ON;SET ANSI_PADDING ON;SET ANSI_WARNINGS ON;SET CONCAT_NULL_YIELDS_NULL ON;SET QUOTED_IDENTIFIER ON;SET NUMERIC_ROUNDABORT OFF;
 -- Compilar las escrituras después de fijar SET, también con compatibilidad 80.
 EXEC sys.sp_executesql N'SET NOCOUNT ON;SET XACT_ABORT ON;SET ARITHABORT ON;
 BEGIN TRY
 BEGIN TRANSACTION;
 IF EXISTS(SELECT 1 FROM dbo.LG_PCP_ANALITICAS_ENVIO WITH(UPDLOCK,HOLDLOCK) WHERE Cod_Solicitud=@Cod_Solicitud)
 BEGIN
 IF NOT EXISTS(SELECT 1 FROM dbo.LG_PCP_ANALITICAS_ENVIO WHERE Cod_Solicitud=@Cod_Solicitud AND Cod_Fabrica=@Cod_Fabrica AND Cod_OrdPro=@Cod_OrdPro AND Cod_Sector=@Cod_Sector AND Cod_Usuario_Envio=@Cod_Usuario AND Txt_Seleccion=@Txt_Seleccion) RAISERROR(''La solicitud ya fue usada con otros datos.'',16,1);
 SELECT * FROM dbo.LG_PCP_ANALITICAS_ENVIO WHERE Cod_Solicitud=@Cod_Solicitud;
 COMMIT TRANSACTION;RETURN;
 END;

 DECLARE @Requisitos TABLE(Flg_Precio_Comercial bit,Flg_Tarifado bit,Flg_Ficha_Tecnica bit,Imp_Precio_Comercial decimal(18,4));
 INSERT @Requisitos EXEC dbo.PA_LG_PCP_ANALITICAS_S0002 @Cod_Fabrica,@Cod_OrdPro;
 IF NOT EXISTS(SELECT 1 FROM @Requisitos) BEGIN RAISERROR(''La NP no existe.'',16,1);RETURN;END;
 DECLARE @Des_Faltantes nvarchar(1000);
 SELECT @Des_Faltantes=CASE WHEN Flg_Precio_Comercial=0 THEN N''Precio Comercial (debe ser mayor que cero); '' ELSE N'''' END+
 CASE WHEN Flg_Tarifado=0 THEN N''Status Tarifado (debe estar en D); '' ELSE N'''' END+
 CASE WHEN Flg_Ficha_Tecnica=0 THEN N''Status Ficha Técnica (debe estar en SI); '' ELSE N'''' END FROM @Requisitos;
 IF LEN(@Des_Faltantes)>0 BEGIN SET @Des_Faltantes=N''No se puede enviar a Logística. Falta: ''+@Des_Faltantes;RAISERROR(@Des_Faltantes,16,1);RETURN;END;
 DECLARE @Num_Version int;
 SELECT @Num_Version=ISNULL(MAX(Num_Version),0)+1 FROM dbo.LG_PCP_ANALITICAS_ENVIO WITH(UPDLOCK,HOLDLOCK) WHERE Cod_Fabrica=@Cod_Fabrica AND Cod_OrdPro=@Cod_OrdPro AND Cod_Sector=@Cod_Sector;
 UPDATE dbo.LG_PCP_ANALITICAS_ENVIO SET Tip_Estado=''S'' WHERE Cod_Fabrica=@Cod_Fabrica AND Cod_OrdPro=@Cod_OrdPro AND Cod_Sector=@Cod_Sector AND Tip_Estado=''E'';
 INSERT dbo.LG_PCP_ANALITICAS_ENVIO(Cod_Solicitud,Cod_Fabrica,Cod_OrdPro,Cod_Sector,Cod_Tarifado,Cod_Variante,Num_Version,Tip_Costura,Tip_Estado,Num_Operaciones_Total,Num_Operaciones_Seleccionadas,Min_Total,Min_Seleccionados,Txt_Seleccion,Cod_Usuario_Envio,Fec_Envio_UTC)
 VALUES(@Cod_Solicitud,@Cod_Fabrica,@Cod_OrdPro,@Cod_Sector,@Cod_Tarifado,@Cod_Variante,@Num_Version,CASE WHEN @Num_Operaciones_Total=@Num_Operaciones_Seleccionadas THEN ''C'' ELSE ''P'' END,''E'',@Num_Operaciones_Total,@Num_Operaciones_Seleccionadas,@Min_Total,@Min_Seleccionados,@Txt_Seleccion,@Cod_Usuario,GETUTCDATE());
 SELECT * FROM dbo.LG_PCP_ANALITICAS_ENVIO WHERE Num_Envio=CONVERT(bigint,SCOPE_IDENTITY());
 COMMIT TRANSACTION;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;DECLARE @Des_Error nvarchar(2048);SET @Des_Error=ERROR_MESSAGE();RAISERROR(@Des_Error,16,1);END CATCH;',
 N'@Cod_Solicitud uniqueidentifier,@Cod_Fabrica char(3),@Cod_OrdPro char(5),@Cod_Sector varchar(10),@Cod_Tarifado char(5),@Cod_Variante char(1),
 @Num_Operaciones_Total int,@Num_Operaciones_Seleccionadas int,@Min_Total decimal(18,6),@Min_Seleccionados decimal(18,6),@Txt_Seleccion nvarchar(max),@Cod_Usuario varchar(100)',
 @Cod_Solicitud,@Cod_Fabrica,@Cod_OrdPro,@Cod_Sector,@Cod_Tarifado,@Cod_Variante,@Num_Operaciones_Total,@Num_Operaciones_Seleccionadas,@Min_Total,@Min_Seleccionados,@Txt_Seleccion,@Cod_Usuario;
END;
GO
IF OBJECT_ID('dbo.PA_LG_PCP_ANALITICAS_S0001','P') IS NULL EXEC('CREATE PROCEDURE dbo.PA_LG_PCP_ANALITICAS_S0001 AS RETURN 0');
GO
ALTER PROCEDURE dbo.PA_LG_PCP_ANALITICAS_S0001
 @Cod_Fabrica char(3),@Cod_OrdPro char(5)=NULL,@Num_Envio bigint=NULL,@Num_Envio_Anterior bigint=NULL,@Tip_Estado char(1)='E'
AS
BEGIN
 SET NOCOUNT ON;
 SELECT TOP(51) Num_Envio,Cod_Solicitud,Cod_Fabrica,Cod_OrdPro,Cod_Sector,Cod_Tarifado,Cod_Variante,Num_Version,Tip_Costura,Tip_Estado,Num_Operaciones_Total,Num_Operaciones_Seleccionadas,Min_Total,Min_Seleccionados,
 CASE WHEN @Num_Envio IS NOT NULL THEN Txt_Seleccion ELSE NULL END AS Txt_Seleccion,Cod_Usuario_Envio,Fec_Envio_UTC,Num_Registro_Historial,Cod_Usuario_Atencion,Fec_Atencion_UTC
 FROM dbo.LG_PCP_ANALITICAS_ENVIO WHERE Cod_Fabrica=@Cod_Fabrica AND (@Cod_OrdPro IS NULL OR Cod_OrdPro=@Cod_OrdPro) AND (@Num_Envio IS NULL OR Num_Envio=@Num_Envio) AND (@Num_Envio_Anterior IS NULL OR Num_Envio<@Num_Envio_Anterior) AND (@Num_Envio IS NOT NULL OR @Tip_Estado IS NULL OR Tip_Estado=@Tip_Estado)
 ORDER BY Num_Envio DESC OPTION(RECOMPILE);
END;
GO
IF OBJECT_ID('dbo.PA_LG_PCP_ANALITICAS_U0001','P') IS NULL EXEC('CREATE PROCEDURE dbo.PA_LG_PCP_ANALITICAS_U0001 AS RETURN 0');
GO
ALTER PROCEDURE dbo.PA_LG_PCP_ANALITICAS_U0001
 @Cod_Fabrica char(3),@Cod_OrdPro char(5),@Num_Envio bigint,@Num_Registro_Historial bigint=NULL,@Cod_Usuario varchar(100)
AS
BEGIN
 SET NOCOUNT ON;
 SET ARITHABORT ON;SET ANSI_NULLS ON;SET ANSI_PADDING ON;SET ANSI_WARNINGS ON;SET CONCAT_NULL_YIELDS_NULL ON;SET QUOTED_IDENTIFIER ON;SET NUMERIC_ROUNDABORT OFF;
 -- Compilar las escrituras después de fijar SET, también con compatibilidad 80.
 EXEC sys.sp_executesql N'SET NOCOUNT ON;
 SET ARITHABORT ON;
 IF @@TRANCOUNT=0 BEGIN RAISERROR(''La atención requiere la transacción del guardado.'',16,1);RETURN;END;
 DECLARE @Tip_Estado char(1),@Num_Actual bigint;
 SELECT @Tip_Estado=Tip_Estado,@Num_Actual=Num_Registro_Historial FROM dbo.LG_PCP_ANALITICAS_ENVIO WITH(UPDLOCK,HOLDLOCK) WHERE Cod_Fabrica=@Cod_Fabrica AND Cod_OrdPro=@Cod_OrdPro AND Num_Envio=@Num_Envio;
 IF @Tip_Estado IS NULL OR @Tip_Estado=''S'' OR (@Tip_Estado=''A'' AND @Num_Registro_Historial IS NOT NULL AND @Num_Actual<>@Num_Registro_Historial)
 BEGIN RAISERROR(''El envío de PCP ya fue reemplazado o finalizado. Actualiza la bandeja.'',16,1);RETURN;END;
 IF @Num_Registro_Historial IS NOT NULL AND @Tip_Estado=''E''
 UPDATE dbo.LG_PCP_ANALITICAS_ENVIO SET Tip_Estado=''A'',Num_Registro_Historial=@Num_Registro_Historial,Cod_Usuario_Atencion=@Cod_Usuario,Fec_Atencion_UTC=GETUTCDATE() WHERE Num_Envio=@Num_Envio;',
 N'@Cod_Fabrica char(3),@Cod_OrdPro char(5),@Num_Envio bigint,@Num_Registro_Historial bigint,@Cod_Usuario varchar(100)',
 @Cod_Fabrica,@Cod_OrdPro,@Num_Envio,@Num_Registro_Historial,@Cod_Usuario;
END;
GO

