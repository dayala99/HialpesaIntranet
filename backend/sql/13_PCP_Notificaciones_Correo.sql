/* Ejecutar en HIALPESA. SQL Server 2008 R2, compatibilidad 80.
 Solo objetos propios PCP. No envía correos históricos al instalarse.
 Database Mail: perfil GestionAutomaticos; destinatario temporal dayalad@hialpesa.com. */
SET ANSI_NULLS ON;SET QUOTED_IDENTIFIER ON;SET ARITHABORT ON;
GO
IF OBJECT_ID('dbo.LG_PCP_CORREO_ENVIO','U') IS NULL
BEGIN
 CREATE TABLE dbo.LG_PCP_CORREO_ENVIO(
 Num_Envio bigint NOT NULL CONSTRAINT PK_LG_PCP_CORREO_ENVIO PRIMARY KEY,
 Txt_Destinatarios varchar(2000) NOT NULL,Des_Perfil varchar(128) NOT NULL,
 Des_Asunto nvarchar(255) NOT NULL,Txt_Cuerpo nvarchar(max) NOT NULL,
 Tip_Estado char(1) NOT NULL CONSTRAINT DF_LG_PCP_CORREO_ESTADO DEFAULT 'P',
 Num_Intentos int NOT NULL CONSTRAINT DF_LG_PCP_CORREO_INTENTOS DEFAULT 0,
 Num_Correo_SQL int NULL,Txt_Error nvarchar(2000) NULL,
 Fec_Creacion_UTC datetime NOT NULL,Fec_Proximo_Intento_UTC datetime NOT NULL,Fec_Ultimo_Intento_UTC datetime NULL,
 CONSTRAINT FK_LG_PCP_CORREO_ENVIO FOREIGN KEY(Num_Envio) REFERENCES dbo.LG_PCP_ANALITICAS_ENVIO(Num_Envio),
 CONSTRAINT CK_LG_PCP_CORREO_ESTADO CHECK(Tip_Estado IN('P','Q','F'))
 );
 CREATE INDEX IX_LG_PCP_CORREO_PENDIENTE ON dbo.LG_PCP_CORREO_ENVIO(Tip_Estado,Fec_Proximo_Intento_UTC);
