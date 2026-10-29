/* CRUD versionado. Ejecutar DESPUÉS de 01_LG_ANALISIS_RB_HISTORIAL_Tabla.sql.
 I0001: nueva foto. U0001: nueva revisión, conserva la anterior.
 D0001: nueva revisión marcada D; NO borra el historial.
 S0001: por NP/fábrica; @Num_Registro permite consultar cualquier revisión.
 La API obtiene Cod_Usuario de la sesion autenticada; en uso administrativo indicar el usuario responsable.
 Cod_Solicitud se reutiliza SOLO al reintentar la misma operación; impide duplicados.
 Txt_Datos_Analisis lo genera la API: SQL Server 2008 no interpreta JSON.
 No ejecutar guardados de ejemplo en producción.
*/
IF OBJECT_ID('dbo.PA_LG_ANALISIS_RB_HISTORIAL_I0001','P') IS NULL EXEC('CREATE PROCEDURE dbo.PA_LG_ANALISIS_RB_HISTORIAL_I0001 AS RETURN 0');
GO
ALTER PROCEDURE dbo.PA_LG_ANALISIS_RB_HISTORIAL_I0001
 @Cod_Solicitud uniqueidentifier,
 @Cod_Fabrica char(3), @Cod_OrdPro char(5), @Num_Prendas_Programadas int,
 @Imp_Precio_Comercial decimal(18,4), @Min_Costura decimal(18,6), @Min_Considerados decimal(18,6),
 @Imp_Precio_Servicio decimal(18,6), @Imp_Otros_Gastos decimal(18,6),
 @Txt_Datos_Analisis nvarchar(max), @Cod_Version_Reglas varchar(40), @Cod_Usuario varchar(30), @Cod_Traza varchar(100),
 @Num_Registro_Anterior bigint = NULL, @Tip_Accion char(1) = 'I', @Des_Ruta_Imagen nvarchar(1024) = NULL
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @Tip_Accion NOT IN ('I','U','D') BEGIN RAISERROR('Accion de historial invalida.',16,1); RETURN; END;
 IF @Cod_Solicitud IS NULL OR LEN(LTRIM(RTRIM(@Txt_Datos_Analisis)))=0 OR LEN(LTRIM(RTRIM(@Cod_Usuario)))=0
 BEGIN RAISERROR('Completa la foto del analisis y el usuario.',16,1); RETURN; END;

 DECLARE @Cod_Grupo_Historial uniqueidentifier, @Num_Version int, @Num_Registro bigint, @Cod_OrdPro_Anterior char(5), @Cod_Fabrica_Anterior char(3), @Tip_Accion_Anterior char(1);
 BEGIN TRY
  BEGIN TRANSACTION;
  SELECT @Num_Registro=Num_Registro FROM dbo.LG_ANALISIS_RB_HISTORIAL WITH(UPDLOCK,HOLDLOCK) WHERE Cod_Solicitud=@Cod_Solicitud;
  IF @Num_Registro IS NOT NULL
  BEGIN
   IF NOT EXISTS(SELECT 1 FROM dbo.LG_ANALISIS_RB_HISTORIAL WHERE Num_Registro=@Num_Registro AND Cod_Fabrica=@Cod_Fabrica AND Cod_OrdPro=@Cod_OrdPro AND Tip_Accion=@Tip_Accion AND ISNULL(Num_Registro_Anterior,0)=ISNULL(@Num_Registro_Anterior,0) AND Cod_Usuario=@Cod_Usuario AND ISNULL(Des_Ruta_Imagen,N'')=ISNULL(@Des_Ruta_Imagen,N'') AND Txt_Datos_Analisis COLLATE Latin1_General_BIN2=@Txt_Datos_Analisis COLLATE Latin1_General_BIN2)
   BEGIN RAISERROR('La clave de reintento pertenece a otra operacion.',16,1); END;
  END
  ELSE
  BEGIN
   IF @Tip_Accion='I'
   BEGIN
    IF @Num_Registro_Anterior IS NOT NULL BEGIN RAISERROR('Un guardado nuevo no puede reemplazar una revision.',16,1); END;
    SET @Cod_Grupo_Historial=NEWID(); SET @Num_Version=1;
   END
   ELSE
   BEGIN
    SELECT @Cod_Grupo_Historial=Cod_Grupo_Historial,@Num_Version=Num_Version+1,@Cod_OrdPro_Anterior=Cod_OrdPro,@Cod_Fabrica_Anterior=Cod_Fabrica,@Tip_Accion_Anterior=Tip_Accion
    FROM dbo.LG_ANALISIS_RB_HISTORIAL WITH(UPDLOCK,HOLDLOCK) WHERE Num_Registro=@Num_Registro_Anterior;
    IF @Cod_Grupo_Historial IS NULL OR @Cod_OrdPro_Anterior<>@Cod_OrdPro OR @Cod_Fabrica_Anterior<>@Cod_Fabrica OR @Tip_Accion_Anterior='D'
       OR EXISTS(SELECT 1 FROM dbo.LG_ANALISIS_RB_HISTORIAL WITH(UPDLOCK,HOLDLOCK) WHERE Num_Registro_Anterior=@Num_Registro_Anterior)
    BEGIN RAISERROR('La revision ya cambio o fue eliminada. Consulta el historial.',16,1); END;
   END;
   INSERT dbo.LG_ANALISIS_RB_HISTORIAL (Cod_Grupo_Historial,Num_Version,Num_Registro_Anterior,Tip_Accion,Cod_Solicitud,Cod_Fabrica,Cod_OrdPro,Num_Prendas_Programadas,Imp_Precio_Comercial,Min_Costura,Min_Considerados,Imp_Precio_Servicio,Imp_Otros_Gastos,Des_Ruta_Imagen,Txt_Datos_Analisis,Cod_Version_Reglas,Cod_Usuario,Cod_Traza)
   VALUES (@Cod_Grupo_Historial,@Num_Version,@Num_Registro_Anterior,@Tip_Accion,@Cod_Solicitud,@Cod_Fabrica,@Cod_OrdPro,@Num_Prendas_Programadas,@Imp_Precio_Comercial,@Min_Costura,@Min_Considerados,@Imp_Precio_Servicio,@Imp_Otros_Gastos,@Des_Ruta_Imagen,@Txt_Datos_Analisis,@Cod_Version_Reglas,@Cod_Usuario,@Cod_Traza);
   SET @Num_Registro=SCOPE_IDENTITY();
  END;
  COMMIT TRANSACTION;
  SELECT Num_Registro,Cod_Grupo_Historial,Num_Version,Num_Registro_Anterior,Tip_Accion,Cod_Fabrica,Cod_OrdPro,Num_Prendas_Programadas,Imp_Precio_Comercial,Min_Costura,Min_Considerados,Imp_Precio_Servicio,Imp_Otros_Gastos,Cod_Version_Reglas,Cod_Usuario,Fec_Registro_UTC,Cod_Traza,Des_Ruta_Imagen,Txt_Datos_Analisis
  FROM dbo.LG_ANALISIS_RB_HISTORIAL WHERE Num_Registro=@Num_Registro;
 END TRY
 BEGIN CATCH
  IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
  DECLARE @Des_Mensaje nvarchar(4000); SET @Des_Mensaje=ERROR_MESSAGE(); RAISERROR(@Des_Mensaje,16,1);
 END CATCH;
