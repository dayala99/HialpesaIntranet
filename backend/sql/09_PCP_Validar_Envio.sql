/* Ejecutar en HIALPESA antes de actualizar Back.
 Bloquea envíos si falta precio > 0, tarifado D o ficha técnica SI.
 No modifica tablas ni datos. Mantiene reintentos de solicitudes ya registradas. */
SET ANSI_NULLS ON;SET QUOTED_IDENTIFIER ON;SET ARITHABORT ON;
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
