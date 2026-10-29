/* Actualizacion del historial existente: conservar la ruta de imagen de cada nuevo guardado.
   Ejecutar en la base de Gestion usada por la API, antes de publicar el backend actualizado.
   Los registros anteriores quedan con ImagePath NULL: no se les asigna la ruta actual.
   No se modifica ES_EstPro ni las imagenes. Compatible con SQL Server 2008 R2.
*/
IF OBJECT_ID('dbo.LG_ANALISIS_RB_HISTORIAL','U') IS NULL
BEGIN
 RAISERROR('Ejecuta primero 01_LG_ANALISIS_RB_HISTORIAL_Tabla.sql.',16,1);
 SET NOEXEC ON;
END;
GO
IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','ImagePath') IS NULL
 ALTER TABLE dbo.LG_ANALISIS_RB_HISTORIAL ADD ImagePath nvarchar(1024) NULL;
GO
/* CRUD versionado. Ejecutar DESPUÉS de 01_LG_ANALISIS_RB_HISTORIAL_Tabla.sql.
 I0001: nueva foto. U0001: nueva revisión, conserva la anterior.
 D0001: nueva revisión marcada D; NO borra el historial.
 S0001: por NP/fábrica; @Id permite consultar cualquier revisión.
 La API obtiene GestionUser de la sesion autenticada; en uso administrativo indicar el usuario responsable.
 RequestId se reutiliza SOLO al reintentar la misma operación; impide duplicados.
 SnapshotJson lo genera la API: SQL Server 2008 no interpreta JSON.
 No ejecutar guardados de ejemplo en producción.
*/
IF OBJECT_ID('dbo.PA_LG_ANALISIS_RB_HISTORIAL_I0001','P') IS NULL EXEC('CREATE PROCEDURE dbo.PA_LG_ANALISIS_RB_HISTORIAL_I0001 AS RETURN 0');
GO
ALTER PROCEDURE dbo.PA_LG_ANALISIS_RB_HISTORIAL_I0001
 @RequestId uniqueidentifier,
 @Factory char(3), @Np char(5), @ProgrammedQuantity int,
 @CommercialPrice decimal(18,4), @SewingTime decimal(18,6), @UsedTime decimal(18,6),
 @ServicePrice decimal(18,6), @OtherExpenses decimal(18,6),
 @SnapshotJson nvarchar(max), @RuleVersion varchar(40), @GestionUser varchar(30), @TraceId varchar(100),
 @PreviousId bigint = NULL, @Action char(1) = 'I', @ImagePath nvarchar(1024) = NULL
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @Action NOT IN ('I','U','D') BEGIN RAISERROR('Accion de historial invalida.',16,1); RETURN; END;
 IF @RequestId IS NULL OR LEN(LTRIM(RTRIM(@SnapshotJson)))=0 OR LEN(LTRIM(RTRIM(@GestionUser)))=0
 BEGIN RAISERROR('Completa la foto del analisis y el usuario.',16,1); RETURN; END;

 DECLARE @RootId uniqueidentifier, @Revision int, @Id bigint, @OldNp char(5), @OldFactory char(3), @OldAction char(1);
 BEGIN TRY
  BEGIN TRANSACTION;
  SELECT @Id=Id FROM dbo.LG_ANALISIS_RB_HISTORIAL WITH(UPDLOCK,HOLDLOCK) WHERE RequestId=@RequestId;
  IF @Id IS NOT NULL
  BEGIN
   IF NOT EXISTS(SELECT 1 FROM dbo.LG_ANALISIS_RB_HISTORIAL WHERE Id=@Id AND Factory=@Factory AND Np=@Np AND Action=@Action AND ISNULL(PreviousId,0)=ISNULL(@PreviousId,0) AND GestionUser=@GestionUser AND ISNULL(ImagePath,N'')=ISNULL(@ImagePath,N'') AND SnapshotJson COLLATE Latin1_General_BIN2=@SnapshotJson COLLATE Latin1_General_BIN2)
   BEGIN RAISERROR('La clave de reintento pertenece a otra operacion.',16,1); END;
  END
  ELSE
  BEGIN
   IF @Action='I'
   BEGIN
    IF @PreviousId IS NOT NULL BEGIN RAISERROR('Un guardado nuevo no puede reemplazar una revision.',16,1); END;
    SET @RootId=NEWID(); SET @Revision=1;
   END
   ELSE
   BEGIN
    SELECT @RootId=RootId,@Revision=Revision+1,@OldNp=Np,@OldFactory=Factory,@OldAction=Action
    FROM dbo.LG_ANALISIS_RB_HISTORIAL WITH(UPDLOCK,HOLDLOCK) WHERE Id=@PreviousId;
    IF @RootId IS NULL OR @OldNp<>@Np OR @OldFactory<>@Factory OR @OldAction='D'
       OR EXISTS(SELECT 1 FROM dbo.LG_ANALISIS_RB_HISTORIAL WITH(UPDLOCK,HOLDLOCK) WHERE PreviousId=@PreviousId)
    BEGIN RAISERROR('La revision ya cambio o fue eliminada. Consulta el historial.',16,1); END;
   END;
   INSERT dbo.LG_ANALISIS_RB_HISTORIAL (RootId,Revision,PreviousId,Action,RequestId,Factory,Np,ProgrammedQuantity,CommercialPrice,SewingTime,UsedTime,ServicePrice,OtherExpenses,ImagePath,SnapshotJson,RuleVersion,GestionUser,TraceId)
   VALUES (@RootId,@Revision,@PreviousId,@Action,@RequestId,@Factory,@Np,@ProgrammedQuantity,@CommercialPrice,@SewingTime,@UsedTime,@ServicePrice,@OtherExpenses,@ImagePath,@SnapshotJson,@RuleVersion,@GestionUser,@TraceId);
   SET @Id=SCOPE_IDENTITY();
  END;
  COMMIT TRANSACTION;
  SELECT Id,RootId,Revision,PreviousId,Action,Factory,Np,ProgrammedQuantity,CommercialPrice,SewingTime,UsedTime,ServicePrice,OtherExpenses,RuleVersion,GestionUser,SavedAtUtc,TraceId,ImagePath,SnapshotJson
  FROM dbo.LG_ANALISIS_RB_HISTORIAL WHERE Id=@Id;
 END TRY
 BEGIN CATCH
  IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
  DECLARE @Message nvarchar(4000); SET @Message=ERROR_MESSAGE(); RAISERROR(@Message,16,1);
 END CATCH;