END;
GO

IF OBJECT_ID('dbo.PA_LG_ANALISIS_RB_HISTORIAL_U0001','P') IS NULL EXEC('CREATE PROCEDURE dbo.PA_LG_ANALISIS_RB_HISTORIAL_U0001 AS RETURN 0');
GO
ALTER PROCEDURE dbo.PA_LG_ANALISIS_RB_HISTORIAL_U0001
 @Cod_Solicitud uniqueidentifier,
 @Cod_Fabrica char(3), @Cod_OrdPro char(5), @Num_Prendas_Programadas int,
 @Imp_Precio_Comercial decimal(18,4), @Min_Costura decimal(18,6), @Min_Considerados decimal(18,6),
 @Imp_Precio_Servicio decimal(18,6), @Imp_Otros_Gastos decimal(18,6),
 @Txt_Datos_Analisis nvarchar(max), @Cod_Version_Reglas varchar(40), @Cod_Usuario varchar(30), @Cod_Traza varchar(100), @Num_Registro bigint, @Des_Ruta_Imagen nvarchar(1024) = NULL
AS
BEGIN
 SET NOCOUNT ON;
 EXEC dbo.PA_LG_ANALISIS_RB_HISTORIAL_I0001 @Cod_Solicitud=@Cod_Solicitud,@Cod_Fabrica=@Cod_Fabrica,@Cod_OrdPro=@Cod_OrdPro,@Num_Prendas_Programadas=@Num_Prendas_Programadas,
 @Imp_Precio_Comercial=@Imp_Precio_Comercial,@Min_Costura=@Min_Costura,@Min_Considerados=@Min_Considerados,@Imp_Precio_Servicio=@Imp_Precio_Servicio,
 @Imp_Otros_Gastos=@Imp_Otros_Gastos,@Txt_Datos_Analisis=@Txt_Datos_Analisis,@Cod_Version_Reglas=@Cod_Version_Reglas,@Cod_Usuario=@Cod_Usuario,@Cod_Traza=@Cod_Traza,@Num_Registro_Anterior=@Num_Registro,@Tip_Accion='U',@Des_Ruta_Imagen=@Des_Ruta_Imagen;