END;
GO
IF OBJECT_ID('dbo.PA_LG_PCP_CORREO_I0001','P') IS NULL EXEC('CREATE PROCEDURE dbo.PA_LG_PCP_CORREO_I0001 AS RETURN 0');
GO
ALTER PROCEDURE dbo.PA_LG_PCP_CORREO_I0001 @Num_Envio bigint
AS
BEGIN
 SET NOCOUNT ON;
 IF @@TRANCOUNT=0 BEGIN RAISERROR('La notificación requiere la transacción del envío PCP.',16,1);RETURN;END;
 IF EXISTS(SELECT 1 FROM dbo.LG_PCP_CORREO_ENVIO WITH(UPDLOCK,HOLDLOCK) WHERE Num_Envio=@Num_Envio) RETURN;
 DECLARE @Cod_OrdPro char(5),@Des_Cliente nvarchar(500),@Num_Programada int,@Num_Requerida int,@Des_Linea nvarchar(max),@Des_Costura nvarchar(20),@Cod_Sector varchar(10),@Des_Usuario varchar(100),@Txt_Cuerpo nvarchar(max),@Des_Asunto nvarchar(255);
 SELECT @Cod_OrdPro=sent.Cod_OrdPro,@Cod_Sector=sent.Cod_Sector,@Des_Usuario=sent.Cod_Usuario_Envio,
 @Des_Costura=CASE sent.Tip_Costura WHEN 'C' THEN N'Completa' ELSE N'Parcial' END,
 @Des_Cliente=NULLIF(RTRIM(customer.Nom_Cliente),''),@Num_Programada=op.Num_PrenPro,@Num_Requerida=op.Num_PrenReq,
 @Des_Linea=STUFF((SELECT DISTINCT N' / '+RTRIM(line.Descripcion) FROM dbo.PL_TRABAJOS work INNER JOIN dbo.CF_Lineas_Produccion line ON line.Cod_LinPro=work.Cod_LinPro WHERE work.Cod_OrdPro=sent.Cod_OrdPro FOR XML PATH(''),TYPE).value('.','nvarchar(max)'),1,3,N'')
 FROM dbo.LG_PCP_ANALITICAS_ENVIO sent LEFT JOIN dbo.ES_ORDPRO op ON op.Cod_Fabrica=sent.Cod_Fabrica AND op.Cod_OrdPro=sent.Cod_OrdPro
 LEFT JOIN dbo.TG_CLIENTE customer ON customer.Cod_Cliente=op.Cod_Cliente WHERE sent.Num_Envio=@Num_Envio;
 IF @Cod_OrdPro IS NULL BEGIN RAISERROR('El envío PCP no existe.',16,1);RETURN;END;
 SET @Des_Asunto=N'PCP - NP '+RTRIM(@Cod_OrdPro)+N' enviada a Logística - Costura '+@Des_Costura;
 SET @Txt_Cuerpo=N'Se ha enviado la siguiente NP a Logística:'+CHAR(13)+CHAR(10)+CHAR(13)+CHAR(10)+
 N'NP: '+RTRIM(@Cod_OrdPro)+CHAR(13)+CHAR(10)+
 N'Cliente: '+ISNULL(@Des_Cliente,N'Sin dato')+CHAR(13)+CHAR(10)+
 N'Cantidad programada: '+ISNULL(CONVERT(nvarchar(30),@Num_Programada),N'Sin dato')+CHAR(13)+CHAR(10)+
 N'Cantidad requerida: '+ISNULL(CONVERT(nvarchar(30),@Num_Requerida),N'Sin dato')+CHAR(13)+CHAR(10)+
 N'Nombre Línea / Servicio: '+ISNULL(NULLIF(@Des_Linea,N''),N'Sin dato')+CHAR(13)+CHAR(10)+
 N'Costura: '+@Des_Costura+CHAR(13)+CHAR(10)+CHAR(13)+CHAR(10)+
 N'Sector: '+@Cod_Sector+CHAR(13)+CHAR(10)+N'Enviado por: '+@Des_Usuario+CHAR(13)+CHAR(10)+N'Número de envío: '+CONVERT(nvarchar(30),@Num_Envio);
 INSERT dbo.LG_PCP_CORREO_ENVIO(Num_Envio,Txt_Destinatarios,Des_Perfil,Des_Asunto,Txt_Cuerpo,Fec_Creacion_UTC,Fec_Proximo_Intento_UTC)
 VALUES(@Num_Envio,'dayalad@hialpesa.com','GestionAutomaticos',@Des_Asunto,@Txt_Cuerpo,GETUTCDATE(),GETUTCDATE());