END;
GO

IF OBJECT_ID('dbo.PA_LG_ANALISIS_RB_HISTORIAL_U0001','P') IS NULL EXEC('CREATE PROCEDURE dbo.PA_LG_ANALISIS_RB_HISTORIAL_U0001 AS RETURN 0');
GO
ALTER PROCEDURE dbo.PA_LG_ANALISIS_RB_HISTORIAL_U0001
 @RequestId uniqueidentifier,
 @Factory char(3), @Np char(5), @ProgrammedQuantity int,
 @CommercialPrice decimal(18,4), @SewingTime decimal(18,6), @UsedTime decimal(18,6),
 @ServicePrice decimal(18,6), @OtherExpenses decimal(18,6),
 @SnapshotJson nvarchar(max), @RuleVersion varchar(40), @GestionUser varchar(30), @TraceId varchar(100), @Id bigint, @ImagePath nvarchar(1024) = NULL
AS
BEGIN
 SET NOCOUNT ON;
 EXEC dbo.PA_LG_ANALISIS_RB_HISTORIAL_I0001 @RequestId=@RequestId,@Factory=@Factory,@Np=@Np,@ProgrammedQuantity=@ProgrammedQuantity,
 @CommercialPrice=@CommercialPrice,@SewingTime=@SewingTime,@UsedTime=@UsedTime,@ServicePrice=@ServicePrice,
 @OtherExpenses=@OtherExpenses,@SnapshotJson=@SnapshotJson,@RuleVersion=@RuleVersion,@GestionUser=@GestionUser,@TraceId=@TraceId,@PreviousId=@Id,@Action='U',@ImagePath=@ImagePath;
END;
GO

IF OBJECT_ID('dbo.PA_LG_ANALISIS_RB_HISTORIAL_D0001','P') IS NULL EXEC('CREATE PROCEDURE dbo.PA_LG_ANALISIS_RB_HISTORIAL_D0001 AS RETURN 0');
GO
ALTER PROCEDURE dbo.PA_LG_ANALISIS_RB_HISTORIAL_D0001
 @Id bigint,@RequestId uniqueidentifier,@GestionUser varchar(30),@TraceId varchar(100)
