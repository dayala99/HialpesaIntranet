/* Ejecutar en HIALPESA. Corrección de escrituras sobre índice filtrado en compatibilidad 80.
 Actualiza solamente I0001 y U0001. No cambia tablas, índices ni datos de negocio.
 Conserva parámetros, validaciones, reintentos y transacciones. SQL permanece dentro del SP. */
SET ANSI_NULLS ON;SET QUOTED_IDENTIFIER ON;SET ARITHABORT ON;
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
