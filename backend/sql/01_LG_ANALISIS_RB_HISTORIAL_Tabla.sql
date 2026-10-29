/* Propuesta: historial versionado de Análisis-Rb.
   Ejecutar primero en la base utilizada por Gestión/API. SQL Server 2008 R2.
   NO modifica tablas originales de Gestión. Fechas de auditoría en UTC.
   Txt_Datos_Analisis conserva cabecera, entradas y TODOS los resultados mostrados.
*/
IF OBJECT_ID('dbo.LG_ANALISIS_RB_HISTORIAL','U') IS NULL
BEGIN
 CREATE TABLE dbo.LG_ANALISIS_RB_HISTORIAL (
  Num_Registro bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_LG_ANALISIS_RB_HISTORIAL PRIMARY KEY,
  Cod_Grupo_Historial uniqueidentifier NOT NULL,
  Num_Version int NOT NULL,
  Num_Registro_Anterior bigint NULL,
  Tip_Accion char(1) NOT NULL,
  Cod_Solicitud uniqueidentifier NOT NULL,
  Cod_Fabrica char(3) NOT NULL,
  Cod_OrdPro char(5) NOT NULL,
  Num_Prendas_Programadas int NOT NULL,
  Imp_Precio_Comercial decimal(18,4) NOT NULL,
  Min_Costura decimal(18,6) NOT NULL,
  Min_Considerados decimal(18,6) NOT NULL,
  Imp_Precio_Servicio decimal(18,6) NOT NULL,
  Imp_Otros_Gastos decimal(18,6) NOT NULL,
  Des_Ruta_Imagen nvarchar(1024) NULL,
  Txt_Datos_Analisis nvarchar(max) NOT NULL,
  Cod_Version_Reglas varchar(40) NOT NULL,
  Cod_Usuario varchar(30) NOT NULL,
  Fec_Registro_UTC datetime NOT NULL CONSTRAINT DF_LG_ANALISIS_RB_FECHA DEFAULT GETUTCDATE(),
  Cod_Traza varchar(100) NOT NULL,
  CONSTRAINT FK_LG_ANALISIS_RB_ANTERIOR FOREIGN KEY(Num_Registro_Anterior) REFERENCES dbo.LG_ANALISIS_RB_HISTORIAL(Num_Registro),
  CONSTRAINT CK_LG_ANALISIS_RB_ACCION CHECK(Tip_Accion IN ('I','U','D')),
  CONSTRAINT CK_LG_ANALISIS_RB_VERSION CHECK(Num_Version>=1),
  CONSTRAINT CK_LG_ANALISIS_RB_DATOS CHECK(DATALENGTH(Txt_Datos_Analisis)>0),
  CONSTRAINT CK_LG_ANALISIS_RB_TIEMPOS CHECK(Min_Costura>0 AND Min_Considerados>0),
  CONSTRAINT CK_LG_ANALISIS_RB_IMPORTES CHECK(Num_Prendas_Programadas>=0 AND Imp_Precio_Comercial>=0 AND Imp_Precio_Servicio>=0 AND Imp_Otros_Gastos>=0),
  CONSTRAINT UQ_LG_ANALISIS_RB_SOLICITUD UNIQUE(Cod_Solicitud),
  CONSTRAINT UQ_LG_ANALISIS_RB_VERSION UNIQUE(Cod_Grupo_Historial,Num_Version)
 );
 CREATE INDEX IX_LG_ANALISIS_RB_ORDPRO ON dbo.LG_ANALISIS_RB_HISTORIAL(Cod_Fabrica,Cod_OrdPro,Fec_Registro_UTC DESC,Num_Registro DESC);
 CREATE INDEX IX_LG_ANALISIS_RB_ANTERIOR ON dbo.LG_ANALISIS_RB_HISTORIAL(Num_Registro_Anterior);
END;
GO
