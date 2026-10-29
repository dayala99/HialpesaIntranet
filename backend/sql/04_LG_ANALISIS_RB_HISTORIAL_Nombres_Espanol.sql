/* MIGRACION 04 (corregida: CHECK antes de renombrar): columnas del historial en espanol con prefijos de tres letras.
   Para una instalacion existente ejecutar SOLO este archivo, en la base usada por la API.
   Conserva tabla, registros, identidad, GUIDs y contenido JSON (incluidos historicos antiguos).
   Actualiza los cuatro procedimientos dentro de la misma transaccion.
   El backend nuevo admite el esquema anterior y el nuevo: publicarlo antes de aplicar este SQL.
   Los scripts 01/02 son para instalaciones nuevas. No volver a ejecutar el antiguo 03 despues del 04.
   Compatible con SQL Server 2008 R2. Este archivo NO se ejecuto automaticamente.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
 IF OBJECT_ID('dbo.LG_ANALISIS_RB_HISTORIAL','U') IS NULL
 BEGIN RAISERROR('No existe la tabla de historial. Para instalacion nueva ejecutar 01 y 02.',16,1); END;
 BEGIN TRANSACTION;
 DECLARE @Num_Registros_Antes bigint,@Num_Registros_Despues bigint,@Num_Bytes_Antes bigint,@Num_Bytes_Despues bigint,
         @Des_Columna_Datos sysname,@Txt_Sql nvarchar(max);
 SELECT @Num_Registros_Antes=COUNT_BIG(*) FROM dbo.LG_ANALISIS_RB_HISTORIAL WITH(TABLOCKX,HOLDLOCK);
 SET @Des_Columna_Datos=CASE WHEN COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','SnapshotJson') IS NOT NULL THEN 'SnapshotJson' ELSE 'Txt_Datos_Analisis' END;
 SET @Txt_Sql=N'SELECT @Num_Bytes=ISNULL(SUM(CONVERT(bigint,DATALENGTH('+QUOTENAME(@Des_Columna_Datos)+N'))),0) FROM dbo.LG_ANALISIS_RB_HISTORIAL';
 EXEC sys.sp_executesql @Txt_Sql,N'@Num_Bytes bigint OUTPUT',@Num_Bytes=@Num_Bytes_Antes OUTPUT;

 -- Retirar CHECK ANTES de renombrar: SQL Server 2008 los considera dependencias forzadas.
 -- Se recrean y validan mas abajo; cualquier error restaura todo con ROLLBACK.
 EXEC(N'IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(''dbo.LG_ANALISIS_RB_HISTORIAL'') AND name=''CK_LG_ANALISIS_RB_ACTION'') ALTER TABLE dbo.LG_ANALISIS_RB_HISTORIAL DROP CONSTRAINT CK_LG_ANALISIS_RB_ACTION;');
 EXEC(N'IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(''dbo.LG_ANALISIS_RB_HISTORIAL'') AND name=''CK_LG_ANALISIS_RB_ACCION'') ALTER TABLE dbo.LG_ANALISIS_RB_HISTORIAL DROP CONSTRAINT CK_LG_ANALISIS_RB_ACCION;');
 EXEC(N'IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(''dbo.LG_ANALISIS_RB_HISTORIAL'') AND name=''CK_LG_ANALISIS_RB_REVISION'') ALTER TABLE dbo.LG_ANALISIS_RB_HISTORIAL DROP CONSTRAINT CK_LG_ANALISIS_RB_REVISION;');
 EXEC(N'IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(''dbo.LG_ANALISIS_RB_HISTORIAL'') AND name=''CK_LG_ANALISIS_RB_VERSION'') ALTER TABLE dbo.LG_ANALISIS_RB_HISTORIAL DROP CONSTRAINT CK_LG_ANALISIS_RB_VERSION;');
 EXEC(N'IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(''dbo.LG_ANALISIS_RB_HISTORIAL'') AND name=''CK_LG_ANALISIS_RB_SNAPSHOT'') ALTER TABLE dbo.LG_ANALISIS_RB_HISTORIAL DROP CONSTRAINT CK_LG_ANALISIS_RB_SNAPSHOT;');
 EXEC(N'IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(''dbo.LG_ANALISIS_RB_HISTORIAL'') AND name=''CK_LG_ANALISIS_RB_DATOS'') ALTER TABLE dbo.LG_ANALISIS_RB_HISTORIAL DROP CONSTRAINT CK_LG_ANALISIS_RB_DATOS;');
 EXEC(N'IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(''dbo.LG_ANALISIS_RB_HISTORIAL'') AND name=''CK_LG_ANALISIS_RB_TIME'') ALTER TABLE dbo.LG_ANALISIS_RB_HISTORIAL DROP CONSTRAINT CK_LG_ANALISIS_RB_TIME;');
 EXEC(N'IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(''dbo.LG_ANALISIS_RB_HISTORIAL'') AND name=''CK_LG_ANALISIS_RB_TIEMPOS'') ALTER TABLE dbo.LG_ANALISIS_RB_HISTORIAL DROP CONSTRAINT CK_LG_ANALISIS_RB_TIEMPOS;');
 EXEC(N'IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(''dbo.LG_ANALISIS_RB_HISTORIAL'') AND name=''CK_LG_ANALISIS_RB_POSITIVE'') ALTER TABLE dbo.LG_ANALISIS_RB_HISTORIAL DROP CONSTRAINT CK_LG_ANALISIS_RB_POSITIVE;');
 EXEC(N'IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(''dbo.LG_ANALISIS_RB_HISTORIAL'') AND name=''CK_LG_ANALISIS_RB_IMPORTES'') ALTER TABLE dbo.LG_ANALISIS_RB_HISTORIAL DROP CONSTRAINT CK_LG_ANALISIS_RB_IMPORTES;');

 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Id') IS NOT NULL AND COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Num_Registro') IS NOT NULL
 BEGIN RAISERROR('Existen ambas columnas: Id y Num_Registro. No se modifico la tabla.',16,1); END;
 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Id') IS NOT NULL
  EXEC sys.sp_rename N'dbo.LG_ANALISIS_RB_HISTORIAL.Id',N'Num_Registro',N'COLUMN';
 ELSE IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Num_Registro') IS NULL
 BEGIN RAISERROR('No se encontro la columna Id / Num_Registro.',16,1); END;

 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','RootId') IS NOT NULL AND COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Cod_Grupo_Historial') IS NOT NULL
 BEGIN RAISERROR('Existen ambas columnas: RootId y Cod_Grupo_Historial. No se modifico la tabla.',16,1); END;
 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','RootId') IS NOT NULL
  EXEC sys.sp_rename N'dbo.LG_ANALISIS_RB_HISTORIAL.RootId',N'Cod_Grupo_Historial',N'COLUMN';
 ELSE IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Cod_Grupo_Historial') IS NULL
 BEGIN RAISERROR('No se encontro la columna RootId / Cod_Grupo_Historial.',16,1); END;

 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Revision') IS NOT NULL AND COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Num_Version') IS NOT NULL
 BEGIN RAISERROR('Existen ambas columnas: Revision y Num_Version. No se modifico la tabla.',16,1); END;
 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Revision') IS NOT NULL
  EXEC sys.sp_rename N'dbo.LG_ANALISIS_RB_HISTORIAL.Revision',N'Num_Version',N'COLUMN';
 ELSE IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Num_Version') IS NULL
 BEGIN RAISERROR('No se encontro la columna Revision / Num_Version.',16,1); END;

 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','PreviousId') IS NOT NULL AND COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Num_Registro_Anterior') IS NOT NULL
 BEGIN RAISERROR('Existen ambas columnas: PreviousId y Num_Registro_Anterior. No se modifico la tabla.',16,1); END;
 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','PreviousId') IS NOT NULL
  EXEC sys.sp_rename N'dbo.LG_ANALISIS_RB_HISTORIAL.PreviousId',N'Num_Registro_Anterior',N'COLUMN';
 ELSE IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Num_Registro_Anterior') IS NULL
 BEGIN RAISERROR('No se encontro la columna PreviousId / Num_Registro_Anterior.',16,1); END;

 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Action') IS NOT NULL AND COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Tip_Accion') IS NOT NULL
 BEGIN RAISERROR('Existen ambas columnas: Action y Tip_Accion. No se modifico la tabla.',16,1); END;
 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Action') IS NOT NULL
  EXEC sys.sp_rename N'dbo.LG_ANALISIS_RB_HISTORIAL.Action',N'Tip_Accion',N'COLUMN';
 ELSE IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Tip_Accion') IS NULL
 BEGIN RAISERROR('No se encontro la columna Action / Tip_Accion.',16,1); END;

 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','RequestId') IS NOT NULL AND COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Cod_Solicitud') IS NOT NULL
 BEGIN RAISERROR('Existen ambas columnas: RequestId y Cod_Solicitud. No se modifico la tabla.',16,1); END;
 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','RequestId') IS NOT NULL
  EXEC sys.sp_rename N'dbo.LG_ANALISIS_RB_HISTORIAL.RequestId',N'Cod_Solicitud',N'COLUMN';
 ELSE IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Cod_Solicitud') IS NULL
 BEGIN RAISERROR('No se encontro la columna RequestId / Cod_Solicitud.',16,1); END;

 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Factory') IS NOT NULL AND COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Cod_Fabrica') IS NOT NULL
 BEGIN RAISERROR('Existen ambas columnas: Factory y Cod_Fabrica. No se modifico la tabla.',16,1); END;
 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Factory') IS NOT NULL
  EXEC sys.sp_rename N'dbo.LG_ANALISIS_RB_HISTORIAL.Factory',N'Cod_Fabrica',N'COLUMN';
 ELSE IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Cod_Fabrica') IS NULL
 BEGIN RAISERROR('No se encontro la columna Factory / Cod_Fabrica.',16,1); END;

 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Np') IS NOT NULL AND COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Cod_OrdPro') IS NOT NULL
 BEGIN RAISERROR('Existen ambas columnas: Np y Cod_OrdPro. No se modifico la tabla.',16,1); END;
 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Np') IS NOT NULL
  EXEC sys.sp_rename N'dbo.LG_ANALISIS_RB_HISTORIAL.Np',N'Cod_OrdPro',N'COLUMN';
 ELSE IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Cod_OrdPro') IS NULL
 BEGIN RAISERROR('No se encontro la columna Np / Cod_OrdPro.',16,1); END;

 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','ProgrammedQuantity') IS NOT NULL AND COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Num_Prendas_Programadas') IS NOT NULL
 BEGIN RAISERROR('Existen ambas columnas: ProgrammedQuantity y Num_Prendas_Programadas. No se modifico la tabla.',16,1); END;
 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','ProgrammedQuantity') IS NOT NULL
  EXEC sys.sp_rename N'dbo.LG_ANALISIS_RB_HISTORIAL.ProgrammedQuantity',N'Num_Prendas_Programadas',N'COLUMN';
 ELSE IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Num_Prendas_Programadas') IS NULL
 BEGIN RAISERROR('No se encontro la columna ProgrammedQuantity / Num_Prendas_Programadas.',16,1); END;

 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','CommercialPrice') IS NOT NULL AND COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Imp_Precio_Comercial') IS NOT NULL
 BEGIN RAISERROR('Existen ambas columnas: CommercialPrice y Imp_Precio_Comercial. No se modifico la tabla.',16,1); END;
 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','CommercialPrice') IS NOT NULL
  EXEC sys.sp_rename N'dbo.LG_ANALISIS_RB_HISTORIAL.CommercialPrice',N'Imp_Precio_Comercial',N'COLUMN';
 ELSE IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Imp_Precio_Comercial') IS NULL
 BEGIN RAISERROR('No se encontro la columna CommercialPrice / Imp_Precio_Comercial.',16,1); END;

 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','SewingTime') IS NOT NULL AND COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Min_Costura') IS NOT NULL
 BEGIN RAISERROR('Existen ambas columnas: SewingTime y Min_Costura. No se modifico la tabla.',16,1); END;
 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','SewingTime') IS NOT NULL
  EXEC sys.sp_rename N'dbo.LG_ANALISIS_RB_HISTORIAL.SewingTime',N'Min_Costura',N'COLUMN';
 ELSE IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Min_Costura') IS NULL
 BEGIN RAISERROR('No se encontro la columna SewingTime / Min_Costura.',16,1); END;

 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','UsedTime') IS NOT NULL AND COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Min_Considerados') IS NOT NULL
 BEGIN RAISERROR('Existen ambas columnas: UsedTime y Min_Considerados. No se modifico la tabla.',16,1); END;
 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','UsedTime') IS NOT NULL
  EXEC sys.sp_rename N'dbo.LG_ANALISIS_RB_HISTORIAL.UsedTime',N'Min_Considerados',N'COLUMN';
 ELSE IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Min_Considerados') IS NULL
 BEGIN RAISERROR('No se encontro la columna UsedTime / Min_Considerados.',16,1); END;

 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','ServicePrice') IS NOT NULL AND COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Imp_Precio_Servicio') IS NOT NULL
 BEGIN RAISERROR('Existen ambas columnas: ServicePrice y Imp_Precio_Servicio. No se modifico la tabla.',16,1); END;
 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','ServicePrice') IS NOT NULL
  EXEC sys.sp_rename N'dbo.LG_ANALISIS_RB_HISTORIAL.ServicePrice',N'Imp_Precio_Servicio',N'COLUMN';
 ELSE IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Imp_Precio_Servicio') IS NULL
 BEGIN RAISERROR('No se encontro la columna ServicePrice / Imp_Precio_Servicio.',16,1); END;

 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','OtherExpenses') IS NOT NULL AND COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Imp_Otros_Gastos') IS NOT NULL
 BEGIN RAISERROR('Existen ambas columnas: OtherExpenses y Imp_Otros_Gastos. No se modifico la tabla.',16,1); END;
 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','OtherExpenses') IS NOT NULL
  EXEC sys.sp_rename N'dbo.LG_ANALISIS_RB_HISTORIAL.OtherExpenses',N'Imp_Otros_Gastos',N'COLUMN';
 ELSE IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Imp_Otros_Gastos') IS NULL
 BEGIN RAISERROR('No se encontro la columna OtherExpenses / Imp_Otros_Gastos.',16,1); END;

 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','ImagePath') IS NOT NULL AND COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Des_Ruta_Imagen') IS NOT NULL
 BEGIN RAISERROR('Existen ambas columnas: ImagePath y Des_Ruta_Imagen. No se modifico la tabla.',16,1); END;
 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','ImagePath') IS NOT NULL
  EXEC sys.sp_rename N'dbo.LG_ANALISIS_RB_HISTORIAL.ImagePath',N'Des_Ruta_Imagen',N'COLUMN';
 ELSE IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Des_Ruta_Imagen') IS NULL
  EXEC(N'ALTER TABLE dbo.LG_ANALISIS_RB_HISTORIAL ADD Des_Ruta_Imagen nvarchar(1024) NULL');

 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','SnapshotJson') IS NOT NULL AND COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Txt_Datos_Analisis') IS NOT NULL
 BEGIN RAISERROR('Existen ambas columnas: SnapshotJson y Txt_Datos_Analisis. No se modifico la tabla.',16,1); END;
 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','SnapshotJson') IS NOT NULL
  EXEC sys.sp_rename N'dbo.LG_ANALISIS_RB_HISTORIAL.SnapshotJson',N'Txt_Datos_Analisis',N'COLUMN';
 ELSE IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Txt_Datos_Analisis') IS NULL
 BEGIN RAISERROR('No se encontro la columna SnapshotJson / Txt_Datos_Analisis.',16,1); END;

 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','RuleVersion') IS NOT NULL AND COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Cod_Version_Reglas') IS NOT NULL
 BEGIN RAISERROR('Existen ambas columnas: RuleVersion y Cod_Version_Reglas. No se modifico la tabla.',16,1); END;
 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','RuleVersion') IS NOT NULL
  EXEC sys.sp_rename N'dbo.LG_ANALISIS_RB_HISTORIAL.RuleVersion',N'Cod_Version_Reglas',N'COLUMN';
 ELSE IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Cod_Version_Reglas') IS NULL
 BEGIN RAISERROR('No se encontro la columna RuleVersion / Cod_Version_Reglas.',16,1); END;

 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','GestionUser') IS NOT NULL AND COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Cod_Usuario') IS NOT NULL
 BEGIN RAISERROR('Existen ambas columnas: GestionUser y Cod_Usuario. No se modifico la tabla.',16,1); END;
 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','GestionUser') IS NOT NULL
  EXEC sys.sp_rename N'dbo.LG_ANALISIS_RB_HISTORIAL.GestionUser',N'Cod_Usuario',N'COLUMN';
 ELSE IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Cod_Usuario') IS NULL
 BEGIN RAISERROR('No se encontro la columna GestionUser / Cod_Usuario.',16,1); END;

 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','SavedAtUtc') IS NOT NULL AND COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Fec_Registro_UTC') IS NOT NULL
 BEGIN RAISERROR('Existen ambas columnas: SavedAtUtc y Fec_Registro_UTC. No se modifico la tabla.',16,1); END;
 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','SavedAtUtc') IS NOT NULL
  EXEC sys.sp_rename N'dbo.LG_ANALISIS_RB_HISTORIAL.SavedAtUtc',N'Fec_Registro_UTC',N'COLUMN';
 ELSE IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Fec_Registro_UTC') IS NULL
 BEGIN RAISERROR('No se encontro la columna SavedAtUtc / Fec_Registro_UTC.',16,1); END;

 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','TraceId') IS NOT NULL AND COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Cod_Traza') IS NOT NULL
 BEGIN RAISERROR('Existen ambas columnas: TraceId y Cod_Traza. No se modifico la tabla.',16,1); END;
 IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','TraceId') IS NOT NULL
  EXEC sys.sp_rename N'dbo.LG_ANALISIS_RB_HISTORIAL.TraceId',N'Cod_Traza',N'COLUMN';
 ELSE IF COL_LENGTH('dbo.LG_ANALISIS_RB_HISTORIAL','Cod_Traza') IS NULL
 BEGIN RAISERROR('No se encontro la columna TraceId / Cod_Traza.',16,1); END;

 IF OBJECT_ID('dbo.FK_LG_ANALISIS_RB_PREVIOUS') IS NOT NULL AND OBJECT_ID('dbo.FK_LG_ANALISIS_RB_ANTERIOR') IS NULL
  EXEC sys.sp_rename N'dbo.FK_LG_ANALISIS_RB_PREVIOUS',N'FK_LG_ANALISIS_RB_ANTERIOR',N'OBJECT';

 IF OBJECT_ID('dbo.CK_LG_ANALISIS_RB_ACTION') IS NOT NULL AND OBJECT_ID('dbo.CK_LG_ANALISIS_RB_ACCION') IS NULL
  EXEC sys.sp_rename N'dbo.CK_LG_ANALISIS_RB_ACTION',N'CK_LG_ANALISIS_RB_ACCION',N'OBJECT';

 IF OBJECT_ID('dbo.CK_LG_ANALISIS_RB_REVISION') IS NOT NULL AND OBJECT_ID('dbo.CK_LG_ANALISIS_RB_VERSION') IS NULL
  EXEC sys.sp_rename N'dbo.CK_LG_ANALISIS_RB_REVISION',N'CK_LG_ANALISIS_RB_VERSION',N'OBJECT';

 IF OBJECT_ID('dbo.CK_LG_ANALISIS_RB_SNAPSHOT') IS NOT NULL AND OBJECT_ID('dbo.CK_LG_ANALISIS_RB_DATOS') IS NULL
  EXEC sys.sp_rename N'dbo.CK_LG_ANALISIS_RB_SNAPSHOT',N'CK_LG_ANALISIS_RB_DATOS',N'OBJECT';

 IF OBJECT_ID('dbo.CK_LG_ANALISIS_RB_TIME') IS NOT NULL AND OBJECT_ID('dbo.CK_LG_ANALISIS_RB_TIEMPOS') IS NULL
  EXEC sys.sp_rename N'dbo.CK_LG_ANALISIS_RB_TIME',N'CK_LG_ANALISIS_RB_TIEMPOS',N'OBJECT';

 IF OBJECT_ID('dbo.CK_LG_ANALISIS_RB_POSITIVE') IS NOT NULL AND OBJECT_ID('dbo.CK_LG_ANALISIS_RB_IMPORTES') IS NULL
  EXEC sys.sp_rename N'dbo.CK_LG_ANALISIS_RB_POSITIVE',N'CK_LG_ANALISIS_RB_IMPORTES',N'OBJECT';

 IF OBJECT_ID('dbo.UQ_LG_ANALISIS_RB_REQUEST') IS NOT NULL AND OBJECT_ID('dbo.UQ_LG_ANALISIS_RB_SOLICITUD') IS NULL
  EXEC sys.sp_rename N'dbo.UQ_LG_ANALISIS_RB_REQUEST',N'UQ_LG_ANALISIS_RB_SOLICITUD',N'OBJECT';

 IF OBJECT_ID('dbo.UQ_LG_ANALISIS_RB_REVISION') IS NOT NULL AND OBJECT_ID('dbo.UQ_LG_ANALISIS_RB_VERSION') IS NULL
  EXEC sys.sp_rename N'dbo.UQ_LG_ANALISIS_RB_REVISION',N'UQ_LG_ANALISIS_RB_VERSION',N'OBJECT';

 IF EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.LG_ANALISIS_RB_HISTORIAL') AND name='IX_LG_ANALISIS_RB_NP') AND NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.LG_ANALISIS_RB_HISTORIAL') AND name='IX_LG_ANALISIS_RB_ORDPRO')
  EXEC sys.sp_rename N'dbo.LG_ANALISIS_RB_HISTORIAL.IX_LG_ANALISIS_RB_NP',N'IX_LG_ANALISIS_RB_ORDPRO',N'INDEX';

 IF EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.LG_ANALISIS_RB_HISTORIAL') AND name='IX_LG_ANALISIS_RB_PREVIOUS') AND NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.LG_ANALISIS_RB_HISTORIAL') AND name='IX_LG_ANALISIS_RB_ANTERIOR')
  EXEC sys.sp_rename N'dbo.LG_ANALISIS_RB_HISTORIAL.IX_LG_ANALISIS_RB_PREVIOUS',N'IX_LG_ANALISIS_RB_ANTERIOR',N'INDEX';

 -- Revalidar las expresiones CHECK con los nombres nuevos.
 EXEC(N'IF OBJECT_ID(''dbo.CK_LG_ANALISIS_RB_ACCION'',''C'') IS NOT NULL ALTER TABLE dbo.LG_ANALISIS_RB_HISTORIAL DROP CONSTRAINT CK_LG_ANALISIS_RB_ACCION; ALTER TABLE dbo.LG_ANALISIS_RB_HISTORIAL WITH CHECK ADD CONSTRAINT CK_LG_ANALISIS_RB_ACCION CHECK(Tip_Accion IN (''I'',''U'',''D''));');
 EXEC(N'IF OBJECT_ID(''dbo.CK_LG_ANALISIS_RB_VERSION'',''C'') IS NOT NULL ALTER TABLE dbo.LG_ANALISIS_RB_HISTORIAL DROP CONSTRAINT CK_LG_ANALISIS_RB_VERSION; ALTER TABLE dbo.LG_ANALISIS_RB_HISTORIAL WITH CHECK ADD CONSTRAINT CK_LG_ANALISIS_RB_VERSION CHECK(Num_Version>=1);');
 EXEC(N'IF OBJECT_ID(''dbo.CK_LG_ANALISIS_RB_DATOS'',''C'') IS NOT NULL ALTER TABLE dbo.LG_ANALISIS_RB_HISTORIAL DROP CONSTRAINT CK_LG_ANALISIS_RB_DATOS; ALTER TABLE dbo.LG_ANALISIS_RB_HISTORIAL WITH CHECK ADD CONSTRAINT CK_LG_ANALISIS_RB_DATOS CHECK(DATALENGTH(Txt_Datos_Analisis)>0);');
 EXEC(N'IF OBJECT_ID(''dbo.CK_LG_ANALISIS_RB_TIEMPOS'',''C'') IS NOT NULL ALTER TABLE dbo.LG_ANALISIS_RB_HISTORIAL DROP CONSTRAINT CK_LG_ANALISIS_RB_TIEMPOS; ALTER TABLE dbo.LG_ANALISIS_RB_HISTORIAL WITH CHECK ADD CONSTRAINT CK_LG_ANALISIS_RB_TIEMPOS CHECK(Min_Costura>0 AND Min_Considerados>0);');
 EXEC(N'IF OBJECT_ID(''dbo.CK_LG_ANALISIS_RB_IMPORTES'',''C'') IS NOT NULL ALTER TABLE dbo.LG_ANALISIS_RB_HISTORIAL DROP CONSTRAINT CK_LG_ANALISIS_RB_IMPORTES; ALTER TABLE dbo.LG_ANALISIS_RB_HISTORIAL WITH CHECK ADD CONSTRAINT CK_LG_ANALISIS_RB_IMPORTES CHECK(Num_Prendas_Programadas>=0 AND Imp_Precio_Comercial>=0 AND Imp_Precio_Servicio>=0 AND Imp_Otros_Gastos>=0);');

 EXEC(N'/* CRUD versionado. Ejecutar DESPUÉS de 01_LG_ANALISIS_RB_HISTORIAL_Tabla.sql.
 I0001: nueva foto. U0001: nueva revisión, conserva la anterior.
 D0001: nueva revisión marcada D; NO borra el historial.
 S0001: por NP/fábrica; @Num_Registro permite consultar cualquier revisión.
 La API obtiene Cod_Usuario de la sesion autenticada; en uso administrativo indicar el usuario responsable.
 Cod_Solicitud se reutiliza SOLO al reintentar la misma operación; impide duplicados.
 Txt_Datos_Analisis lo genera la API: SQL Server 2008 no interpreta JSON.
 No ejecutar guardados de ejemplo en producción.
*/
IF OBJECT_ID(''dbo.PA_LG_ANALISIS_RB_HISTORIAL_I0001'',''P'') IS NULL EXEC(''CREATE PROCEDURE dbo.PA_LG_ANALISIS_RB_HISTORIAL_I0001 AS RETURN 0'');');

 EXEC(N'ALTER PROCEDURE dbo.PA_LG_ANALISIS_RB_HISTORIAL_I0001
 @Cod_Solicitud uniqueidentifier,
 @Cod_Fabrica char(3), @Cod_OrdPro char(5), @Num_Prendas_Programadas int,
 @Imp_Precio_Comercial decimal(18,4), @Min_Costura decimal(18,6), @Min_Considerados decimal(18,6),
 @Imp_Precio_Servicio decimal(18,6), @Imp_Otros_Gastos decimal(18,6),
 @Txt_Datos_Analisis nvarchar(max), @Cod_Version_Reglas varchar(40), @Cod_Usuario varchar(30), @Cod_Traza varchar(100),
 @Num_Registro_Anterior bigint = NULL, @Tip_Accion char(1) = ''I'', @Des_Ruta_Imagen nvarchar(1024) = NULL
AS
BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF @Tip_Accion NOT IN (''I'',''U'',''D'') BEGIN RAISERROR(''Accion de historial invalida.'',16,1); RETURN; END;
 IF @Cod_Solicitud IS NULL OR LEN(LTRIM(RTRIM(@Txt_Datos_Analisis)))=0 OR LEN(LTRIM(RTRIM(@Cod_Usuario)))=0
 BEGIN RAISERROR(''Completa la foto del analisis y el usuario.'',16,1); RETURN; END;

 DECLARE @Cod_Grupo_Historial uniqueidentifier, @Num_Version int, @Num_Registro bigint, @Cod_OrdPro_Anterior char(5), @Cod_Fabrica_Anterior char(3), @Tip_Accion_Anterior char(1);
 BEGIN TRY
  BEGIN TRANSACTION;
  SELECT @Num_Registro=Num_Registro FROM dbo.LG_ANALISIS_RB_HISTORIAL WITH(UPDLOCK,HOLDLOCK) WHERE Cod_Solicitud=@Cod_Solicitud;
  IF @Num_Registro IS NOT NULL
  BEGIN
   IF NOT EXISTS(SELECT 1 FROM dbo.LG_ANALISIS_RB_HISTORIAL WHERE Num_Registro=@Num_Registro AND Cod_Fabrica=@Cod_Fabrica AND Cod_OrdPro=@Cod_OrdPro AND Tip_Accion=@Tip_Accion AND ISNULL(Num_Registro_Anterior,0)=ISNULL(@Num_Registro_Anterior,0) AND Cod_Usuario=@Cod_Usuario AND ISNULL(Des_Ruta_Imagen,N'''')=ISNULL(@Des_Ruta_Imagen,N'''') AND Txt_Datos_Analisis COLLATE Latin1_General_BIN2=@Txt_Datos_Analisis COLLATE Latin1_General_BIN2)
   BEGIN RAISERROR(''La clave de reintento pertenece a otra operacion.'',16,1); END;
  END
  ELSE
  BEGIN
   IF @Tip_Accion=''I''
   BEGIN
    IF @Num_Registro_Anterior IS NOT NULL BEGIN RAISERROR(''Un guardado nuevo no puede reemplazar una revision.'',16,1); END;
    SET @Cod_Grupo_Historial=NEWID(); SET @Num_Version=1;
   END
   ELSE
   BEGIN
    SELECT @Cod_Grupo_Historial=Cod_Grupo_Historial,@Num_Version=Num_Version+1,@Cod_OrdPro_Anterior=Cod_OrdPro,@Cod_Fabrica_Anterior=Cod_Fabrica,@Tip_Accion_Anterior=Tip_Accion
    FROM dbo.LG_ANALISIS_RB_HISTORIAL WITH(UPDLOCK,HOLDLOCK) WHERE Num_Registro=@Num_Registro_Anterior;
    IF @Cod_Grupo_Historial IS NULL OR @Cod_OrdPro_Anterior<>@Cod_OrdPro OR @Cod_Fabrica_Anterior<>@Cod_Fabrica OR @Tip_Accion_Anterior=''D''
       OR EXISTS(SELECT 1 FROM dbo.LG_ANALISIS_RB_HISTORIAL WITH(UPDLOCK,HOLDLOCK) WHERE Num_Registro_Anterior=@Num_Registro_Anterior)
    BEGIN RAISERROR(''La revision ya cambio o fue eliminada. Consulta el historial.'',16,1); END;
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
END;');

 EXEC(N'IF OBJECT_ID(''dbo.PA_LG_ANALISIS_RB_HISTORIAL_U0001'',''P'') IS NULL EXEC(''CREATE PROCEDURE dbo.PA_LG_ANALISIS_RB_HISTORIAL_U0001 AS RETURN 0'');');

 EXEC(N'ALTER PROCEDURE dbo.PA_LG_ANALISIS_RB_HISTORIAL_U0001
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
 @Imp_Otros_Gastos=@Imp_Otros_Gastos,@Txt_Datos_Analisis=@Txt_Datos_Analisis,@Cod_Version_Reglas=@Cod_Version_Reglas,@Cod_Usuario=@Cod_Usuario,@Cod_Traza=@Cod_Traza,@Num_Registro_Anterior=@Num_Registro,@Tip_Accion=''U'',@Des_Ruta_Imagen=@Des_Ruta_Imagen;
END;');

 EXEC(N'IF OBJECT_ID(''dbo.PA_LG_ANALISIS_RB_HISTORIAL_D0001'',''P'') IS NULL EXEC(''CREATE PROCEDURE dbo.PA_LG_ANALISIS_RB_HISTORIAL_D0001 AS RETURN 0'');');

 EXEC(N'ALTER PROCEDURE dbo.PA_LG_ANALISIS_RB_HISTORIAL_D0001
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
 IF @Cod_OrdPro IS NULL BEGIN RAISERROR(''El analisis no existe.'',16,1); RETURN; END;
 EXEC dbo.PA_LG_ANALISIS_RB_HISTORIAL_I0001 @Cod_Solicitud=@Cod_Solicitud,@Cod_Fabrica=@Cod_Fabrica,@Cod_OrdPro=@Cod_OrdPro,@Num_Prendas_Programadas=@Num_Prendas_Programadas,
 @Imp_Precio_Comercial=@Imp_Precio_Comercial,@Min_Costura=@Min_Costura,@Min_Considerados=@Min_Considerados,@Imp_Precio_Servicio=@Imp_Precio_Servicio,
 @Imp_Otros_Gastos=@Imp_Otros_Gastos,@Txt_Datos_Analisis=@Txt_Datos_Analisis,@Cod_Version_Reglas=@Cod_Version_Reglas,@Cod_Usuario=@Cod_Usuario,@Cod_Traza=@Cod_Traza,@Num_Registro_Anterior=@Num_Registro,@Tip_Accion=''D'',@Des_Ruta_Imagen=@Des_Ruta_Imagen;
END;');

 EXEC(N'IF OBJECT_ID(''dbo.PA_LG_ANALISIS_RB_HISTORIAL_S0001'',''P'') IS NULL EXEC(''CREATE PROCEDURE dbo.PA_LG_ANALISIS_RB_HISTORIAL_S0001 AS RETURN 0'');');

 EXEC(N'ALTER PROCEDURE dbo.PA_LG_ANALISIS_RB_HISTORIAL_S0001
 @Cod_Fabrica char(3),@Cod_OrdPro char(5),@Num_Registro bigint=NULL,@Num_Desplazamiento int=0,@Num_Tamano_Pagina int=50
AS
BEGIN
 SET NOCOUNT ON;
 IF @Num_Desplazamiento<0 OR @Num_Tamano_Pagina<1 OR @Num_Tamano_Pagina>100 BEGIN RAISERROR(''Paginacion invalida.'',16,1); RETURN; END;
 SELECT COUNT(*) AS Num_Total_Registros FROM dbo.LG_ANALISIS_RB_HISTORIAL WHERE Cod_Fabrica=@Cod_Fabrica AND Cod_OrdPro=@Cod_OrdPro AND (@Num_Registro IS NULL OR Num_Registro=@Num_Registro);
 ;WITH Historial AS (
  SELECT *,ROW_NUMBER() OVER(ORDER BY Fec_Registro_UTC DESC,Num_Registro DESC) AS Num_Fila
  FROM dbo.LG_ANALISIS_RB_HISTORIAL WHERE Cod_Fabrica=@Cod_Fabrica AND Cod_OrdPro=@Cod_OrdPro AND (@Num_Registro IS NULL OR Num_Registro=@Num_Registro)
 )
 SELECT Num_Registro,Cod_Grupo_Historial,Num_Version,Num_Registro_Anterior,Tip_Accion,Cod_Fabrica,Cod_OrdPro,Num_Prendas_Programadas,Imp_Precio_Comercial,Min_Costura,
 Min_Considerados,Imp_Precio_Servicio,Imp_Otros_Gastos,Cod_Version_Reglas,Cod_Usuario,Fec_Registro_UTC,Cod_Traza,Des_Ruta_Imagen,
 CASE WHEN @Num_Registro IS NOT NULL THEN Txt_Datos_Analisis ELSE NULL END AS Txt_Datos_Analisis
 FROM Historial WHERE Num_Fila>@Num_Desplazamiento AND Num_Fila<=@Num_Desplazamiento+@Num_Tamano_Pagina ORDER BY Num_Fila;
END;');

 EXEC(N'/* EJEMPLOS DE CONSULTA (descomentar y ajustar NP/Num_Registro en la base correcta):
EXEC dbo.PA_LG_ANALISIS_RB_HISTORIAL_S0001 @Cod_Fabrica=''001'', @Cod_OrdPro=''J1825'';
EXEC dbo.PA_LG_ANALISIS_RB_HISTORIAL_S0001 @Cod_Fabrica=''001'', @Cod_OrdPro=''J1825'', @Num_Registro=42;
SELECT Num_Registro,Cod_OrdPro,Num_Version,Num_Registro_Anterior,Tip_Accion,Num_Prendas_Programadas,Imp_Precio_Comercial,Min_Costura,
       Min_Considerados,Imp_Precio_Servicio,Imp_Otros_Gastos,Cod_Usuario,Fec_Registro_UTC,Txt_Datos_Analisis
FROM dbo.LG_ANALISIS_RB_HISTORIAL
WHERE Cod_Fabrica=''001'' AND Cod_OrdPro=''J1825''
ORDER BY Fec_Registro_UTC DESC,Num_Registro DESC;

I0001 recibe la copia completa generada por la API y devuelve el registro insertado.
U0001 recibe los mismos parametros mas @Num_Registro (revision anterior).
D0001 recibe @Num_Registro, @Cod_Solicitud, @Cod_Usuario, @Cod_Traza.
Para cada operacion nueva generar un Cod_Solicitud con NEWID(); reutilizarlo solo en el mismo reintento.
*/');

 SELECT @Num_Registros_Despues=COUNT_BIG(*) FROM dbo.LG_ANALISIS_RB_HISTORIAL;
 SET @Txt_Sql=N'SELECT @Num_Bytes=ISNULL(SUM(CONVERT(bigint,DATALENGTH(Txt_Datos_Analisis))),0) FROM dbo.LG_ANALISIS_RB_HISTORIAL';
 EXEC sys.sp_executesql @Txt_Sql,N'@Num_Bytes bigint OUTPUT',@Num_Bytes=@Num_Bytes_Despues OUTPUT;
 IF @Num_Registros_Antes<>@Num_Registros_Despues OR @Num_Bytes_Antes<>@Num_Bytes_Despues
 BEGIN RAISERROR('La comprobacion del historial no coincide. Se revierte la migracion.',16,1); END;
 COMMIT TRANSACTION;
 SELECT @Num_Registros_Despues AS Num_Registros_Conservados,@Num_Bytes_Despues AS Num_Bytes_Datos_Conservados;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
 DECLARE @Des_Error nvarchar(4000);SET @Des_Error=ERROR_MESSAGE();RAISERROR(@Des_Error,16,1);
END CATCH;
GO