AS
BEGIN
 SET NOCOUNT ON;
 DECLARE @Factory char(3),@Np char(5),@ProgrammedQuantity int,@CommercialPrice decimal(18,4),
 @SewingTime decimal(18,6),@UsedTime decimal(18,6),@ServicePrice decimal(18,6),@OtherExpenses decimal(18,6),
 @SnapshotJson nvarchar(max),@RuleVersion varchar(40),@ImagePath nvarchar(1024);
 SELECT @Factory=Factory,@Np=Np,@ProgrammedQuantity=ProgrammedQuantity,@CommercialPrice=CommercialPrice,
 @SewingTime=SewingTime,@UsedTime=UsedTime,@ServicePrice=ServicePrice,@OtherExpenses=OtherExpenses,
 @SnapshotJson=SnapshotJson,@RuleVersion=RuleVersion,@ImagePath=ImagePath
 FROM dbo.LG_ANALISIS_RB_HISTORIAL WHERE Id=@Id;
 IF @Np IS NULL BEGIN RAISERROR('El analisis no existe.',16,1); RETURN; END;
 EXEC dbo.PA_LG_ANALISIS_RB_HISTORIAL_I0001 @RequestId=@RequestId,@Factory=@Factory,@Np=@Np,@ProgrammedQuantity=@ProgrammedQuantity,
 @CommercialPrice=@CommercialPrice,@SewingTime=@SewingTime,@UsedTime=@UsedTime,@ServicePrice=@ServicePrice,
 @OtherExpenses=@OtherExpenses,@SnapshotJson=@SnapshotJson,@RuleVersion=@RuleVersion,@GestionUser=@GestionUser,@TraceId=@TraceId,@PreviousId=@Id,@Action='D',@ImagePath=@ImagePath;
END;
GO

IF OBJECT_ID('dbo.PA_LG_ANALISIS_RB_HISTORIAL_S0001','P') IS NULL EXEC('CREATE PROCEDURE dbo.PA_LG_ANALISIS_RB_HISTORIAL_S0001 AS RETURN 0');
GO
ALTER PROCEDURE dbo.PA_LG_ANALISIS_RB_HISTORIAL_S0001
 @Factory char(3),@Np char(5),@Id bigint=NULL,@Offset int=0,@PageSize int=50
AS
BEGIN
 SET NOCOUNT ON;
 IF @Offset<0 OR @PageSize<1 OR @PageSize>100 BEGIN RAISERROR('Paginacion invalida.',16,1); RETURN; END;
 SELECT COUNT(*) AS TotalRows FROM dbo.LG_ANALISIS_RB_HISTORIAL WHERE Factory=@Factory AND Np=@Np AND (@Id IS NULL OR Id=@Id);
 ;WITH History AS (
  SELECT *,ROW_NUMBER() OVER(ORDER BY SavedAtUtc DESC,Id DESC) AS RowNum
  FROM dbo.LG_ANALISIS_RB_HISTORIAL WHERE Factory=@Factory AND Np=@Np AND (@Id IS NULL OR Id=@Id)
 )
 SELECT Id,RootId,Revision,PreviousId,Action,Factory,Np,ProgrammedQuantity,CommercialPrice,SewingTime,
 UsedTime,ServicePrice,OtherExpenses,RuleVersion,GestionUser,SavedAtUtc,TraceId,ImagePath,
 CASE WHEN @Id IS NOT NULL THEN SnapshotJson ELSE NULL END AS SnapshotJson
 FROM History WHERE RowNum>@Offset AND RowNum<=@Offset+@PageSize ORDER BY RowNum;
END;
GO


/* EJEMPLOS DE CONSULTA (descomentar y ajustar NP/Id en la base correcta):
EXEC dbo.PA_LG_ANALISIS_RB_HISTORIAL_S0001 @Factory='001', @Np='J1825';
EXEC dbo.PA_LG_ANALISIS_RB_HISTORIAL_S0001 @Factory='001', @Np='J1825', @Id=42;
SELECT Id,Np,Revision,PreviousId,Action,ProgrammedQuantity,CommercialPrice,SewingTime,
       UsedTime,ServicePrice,OtherExpenses,GestionUser,SavedAtUtc,SnapshotJson
FROM dbo.LG_ANALISIS_RB_HISTORIAL
WHERE Factory='001' AND Np='J1825'
ORDER BY SavedAtUtc DESC,Id DESC;

I0001 recibe la copia completa generada por la API y devuelve el registro insertado.
U0001 recibe los mismos parametros mas @Id (revision anterior).
D0001 recibe @Id, @RequestId, @GestionUser, @TraceId.
Para cada operacion nueva generar un RequestId con NEWID(); reutilizarlo solo en el mismo reintento.
*/

SET NOEXEC OFF;
GO