END;
GO

IF OBJECT_ID('dbo.PA_LG_ANALISIS_RB_HISTORIAL_D0001','P') IS NULL EXEC('CREATE PROCEDURE dbo.PA_LG_ANALISIS_RB_HISTORIAL_D0001 AS RETURN 0');
GO
ALTER PROCEDURE dbo.PA_LG_ANALISIS_RB_HISTORIAL_D0001
 @Num_Registro bigint,@Cod_Solicitud uniqueidentifier,@Cod_Usuario varchar(30),@Cod_Traza varchar(100)
AS
BEGIN
 SET NOCOUNT ON;
 DECLARE @Cod_Fabrica char(3),@Cod_OrdPro char(5),@Num_Prendas_Programadas int,@Imp_Precio_Comercial decimal(18,4),
 @Min_Costura decimal(18,6),@Min_Considerados decimal(18,6),@Imp_Precio_Servicio decimal(18,6),@Imp_Otros_Gastos decimal(18,6),
 @Txt_Datos_Analisis nvarchar(max),@Cod_Version_Reglas varchar(40),@Des_Ruta_Imagen nvarchar(1024);
 SELECT @Cod_Fabrica=Cod_Fabrica,@Cod_OrdPro=Cod_OrdPro,@Num_Prendas_Programadas=Num_Prendas_Programadas,@Imp_Precio_Comercial=Imp_Precio_Comercial,
 @Min_Costura=Min_Costura,@Min_Considerados=Min_Considerados,@Imp_Precio_Servicio=Imp_Precio_Servicio,@Imp_Otros_Gastos=Imp_Otros_Gastos,
 @Txt_Datos_Analisis=Txt_Datos_Analisis,@Cod_Version_Reglas=Cod_Version_Reglas,@Des_Ruta_Imagen=Des_Ruta_Imagen
 FROM dbo.LG_ANALISIS_RB_HISTORIAL WHERE Num_Registro=@Num_Registro;
 IF @Cod_OrdPro IS NULL BEGIN RAISERROR('El analisis no existe.',16,1); RETURN; END;
 EXEC dbo.PA_LG_ANALISIS_RB_HISTORIAL_I0001 @Cod_Solicitud=@Cod_Solicitud,@Cod_Fabrica=@Cod_Fabrica,@Cod_OrdPro=@Cod_OrdPro,@Num_Prendas_Programadas=@Num_Prendas_Programadas,
 @Imp_Precio_Comercial=@Imp_Precio_Comercial,@Min_Costura=@Min_Costura,@Min_Considerados=@Min_Considerados,@Imp_Precio_Servicio=@Imp_Precio_Servicio,
 @Imp_Otros_Gastos=@Imp_Otros_Gastos,@Txt_Datos_Analisis=@Txt_Datos_Analisis,@Cod_Version_Reglas=@Cod_Version_Reglas,@Cod_Usuario=@Cod_Usuario,@Cod_Traza=@Cod_Traza,@Num_Registro_Anterior=@Num_Registro,@Tip_Accion='D',@Des_Ruta_Imagen=@Des_Ruta_Imagen;