END;
GO
IF OBJECT_ID('dbo.PA_LG_PCP_CORREO_U0001','P') IS NULL EXEC('CREATE PROCEDURE dbo.PA_LG_PCP_CORREO_U0001 AS RETURN 0');
GO
ALTER PROCEDURE dbo.PA_LG_PCP_CORREO_U0001
AS
BEGIN
 SET NOCOUNT ON;SET XACT_ABORT OFF;
 -- Database Mail conserva su propio estado de entrega. Reintentar solo fallidos,
 -- nunca aquellos que siguen en cola o en reintento del servidor de correo.
 UPDATE own SET Tip_Estado='F',Txt_Error=N'Database Mail no entregó el correo tras cinco intentos. Corregir el servicio y reintentar esta notificación.'
 FROM dbo.LG_PCP_CORREO_ENVIO own INNER JOIN msdb.dbo.sysmail_allitems mail ON mail.mailitem_id=own.Num_Correo_SQL
 WHERE own.Tip_Estado='Q' AND own.Num_Intentos>=5 AND mail.sent_status='failed';
 IF @@ROWCOUNT>0 BEGIN SELECT CAST(1 AS bit) AS Processed,CAST(0 AS bit) AS Queued,N'Hay notificaciones de PCP sin entregar tras cinco intentos. Revisar PA_LG_PCP_CORREO_S0001 y el servicio Database Mail.' AS Error;RETURN;END;
 DECLARE @Num_Envio bigint,@Num_Intentos int,@Txt_Destinatarios varchar(2000),@Des_Perfil varchar(128),@Des_Asunto nvarchar(255),@Txt_Cuerpo nvarchar(max),@Num_Correo_SQL int,@Txt_Error nvarchar(2000);
 BEGIN TRY
 BEGIN TRANSACTION;
 SELECT TOP 1 @Num_Envio=own.Num_Envio,@Num_Intentos=own.Num_Intentos,@Txt_Destinatarios=own.Txt_Destinatarios,@Des_Perfil=own.Des_Perfil,@Des_Asunto=own.Des_Asunto,@Txt_Cuerpo=own.Txt_Cuerpo
 FROM dbo.LG_PCP_CORREO_ENVIO own WITH(UPDLOCK,READPAST,ROWLOCK)
 WHERE own.Num_Intentos<5 AND own.Fec_Proximo_Intento_UTC<=GETUTCDATE() AND
 (own.Tip_Estado='P' OR (own.Tip_Estado='Q' AND EXISTS(SELECT 1 FROM msdb.dbo.sysmail_allitems mail WHERE mail.mailitem_id=own.Num_Correo_SQL AND mail.sent_status='failed')))
 ORDER BY own.Fec_Proximo_Intento_UTC,own.Num_Envio;
 IF @Num_Envio IS NULL BEGIN COMMIT TRANSACTION;SELECT CAST(0 AS bit) AS Processed,CAST(0 AS bit) AS Queued,CAST(NULL AS nvarchar(2000)) AS Error;RETURN;END;
 EXEC msdb.dbo.sp_send_dbmail @profile_name=@Des_Perfil,@recipients=@Txt_Destinatarios,@subject=@Des_Asunto,@body=@Txt_Cuerpo,@body_format='TEXT',@mailitem_id=@Num_Correo_SQL OUTPUT;
 IF @Num_Correo_SQL IS NULL RAISERROR('Database Mail no devolvió un identificador de correo.',16,1);
 UPDATE dbo.LG_PCP_CORREO_ENVIO SET Tip_Estado='Q',Num_Intentos=Num_Intentos+1,Num_Correo_SQL=@Num_Correo_SQL,Txt_Error=NULL,Fec_Ultimo_Intento_UTC=GETUTCDATE(),Fec_Proximo_Intento_UTC=DATEADD(minute,5,GETUTCDATE()) WHERE Num_Envio=@Num_Envio;
 COMMIT TRANSACTION;
 SELECT CAST(1 AS bit) AS Processed,CAST(1 AS bit) AS Queued,CAST(NULL AS nvarchar(2000)) AS Error;
 END TRY
 BEGIN CATCH
 SET @Txt_Error=ERROR_MESSAGE();IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
 IF @Num_Envio IS NULL RAISERROR(@Txt_Error,16,1);
 ELSE BEGIN
 UPDATE dbo.LG_PCP_CORREO_ENVIO SET Num_Intentos=Num_Intentos+1,Tip_Estado=CASE WHEN Num_Intentos+1>=5 THEN 'F' ELSE 'P' END,Txt_Error=@Txt_Error,Fec_Ultimo_Intento_UTC=GETUTCDATE(),Fec_Proximo_Intento_UTC=DATEADD(minute,5,GETUTCDATE()) WHERE Num_Envio=@Num_Envio AND Num_Intentos=@Num_Intentos;
 SELECT CAST(1 AS bit) AS Processed,CAST(0 AS bit) AS Queued,@Txt_Error AS Error;
 END;
 END CATCH;
