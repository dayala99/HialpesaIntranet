-- Actualizar manualmente en HIALPESA. No modifica los procedimientos originales.
-- Compatible con SQL Server 2008 R2. Modos C: catálogos; L: bloquear y consultar eventos de un ítem.
ALTER PROCEDURE dbo.PA_TI_CC_EVENTOS_S0001
 @MODO CHAR(1)='C', @COD_ORDTRA CHAR(5)='', @NUM_SECUENCIA INT=0, @COD_EVENTO_CC CHAR(3)=''
AS
BEGIN
 SET NOCOUNT ON;
 IF @MODO='C'
 BEGIN
 SELECT 'events' kind,RTRIM(Cod_Evento_CC) code,RTRIM(Descripcion) description FROM TI_CC_EVENTOS ORDER BY Orden,Descripcion;
 SELECT 'validEvents' kind,RTRIM(Cod_Evento_CC) code,RTRIM(Descripcion) description FROM TI_CC_EVENTOS WHERE Orden>0 AND Flg_Valido='S' ORDER BY Orden,Descripcion;
 SELECT 'statuses' kind,RTRIM(Flg_Status_CC) code,RTRIM(Descripcion) description FROM TI_CC_STATUS ORDER BY Orden;
 SELECT 'auditors' kind,RTRIM(a.Tip_Auditor)+RTRIM(a.Cod_Auditor) code,RTRIM(a.Nom_Auditor) description FROM TI_CC_AUDITOR a INNER JOIN CC_AREAS b ON a.Area_Auditor=b.Cod_Area_CC WHERE b.Flg_Tintoreria='*' ORDER BY a.Tip_Auditor,a.Cod_Auditor;
 SELECT 'locations' kind,RTRIM(Cod_Ubicacion_Reproceso) code,RTRIM(Nom_Ubicacion_Reproceso) description FROM TI_UBICACION_REPROCESO WHERE FLG_ACTIVO='S' ORDER BY Cod_Ubicacion_Reproceso;
 SELECT 'reasons' kind,RTRIM(a.Cod_Motivo) code,RTRIM(a.Descripcion) description FROM CC_CONFEC_MOTIVOS a INNER JOIN CC_AREAS b ON a.Cod_Area_CC=b.Cod_Area_CC WHERE b.Flg_Tintoreria='*' ORDER BY a.Descripcion;
 SELECT 'deviations' kind,RTRIM(Nro_Desviacion) code,RTRIM(Des_Desviacion) description FROM TI_DESVIACION ORDER BY Nro_Desviacion;
 SELECT 'causes' kind,RTRIM(Nro_Causa) code,RTRIM(Des_Causa) description FROM TI_CAUSAS ORDER BY Nro_Causa;
 RETURN;
 END;
 IF @MODO='P'
 BEGIN
 SELECT RTRIM(c.Cod_Cliente_Tex) code,RTRIM(c.Abr_Cliente) abbreviation,RTRIM(c.Nom_Cliente) name
 FROM TI_ORDTRA_TINTORERIA p INNER JOIN TX_CLIENTE c ON c.Cod_Cliente_Tex=p.Cod_Cliente_Tex
 WHERE p.Cod_OrdTra=@COD_ORDTRA;
 RETURN;
 END;
 IF @MODO='L'
 BEGIN
 SELECT * FROM TI_ORDTRA_TINTORERIA_ITEMS_CC WITH (UPDLOCK,HOLDLOCK)
 WHERE Cod_OrdTra=@COD_ORDTRA AND Num_Secuencia=@NUM_SECUENCIA AND Cod_Evento_CC=@COD_EVENTO_CC;
 RETURN;
 END;
 RAISERROR('Modo no válido.',16,1);
END;
GO