END;
GO

IF OBJECT_ID('dbo.PA_LG_ANALISIS_RB_HISTORIAL_S0001','P') IS NULL EXEC('CREATE PROCEDURE dbo.PA_LG_ANALISIS_RB_HISTORIAL_S0001 AS RETURN 0');
GO
ALTER PROCEDURE dbo.PA_LG_ANALISIS_RB_HISTORIAL_S0001
 @Cod_Fabrica char(3),@Cod_OrdPro char(5),@Num_Registro bigint=NULL,@Num_Desplazamiento int=0,@Num_Tamano_Pagina int=50
AS
BEGIN
 SET NOCOUNT ON;
 IF @Num_Desplazamiento<0 OR @Num_Tamano_Pagina<1 OR @Num_Tamano_Pagina>100 BEGIN RAISERROR('Paginacion invalida.',16,1); RETURN; END;
 SELECT COUNT(*) AS Num_Total_Registros FROM dbo.LG_ANALISIS_RB_HISTORIAL WHERE Cod_Fabrica=@Cod_Fabrica AND Cod_OrdPro=@Cod_OrdPro AND (@Num_Registro IS NULL OR Num_Registro=@Num_Registro);
 ;WITH Historial AS (
  SELECT *,ROW_NUMBER() OVER(ORDER BY Fec_Registro_UTC DESC,Num_Registro DESC) AS Num_Fila
  FROM dbo.LG_ANALISIS_RB_HISTORIAL WHERE Cod_Fabrica=@Cod_Fabrica AND Cod_OrdPro=@Cod_OrdPro AND (@Num_Registro IS NULL OR Num_Registro=@Num_Registro)
 )
 SELECT Num_Registro,Cod_Grupo_Historial,Num_Version,Num_Registro_Anterior,Tip_Accion,Cod_Fabrica,Cod_OrdPro,Num_Prendas_Programadas,Imp_Precio_Comercial,Min_Costura,
 Min_Considerados,Imp_Precio_Servicio,Imp_Otros_Gastos,Cod_Version_Reglas,Cod_Usuario,Fec_Registro_UTC,Cod_Traza,Des_Ruta_Imagen,
 CASE WHEN @Num_Registro IS NOT NULL THEN Txt_Datos_Analisis ELSE NULL END AS Txt_Datos_Analisis
 FROM Historial WHERE Num_Fila>@Num_Desplazamiento AND Num_Fila<=@Num_Desplazamiento+@Num_Tamano_Pagina ORDER BY Num_Fila;
END;
GO


/* EJEMPLOS DE CONSULTA (descomentar y ajustar NP/Num_Registro en la base correcta):
EXEC dbo.PA_LG_ANALISIS_RB_HISTORIAL_S0001 @Cod_Fabrica='001', @Cod_OrdPro='J1825';
EXEC dbo.PA_LG_ANALISIS_RB_HISTORIAL_S0001 @Cod_Fabrica='001', @Cod_OrdPro='J1825', @Num_Registro=42;
SELECT Num_Registro,Cod_OrdPro,Num_Version,Num_Registro_Anterior,Tip_Accion,Num_Prendas_Programadas,Imp_Precio_Comercial,Min_Costura,
       Min_Considerados,Imp_Precio_Servicio,Imp_Otros_Gastos,Cod_Usuario,Fec_Registro_UTC,Txt_Datos_Analisis
FROM dbo.LG_ANALISIS_RB_HISTORIAL
WHERE Cod_Fabrica='001' AND Cod_OrdPro='J1825'
ORDER BY Fec_Registro_UTC DESC,Num_Registro DESC;

I0001 recibe la copia completa generada por la API y devuelve el registro insertado.
U0001 recibe los mismos parametros mas @Num_Registro (revision anterior).
D0001 recibe @Num_Registro, @Cod_Solicitud, @Cod_Usuario, @Cod_Traza.
Para cada operacion nueva generar un Cod_Solicitud con NEWID(); reutilizarlo solo en el mismo reintento.
*/