END;
GO
IF OBJECT_ID('dbo.PA_LG_PCP_CORREO_S0001','P') IS NULL EXEC('CREATE PROCEDURE dbo.PA_LG_PCP_CORREO_S0001 AS RETURN 0');
GO
ALTER PROCEDURE dbo.PA_LG_PCP_CORREO_S0001 @Num_Envio bigint=NULL
AS
BEGIN
 SET NOCOUNT ON;
 SELECT TOP 200 own.Num_Envio,sent.Cod_OrdPro,own.Txt_Destinatarios,own.Des_Asunto,own.Tip_Estado,own.Num_Intentos,own.Num_Correo_SQL,
 mail.sent_status AS Des_Estado_Entrega,own.Txt_Error,own.Fec_Creacion_UTC,own.Fec_Ultimo_Intento_UTC
 FROM dbo.LG_PCP_CORREO_ENVIO own INNER JOIN dbo.LG_PCP_ANALITICAS_ENVIO sent ON sent.Num_Envio=own.Num_Envio
 LEFT JOIN msdb.dbo.sysmail_allitems mail ON mail.mailitem_id=own.Num_Correo_SQL
 WHERE @Num_Envio IS NULL OR own.Num_Envio=@Num_Envio ORDER BY own.Num_Envio DESC;
END;
GO
IF OBJECT_ID('dbo.PA_LG_PCP_CORREO_U0002','P') IS NULL EXEC('CREATE PROCEDURE dbo.PA_LG_PCP_CORREO_U0002 AS RETURN 0');
GO
ALTER PROCEDURE dbo.PA_LG_PCP_CORREO_U0002 @Num_Envio bigint
AS
BEGIN
 SET NOCOUNT ON;
 -- Reintento manual SOLO tras corregir el servicio. No duplica entregados o en cola.
 UPDATE own SET Tip_Estado='P',Num_Intentos=0,Txt_Error=NULL,Fec_Proximo_Intento_UTC=GETUTCDATE()
 FROM dbo.LG_PCP_CORREO_ENVIO own LEFT JOIN msdb.dbo.sysmail_allitems mail ON mail.mailitem_id=own.Num_Correo_SQL
 WHERE own.Num_Envio=@Num_Envio AND (own.Tip_Estado='F' OR mail.sent_status='failed');
END;
GO

-- Mantiene validaciones, idempotencia y sintaxis de compatibilidad 80.
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
 DECLARE @Num_Nuevo_Envio bigint;SET @Num_Nuevo_Envio=CONVERT(bigint,SCOPE_IDENTITY());
 EXEC dbo.PA_LG_PCP_CORREO_I0001 @Num_Envio=@Num_Nuevo_Envio;
 SELECT * FROM dbo.LG_PCP_ANALITICAS_ENVIO WHERE Num_Envio=@Num_Nuevo_Envio;
 COMMIT TRANSACTION;
 END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;DECLARE @Des_Error nvarchar(2048);SET @Des_Error=ERROR_MESSAGE();RAISERROR(@Des_Error,16,1);END CATCH;',
 N'@Cod_Solicitud uniqueidentifier,@Cod_Fabrica char(3),@Cod_OrdPro char(5),@Cod_Sector varchar(10),@Cod_Tarifado char(5),@Cod_Variante char(1),
 @Num_Operaciones_Total int,@Num_Operaciones_Seleccionadas int,@Min_Total decimal(18,6),@Min_Seleccionados decimal(18,6),@Txt_Seleccion nvarchar(max),@Cod_Usuario varchar(100)',
 @Cod_Solicitud,@Cod_Fabrica,@Cod_OrdPro,@Cod_Sector,@Cod_Tarifado,@Cod_Variante,@Num_Operaciones_Total,@Num_Operaciones_Seleccionadas,@Min_Total,@Min_Seleccionados,@Txt_Seleccion,@Cod_Usuario;
END;
GO
