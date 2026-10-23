/*
  HIALPESA - Procedimientos propuestos para SQL directo del backend
  Convención: PA_<TABLA>_<S|I|U|D><secuencia de 4 dígitos>
  Secuencia independiente por tabla y operación; tabla real en MAYÚSCULAS.
  Inventario: backend/src/Intranet.Data, excluyendo bin/obj, pruebas y herramientas.
  No se incluyen ni modifican los SP reutilizados UP_*, LG_MAN_* o LG_MUESTRA_*.
  Todo el SQL directo inventariado es SELECT; no se crean I/U/D sin operación real.
  Archivo preparado para revisión: NO ejecutado en HIALPESA.
  El backend conserva sus consultas actuales; su sustitución por llamadas a estos
  SP requiere desplegar y autorizar primero este archivo.
  CREATE PROCEDURE falla si ya existe un nombre: no sobrescribe objetos existentes.
  Los parámetros usados en búsquedas mantienen los patrones/escapes del backend.
  Los bloqueos UPDLOCK/HOLDLOCK no abren ni confirman transacciones propias.
  No se añaden GRANT, EXECUTE AS, COMMIT, INSERT, UPDATE ni DELETE.
  Consultas con JOIN/UNION usan la tabla principal; los lotes de paginación e
  impresión conservan sus conjuntos de resultados y orden.

  ÍNDICE (46 procedimientos)
  PA_LG_ORDCOMPITEMREQ_S0001 - Requerimientos vinculados y presentación; lectura o bloqueo de filas
  PA_LG_ORDCOMP_S0001 - Cabecera mínima de registro; lectura o bloqueo de filas
  PA_ES_ORDPRO_S0001 - Validar que la OP pertenezca a un grupo asociado a la OC
  PA_TG_PURORD_S0001 - Listado paginado de PO; total seguido de filas
  PA_TG_PURORD_S0002 - Verificar PO y cliente
  PA_TG_LOTCOLTAL_S0001 - Detalle de PO por lote, estilo, color y talla
  PA_LG_ORDCOMP_S0002 - Lectura de cabecera para identidad con permiso de lectura global
  PA_LG_ORDCOMPITEMREQ_S0002 - NP vinculadas a secuencias de la OC, con respaldo desde el ítem
  PA_SEG_USUARIOS_S0001 - Verificar usuario de Gestión; lectura de SEGURIDAD
  PA_LG_ORDCOMP_S0003 - Proveedor, anexo y datos de origen para importación; lectura o bloqueo
  PA_LG_IMPORTACIONES_S0001 - Listado paginado de importaciones y total; filtros antes construidos en C#
  PA_TG_CONTROL_S0001 - Consultar modo de numeración de importaciones; lectura o bloqueo
  PA_CN_ANEXOSCONTABLES_S0001 - Validar anexo contable del proveedor por RUC
  PA_LG_ORIGEN_IMPORTACION_S0001 - Catálogos de importaciones en un único resultado con Kind
  PA_LG_IMPORTACIONES_S0002 - Validar contexto de conexión para registrar importaciones
  PA_CN_TIPOITEM_S0001 - Validar tipo de ítem habilitado para importaciones
  PA_TG_ANOCONTROL_S0001 - Bloquear correlativo anual de importaciones
  PA_LG_ORDCOMP_S0004 - Verificar número de importación asociado a la OC
  PA_LG_ORDCOMP_S0005 - Listado paginado de OC y total; filtros antes construidos en C#
  PA_LG_CONDVENT_S0001 - Catálogo de registro: LG_CONDVENT
  PA_LG_DSCTOS_S0001 - Catálogo de registro: LG_DSCTOS
  PA_TG_MONEDA_S0001 - Catálogo de registro: TG_MONEDA
  PA_LG_LUGENTR_S0001 - Catálogo de registro: LG_LUGENTR
  PA_TG_CENCOSTO_S0001 - Catálogo de registro: TG_CENCOSTO
  PA_LG_CLAORDCOMP_S0001 - Catálogo de registro: LG_CLAORDCOMP
  PA_ES_GRUPOLOG_S0001 - Grupos de manufactura asociados a la OC
  PA_ES_ORDPRO_S0002 - OP del grupo asignado a la OC
  PA_LG_ORDCOMPITEM_S0001 - Valores editables del detalle de OC
  PA_LG_ORDCOMP_S0006 - Valores editables de cabecera y nombre del proveedor
  PA_TG_FABRICA_S0001 - Catálogo de fábricas
  PA_ES_GRUPOLOG_S0002 - Buscar grupos por fábrica y OP
  PA_LG_PROVEEDOR_S0001 - Valores por defecto del proveedor: condición, descuento e IGV
  PA_LG_PROVEEDOR_S0002 - Buscar proveedores activos
  PA_ES_GRUPOLOG_S0003 - Buscar grupos de manufactura
  PA_LG_ORDCOMP_S0007 - Cabecera y líneas de impresión de OC
  PA_LG_ORDCOMPITEM_S0002 - Desglose de impresión por requerimiento
  PA_LG_SEGORDCOMP_S0001 - Validar permiso de escritura de OC
  PA_LG_NUMORDCOMP_S0001 - Bloquear numeración de OC
  PA_ES_GRUPOLOG_S0004 - Verificar existencia del grupo
  PA_LG_ORDCOMP_GRUPOS_S0001 - Verificar vínculo grupo-OC con bloqueo
  PA_LG_ORDCOMPITEM_S0003 - Consultar cantidad recibida con bloqueo
  PA_LG_ORDCOMPITEM_S0004 - Leer detalle completo con bloqueo
  PA_LG_ORDCOMPITEMREQ_S0003 - Contar requerimientos vinculados con bloqueo
  PA_LG_ORDCOMP_S0008 - Leer cabecera completa para preservar campos
  PA_ES_ORDPROREQ_ITEMS_S0001 - Bloquear requerimientos por OP e ítem
  PA_LG_ORDCOMPITEM_S0005 - Leer secuencias, requeridos y precio con bloqueo
*/
USE [HIALPESA];
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/*
 Requerimientos vinculados y presentación; lectura o bloqueo de filas
 Origen: backend/src/Intranet.Data/Queries/OrderRequirementLinks.cs:15
 Nota: La variante con bloqueo requiere la transacción del llamador para conservar los bloqueos.
*/
CREATE PROCEDURE [dbo].[PA_LG_ORDCOMPITEMREQ_S0001]
    @Series char(3),
    @Number char(6),
    @Sequence char(3),
    @LockRow bit = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF @LockRow=1
    BEGIN
        SELECT r.*,ep.Des_Present Presentation FROM dbo.LG_ORDCOMPITEMREQ r WITH(UPDLOCK,HOLDLOCK) LEFT JOIN dbo.ES_ORDPRO op ON op.Cod_Fabrica=r.Cod_Fabrica AND op.Cod_OrdPro=r.Cod_OrdPro LEFT JOIN dbo.ES_ESTPROPRE ep ON ep.Cod_EstPro=op.Cod_EstPro AND ep.Cod_Present=r.Cod_Present WHERE r.Ser_OrdComp=@Series AND r.Cod_OrdComp=@Number AND r.Sec_OrdComp=@Sequence ORDER BY r.Cod_OrdPro,r.Cod_Present,r.Cod_Talla;
    END
    ELSE
    BEGIN
        SELECT r.*,ep.Des_Present Presentation FROM dbo.LG_ORDCOMPITEMREQ r LEFT JOIN dbo.ES_ORDPRO op ON op.Cod_Fabrica=r.Cod_Fabrica AND op.Cod_OrdPro=r.Cod_OrdPro LEFT JOIN dbo.ES_ESTPROPRE ep ON ep.Cod_EstPro=op.Cod_EstPro AND ep.Cod_Present=r.Cod_Present WHERE r.Ser_OrdComp=@Series AND r.Cod_OrdComp=@Number AND r.Sec_OrdComp=@Sequence ORDER BY r.Cod_OrdPro,r.Cod_Present,r.Cod_Talla;
    END ;
END;
GO

/*
 Cabecera mínima de registro; lectura o bloqueo de filas
 Origen: backend/src/Intranet.Data/Queries/RegistrationQueries.cs:27
*/
CREATE PROCEDURE [dbo].[PA_LG_ORDCOMP_S0001]
    @Series char(3),
    @Number char(6),
    @LockRow bit = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF @LockRow=1
    BEGIN
        SELECT Ser_OrdComp,Cod_OrdComp,Cod_Proveedor,Cod_StaOrdComp,Cod_ClaOrdComp,Cod_Grupo FROM dbo.LG_ORDCOMP WITH (UPDLOCK,HOLDLOCK) WHERE Ser_OrdComp=@Series AND Cod_OrdComp=@Number;
    END
    ELSE
    BEGIN
        SELECT Ser_OrdComp,Cod_OrdComp,Cod_Proveedor,Cod_StaOrdComp,Cod_ClaOrdComp,Cod_Grupo FROM dbo.LG_ORDCOMP WHERE Ser_OrdComp=@Series AND Cod_OrdComp=@Number;
    END ;
END;
GO

/*
 Validar que la OP pertenezca a un grupo asociado a la OC
 Origen: backend/src/Intranet.Data/Queries/RegistrationQueries.cs:36
*/
CREATE PROCEDURE [dbo].[PA_ES_ORDPRO_S0001]
    @Series char(3),
    @Number char(6),
    @Op char(5)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) FROM dbo.ES_ORDPRO p
    WHERE p.Cod_OrdPro=@Op AND EXISTS (
        SELECT 1 FROM dbo.LG_OrdComp_Grupos g WHERE g.Ser_OrdComp=@Series AND g.Cod_OrdComp=@Number AND g.Cod_Grupo=p.Cod_GrupoLog) ;
END;
GO

/*
 Listado paginado de PO; total seguido de filas
 Origen: backend/src/Intranet.Data/Repositories/AviosRepository.cs:14; backend/src/Intranet.Data/Repositories/AviosRepository.cs:21
 Nota: Dos resultados: COUNT y página. PoPattern/Pattern conservan el escape ~ que hoy aplica el backend.
*/
CREATE PROCEDURE [dbo].[PA_TG_PURORD_S0001]
    @Po nvarchar(70) = N'',
    @PoPattern nvarchar(150) = N'%%',
    @Search nvarchar(100) = N'',
    @Pattern nvarchar(220) = N'%%',
    @Offset int = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) FROM dbo.TG_PurOrd p
    LEFT JOIN dbo.TG_Cliente c ON c.Cod_Cliente=p.Cod_Cliente
    WHERE (@Po='' OR p.Cod_PurOrd LIKE @PoPattern ESCAPE '~')
      AND (@Search='' OR p.Cod_PurOrd LIKE @Pattern ESCAPE '~' OR c.Nom_Cliente LIKE @Pattern ESCAPE '~' OR p.Des_General LIKE @Pattern ESCAPE '~');
    ;WITH Paged AS (
     SELECT ROW_NUMBER() OVER(ORDER BY p.Fec_Creacion DESC,p.Cod_PurOrd,p.Cod_Cliente) AS RowNumber,RTRIM(p.Cod_PurOrd) AS Po,RTRIM(p.Cod_Cliente) AS ClientCode,RTRIM(c.Nom_Cliente) AS Client,RTRIM(p.Des_General) AS Description,p.Fec_Creacion AS CreatedAt
     FROM dbo.TG_PurOrd p
    LEFT JOIN dbo.TG_Cliente c ON c.Cod_Cliente=p.Cod_Cliente
    WHERE (@Po='' OR p.Cod_PurOrd LIKE @PoPattern ESCAPE '~')
      AND (@Search='' OR p.Cod_PurOrd LIKE @Pattern ESCAPE '~' OR c.Nom_Cliente LIKE @Pattern ESCAPE '~' OR p.Des_General LIKE @Pattern ESCAPE '~')
    )
    SELECT Po,ClientCode,Client,Description,CreatedAt FROM Paged WHERE RowNumber>@Offset AND RowNumber<=@Offset+50 ORDER BY RowNumber;
END;
GO

/*
 Verificar PO y cliente
 Origen: backend/src/Intranet.Data/Repositories/AviosRepository.cs:32
*/
CREATE PROCEDURE [dbo].[PA_TG_PURORD_S0002]
    @Po char(20),
    @ClientCode char(5)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) FROM dbo.TG_PurOrd WHERE Cod_PurOrd=@Po AND Cod_Cliente=@ClientCode ;
END;
GO

/*
 Detalle de PO por lote, estilo, color y talla
 Origen: backend/src/Intranet.Data/Repositories/AviosRepository.cs:37
*/
CREATE PROCEDURE [dbo].[PA_TG_LOTCOLTAL_S0001]
    @Po char(20),
    @ClientCode char(5)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT RTRIM(d.Cod_LotPurOrd) Lot,RTRIM(d.Cod_EstCli) Style,RTRIM(e.Des_EstCli) Description,
        RTRIM(d.Cod_ColCli) ColorCode,RTRIM(c.Nom_ColCli) Color,RTRIM(d.Cod_Talla) Size,
        CAST(ISNULL(d.Num_PreReq,0) AS decimal(18,5)) Required,CAST(ISNULL(d.Num_PreDes,0) AS decimal(18,5)) Shipped,RTRIM(d.UPC) Upc
    FROM dbo.TG_LotColTal d
    LEFT JOIN dbo.TG_EstCli e ON e.Cod_Cliente=d.Cod_Cliente AND e.Cod_EstCli=d.Cod_EstCli
    LEFT JOIN dbo.TG_ColCli c ON c.Cod_Cliente=d.Cod_Cliente AND c.Cod_ColCli=d.Cod_ColCli
    WHERE d.Cod_PurOrd=@Po AND d.Cod_Cliente=@ClientCode
    ORDER BY d.Cod_LotPurOrd,d.Cod_EstCli,d.Cod_ColCli,d.Cod_Talla ;
END;
GO

/*
 Lectura de cabecera para identidad con permiso de lectura global
 Origen: backend/src/Intranet.Data/Repositories/AviosRepository.cs:84
 Nota: Invocar únicamente después de validar la identidad contra ReadAllWindowsUsers, como hace hoy el backend.
*/
CREATE PROCEDURE [dbo].[PA_LG_ORDCOMP_S0002]
    @Series char(3),
    @Number char(6)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT o.Cod_StaOrdComp, o.Cod_ClaOrdComp, o.Fec_Creacion,
           p.Des_Proveedor AS PROVEEDOR, c.Nom_Cliente AS NOM_CLIENTE
    FROM dbo.LG_ORDCOMP o
    LEFT JOIN dbo.LG_PROVEEDOR p ON p.Cod_Proveedor = o.Cod_Proveedor
    LEFT JOIN dbo.TG_CLIENTE c ON c.Cod_Cliente = o.COD_CLIENTE
    WHERE o.Ser_OrdComp = @Series AND o.Cod_OrdComp = @Number ;
END;
GO

/*
 NP vinculadas a secuencias de la OC, con respaldo desde el ítem
 Origen: backend/src/Intranet.Data/Repositories/AviosRepository.cs:106
*/
CREATE PROCEDURE [dbo].[PA_LG_ORDCOMPITEMREQ_S0002]
    @Series char(3),
    @Number char(6)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Sec_OrdComp,Cod_OrdPro FROM dbo.LG_ORDCOMPITEMREQ WHERE Ser_OrdComp=@Series AND Cod_OrdComp=@Number UNION SELECT Sec_OrdComp,Cod_OrdPro FROM dbo.LG_ORDCOMPITEM WHERE Ser_OrdComp=@Series AND Cod_OrdComp=@Number ;
END;
GO

/*
 Verificar usuario de Gestión; lectura de SEGURIDAD
 Origen: backend/src/Intranet.Data/Repositories/GestionLoginRepository.cs:11
 Nota: El procedimiento se crea en HIALPESA y mantiene la referencia SEGURIDAD.dbo.SEG_Usuarios. No registra ni almacena contraseñas.
*/
CREATE PROCEDURE [dbo].[PA_SEG_USUARIOS_S0001]
    @Username varchar(15),
    @Password varchar(128)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (2) RTRIM(Cod_Usuario) Code,LTRIM(RTRIM(ISNULL(Nom_Usuarioentonc,''))) Name
    FROM SEGURIDAD.dbo.SEG_Usuarios
    WHERE Cod_Usuario=@Username
      AND [Password] COLLATE Latin1_General_BIN2=@Password COLLATE Latin1_General_BIN2
      AND DATALENGTH([Password])=DATALENGTH(@Password)
      AND ISNULL(is_active,1)=1 ;
END;
GO

/*
 Proveedor, anexo y datos de origen para importación; lectura o bloqueo
 Origen: backend/src/Intranet.Data/Repositories/ImportsRepository.cs:9; backend/src/Intranet.Data/Repositories/ImportsRepository.cs:72
*/
CREATE PROCEDURE [dbo].[PA_LG_ORDCOMP_S0003]
    @Series char(3),
    @Number char(6),
    @LockRow bit = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF @LockRow=1
    BEGIN
     SELECT RTRIM(o.Cod_Proveedor) SupplierCode,RTRIM(p.Des_Proveedor) Supplier,'P' AnnexType,
    ISNULL((SELECT CASE WHEN COUNT(*)=1 THEN RTRIM(MAX(a.Cod_Anxo)) ELSE '' END FROM CN_ANEXOSCONTABLES a WHERE a.Cod_TipAnex='P' AND RTRIM(a.Num_Ruc)=RTRIM(p.Num_Ruc) AND RTRIM(p.Num_Ruc)<>''),'') Annex,RTRIM(o.Cod_Moneda) Currency,RTRIM(ISNULL(o.Num_Importacion,'')) ExistingImport,RTRIM(p.Origen) SupplierOrigin
    FROM LG_ORDCOMP o WITH (UPDLOCK,HOLDLOCK) JOIN LG_PROVEEDOR p ON p.Cod_Proveedor=o.Cod_Proveedor
    WHERE o.Ser_OrdComp=@Series AND o.Cod_OrdComp=@Number;
    END
    ELSE
    BEGIN
     SELECT RTRIM(o.Cod_Proveedor) SupplierCode,RTRIM(p.Des_Proveedor) Supplier,'P' AnnexType,
    ISNULL((SELECT CASE WHEN COUNT(*)=1 THEN RTRIM(MAX(a.Cod_Anxo)) ELSE '' END FROM CN_ANEXOSCONTABLES a WHERE a.Cod_TipAnex='P' AND RTRIM(a.Num_Ruc)=RTRIM(p.Num_Ruc) AND RTRIM(p.Num_Ruc)<>''),'') Annex,RTRIM(o.Cod_Moneda) Currency,RTRIM(ISNULL(o.Num_Importacion,'')) ExistingImport,RTRIM(p.Origen) SupplierOrigin
    FROM LG_ORDCOMP o  JOIN LG_PROVEEDOR p ON p.Cod_Proveedor=o.Cod_Proveedor
    WHERE o.Ser_OrdComp=@Series AND o.Cod_OrdComp=@Number;
    END ;
END;
GO

/*
 Listado paginado de importaciones y total; filtros antes construidos en C#
 Origen: backend/src/Intranet.Data/Repositories/ImportsRepository.cs:22; backend/src/Intranet.Data/Repositories/ImportsRepository.cs:25; backend/src/Intranet.Data/Repositories/ImportsRepository.cs:26
 Nota: Dos resultados: página (hasta 51 para hasMore) y total. ReadAll solo debe derivarse de la identidad autenticada/configurada del servidor.
*/
CREATE PROCEDURE [dbo].[PA_LG_IMPORTACIONES_S0001]
    @User varchar(15),
    @ReadAll bit = 0,
    @Search nvarchar(70) = N'',
    @Offset int = 0,
    @End int = 51
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Term nvarchar(70)=LTRIM(RTRIM(ISNULL(@Search,N'')));
    DECLARE @IsNumeric bit=CASE WHEN LEN(@Term)>0 AND @Term COLLATE Latin1_General_BIN2 NOT LIKE N'%[^0-9]%' THEN 1 ELSE 0 END;
    DECLARE @IsReference bit=CASE WHEN LEN(@Term)=10 AND @Term COLLATE Latin1_General_BIN2 LIKE N'[0-9][0-9][0-9]-[0-9][0-9][0-9][0-9][0-9][0-9]' THEN 1 ELSE 0 END;
    DECLARE @Prefix nvarchar(71)=@Term+N'%',@Pattern nvarchar(72)=N'%'+@Term+N'%';
    DECLARE @Series char(3)=CASE WHEN @IsReference=1 THEN LEFT(@Term,3) ELSE '' END;
    DECLARE @OrderNumber char(6)=CASE WHEN @IsReference=1 THEN RIGHT(@Term,6) ELSE '' END;
    ;WITH list AS (
      SELECT i.Num_Importacion Number,i.Fec_RegDoc RegisteredAt,i.Flg_Status Status,a.Des_Anexo Supplier,
        i.Cod_Moneda Currency,g.Des_OrigenImportacion Origin,i.Ser_OrdComp Series,i.Cod_OrdComp OrderNumber,
        ROW_NUMBER() OVER(ORDER BY i.Fec_Creacion DESC,i.Num_Importacion DESC) rn
      FROM LG_IMPORTACIONES i LEFT JOIN CN_ANEXOSCONTABLES a ON a.Cod_TipAnex=i.Cod_TipAnex AND a.Cod_Anxo=i.Cod_Anxo
      LEFT JOIN LG_ORIGEN_IMPORTACION g ON g.Cod_OrigenImportacion=i.Cod_OrigenImportacion
      WHERE (@ReadAll=1 OR EXISTS(SELECT 1 FROM LG_ORDCOMP o JOIN LG_SEGORDCOMP s ON s.Cod_ClaOrdComp=o.Cod_ClaOrdComp WHERE o.Num_Importacion=i.Num_Importacion AND s.Cod_Usuario=@User)) AND ((@Term=N'')
     OR (@IsNumeric=1 AND LEN(@Term)>=8 AND i.Num_Importacion=@Term)
     OR (@IsNumeric=1 AND LEN(@Term)<8 AND (i.Num_Importacion LIKE @Prefix OR EXISTS(SELECT 1 FROM LG_ORDCOMP o WHERE o.Num_Importacion=i.Num_Importacion AND o.Cod_OrdComp LIKE @Prefix)))
     OR (@IsNumeric=0 AND @IsReference=1 AND EXISTS(SELECT 1 FROM LG_ORDCOMP o WHERE o.Num_Importacion=i.Num_Importacion AND o.Ser_OrdComp=@Series AND o.Cod_OrdComp=@OrderNumber))
     OR (@IsNumeric=0 AND @IsReference=0 AND @Term<>N'' AND a.Des_Anexo LIKE @Pattern))
    ) SELECT Number,RegisteredAt,Status,Supplier,Currency,Origin,Series,OrderNumber FROM list WHERE rn>@Offset AND rn<=@End ORDER BY rn OPTION (RECOMPILE);
    SELECT COUNT(*) FROM LG_IMPORTACIONES i LEFT JOIN CN_ANEXOSCONTABLES a ON a.Cod_TipAnex=i.Cod_TipAnex AND a.Cod_Anxo=i.Cod_Anxo LEFT JOIN LG_ORIGEN_IMPORTACION g ON g.Cod_OrigenImportacion=i.Cod_OrigenImportacion WHERE (@ReadAll=1 OR EXISTS(SELECT 1 FROM LG_ORDCOMP o JOIN LG_SEGORDCOMP s ON s.Cod_ClaOrdComp=o.Cod_ClaOrdComp WHERE o.Num_Importacion=i.Num_Importacion AND s.Cod_Usuario=@User)) AND ((@Term=N'')
     OR (@IsNumeric=1 AND LEN(@Term)>=8 AND i.Num_Importacion=@Term)
     OR (@IsNumeric=1 AND LEN(@Term)<8 AND (i.Num_Importacion LIKE @Prefix OR EXISTS(SELECT 1 FROM LG_ORDCOMP o WHERE o.Num_Importacion=i.Num_Importacion AND o.Cod_OrdComp LIKE @Prefix)))
     OR (@IsNumeric=0 AND @IsReference=1 AND EXISTS(SELECT 1 FROM LG_ORDCOMP o WHERE o.Num_Importacion=i.Num_Importacion AND o.Ser_OrdComp=@Series AND o.Cod_OrdComp=@OrderNumber))
     OR (@IsNumeric=0 AND @IsReference=0 AND @Term<>N'' AND a.Des_Anexo LIKE @Pattern)) OPTION (RECOMPILE);
END;
GO

/*
 Consultar modo de numeración de importaciones; lectura o bloqueo
 Origen: backend/src/Intranet.Data/Repositories/ImportsRepository.cs:45; backend/src/Intranet.Data/Repositories/ImportsRepository.cs:75
*/
CREATE PROCEDURE [dbo].[PA_TG_CONTROL_S0001]
    @LockRow bit = 0
AS
BEGIN
    SET NOCOUNT ON;

    IF @LockRow=1
    BEGIN
     SELECT Flg_NumImportacion_Manual FROM TG_CONTROL WITH (UPDLOCK,HOLDLOCK);
    END
    ELSE
    BEGIN
     SELECT Flg_NumImportacion_Manual FROM TG_CONTROL;
    END ;
END;
GO

/*
 Validar anexo contable del proveedor por RUC
 Origen: backend/src/Intranet.Data/Repositories/ImportsRepository.cs:46; backend/src/Intranet.Data/Repositories/ImportsRepository.cs:76
*/
CREATE PROCEDURE [dbo].[PA_CN_ANEXOSCONTABLES_S0001]
    @AnnexType char(1),
    @Annex char(4),
    @SupplierCode char(12)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) FROM CN_ANEXOSCONTABLES a JOIN LG_PROVEEDOR p ON RTRIM(p.Num_Ruc)=RTRIM(a.Num_Ruc) WHERE a.Cod_TipAnex=@AnnexType AND a.Cod_Anxo=@Annex AND p.Cod_Proveedor=@SupplierCode ;
END;
GO

/*
 Catálogos de importaciones en un único resultado con Kind
 Origen: backend/src/Intranet.Data/Repositories/ImportsRepository.cs:53
 Nota: UNION ALL original: orígenes, embarques, países, tipos de ítem y áreas. Tabla principal: LG_ORIGEN_IMPORTACION.
*/
CREATE PROCEDURE [dbo].[PA_LG_ORIGEN_IMPORTACION_S0001]

AS
BEGIN
    SET NOCOUNT ON;

    SELECT 'origins' Kind,RTRIM(Cod_OrigenImportacion) Code,RTRIM(Des_OrigenImportacion) Name FROM LG_ORIGEN_IMPORTACION
    UNION ALL SELECT 'shipments',RTRIM(Cod_Embarque),RTRIM(Des_Embarque) FROM TG_TIPEMB
    UNION ALL SELECT 'countries',RTRIM(Cod_Pais),RTRIM(Descripcion) FROM CN_PAISES
    UNION ALL SELECT 'itemTypes',RTRIM(Cod_TipIte),RTRIM(Des_TipIte) FROM CN_TIPOITEM WHERE Flg_Importacion='S'
    UNION ALL SELECT 'areas',RTRIM(Cod_AreaResponsable),RTRIM(Des_AreaResponsable) FROM CN_AreasResponsables ;
END;
GO

/*
 Validar contexto de conexión para registrar importaciones
 Origen: backend/src/Intranet.Data/Repositories/ImportsRepository.cs:68
 Nota: Consulta de contexto sin FROM. Se agrupa bajo la tabla del proceso LG_IMPORTACIONES; no lee ni modifica sus filas.
*/
CREATE PROCEDURE [dbo].[PA_LG_IMPORTACIONES_S0002]
    @Windows nvarchar(256)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CASE WHEN ORIGINAL_LOGIN()=@Windows AND IS_SRVROLEMEMBER('sysadmin')=1 THEN 1 ELSE 0 END ;
END;
GO

/*
 Validar tipo de ítem habilitado para importaciones
 Origen: backend/src/Intranet.Data/Repositories/ImportsRepository.cs:78
*/
CREATE PROCEDURE [dbo].[PA_CN_TIPOITEM_S0001]
    @ItemType char(3)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) FROM CN_TIPOITEM WHERE Cod_TipIte=@ItemType AND Flg_Importacion='S' ;
END;
GO

/*
 Bloquear correlativo anual de importaciones
 Origen: backend/src/Intranet.Data/Repositories/ImportsRepository.cs:79
 Nota: Ejecutar dentro de la transacción de creación, antes del SP reutilizado.
*/
CREATE PROCEDURE [dbo].[PA_TG_ANOCONTROL_S0001]
    @Year char(4)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Ult_Importacion FROM TG_ANOCONTROL WITH (UPDLOCK,HOLDLOCK) WHERE Ano=@Year ;
END;
GO

/*
 Verificar número de importación asociado a la OC
 Origen: backend/src/Intranet.Data/Repositories/ImportsRepository.cs:85
*/
CREATE PROCEDURE [dbo].[PA_LG_ORDCOMP_S0004]
    @Series char(3),
    @Number char(6)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Num_Importacion FROM LG_ORDCOMP WHERE Ser_OrdComp=@Series AND Cod_OrdComp=@Number ;
END;
GO

/*
 Listado paginado de OC y total; filtros antes construidos en C#
 Origen: backend/src/Intranet.Data/Repositories/RegistrationReadRepository.cs:20; backend/src/Intranet.Data/Repositories/RegistrationReadRepository.cs:21
 Nota: Dos resultados: página y total. ReadAll solo debe derivarse de la identidad autenticada/configurada del servidor.
*/
CREATE PROCEDURE [dbo].[PA_LG_ORDCOMP_S0005]
    @User varchar(15),
    @ReadAll bit = 0,
    @Search nvarchar(70) = N'',
    @Offset int = 0,
    @End int = 51
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Term nvarchar(70)=LTRIM(RTRIM(ISNULL(@Search,N'')));
    DECLARE @IsNumeric bit=CASE WHEN LEN(@Term)>0 AND @Term COLLATE Latin1_General_BIN2 NOT LIKE N'%[^0-9]%' THEN 1 ELSE 0 END;
    DECLARE @IsReference bit=CASE WHEN LEN(@Term)=10 AND @Term COLLATE Latin1_General_BIN2 LIKE N'[0-9][0-9][0-9]-[0-9][0-9][0-9][0-9][0-9][0-9]' THEN 1 ELSE 0 END;
    DECLARE @Prefix nvarchar(71)=@Term+N'%',@Pattern nvarchar(72)=N'%'+@Term+N'%';
    DECLARE @Series char(3)=CASE WHEN @IsReference=1 THEN LEFT(@Term,3) ELSE '' END;
    DECLARE @Number char(6)=CASE WHEN @IsReference=1 THEN RIGHT(@Term,6) ELSE '' END;
    ;WITH orders AS (
      SELECT RTRIM(o.Ser_OrdComp) Series,RTRIM(o.Cod_OrdComp) Number,RTRIM(p.Des_Proveedor) Supplier,
        RTRIM(o.Cod_StaOrdComp) Status,RTRIM(o.Cod_ClaOrdComp) Class,o.Fec_Creacion CreatedAt,
        ROW_NUMBER() OVER(ORDER BY o.Fec_Creacion DESC,o.Ser_OrdComp DESC,o.Cod_OrdComp DESC) rn
      FROM dbo.LG_ORDCOMP o LEFT JOIN dbo.LG_PROVEEDOR p ON p.Cod_Proveedor=o.Cod_Proveedor
      WHERE (@ReadAll=1 OR EXISTS(SELECT 1 FROM dbo.LG_SEGORDCOMP s WHERE s.Cod_Usuario=@User AND s.Cod_ClaOrdComp=o.Cod_ClaOrdComp))
      AND ((@Term=N'')
     OR (@IsReference=1 AND o.Ser_OrdComp=@Series AND o.Cod_OrdComp=@Number)
     OR (@IsReference=0 AND @IsNumeric=1 AND LEN(@Term)=6 AND o.Cod_OrdComp=@Term)
     OR (@IsReference=0 AND @IsNumeric=1 AND LEN(@Term)<>6 AND o.Cod_OrdComp LIKE @Prefix)
     OR (@IsReference=0 AND @IsNumeric=0 AND @Term<>N'' AND p.Des_Proveedor LIKE @Pattern))
    ) SELECT Series,Number,Supplier,Status,Class,CreatedAt FROM orders WHERE rn>@Offset AND rn<=@End ORDER BY rn OPTION (RECOMPILE);
    SELECT COUNT(*) FROM dbo.LG_ORDCOMP o LEFT JOIN dbo.LG_PROVEEDOR p ON p.Cod_Proveedor=o.Cod_Proveedor WHERE (@ReadAll=1 OR EXISTS(SELECT 1 FROM dbo.LG_SEGORDCOMP s WHERE s.Cod_Usuario=@User AND s.Cod_ClaOrdComp=o.Cod_ClaOrdComp)) AND ((@Term=N'')
     OR (@IsReference=1 AND o.Ser_OrdComp=@Series AND o.Cod_OrdComp=@Number)
     OR (@IsReference=0 AND @IsNumeric=1 AND LEN(@Term)=6 AND o.Cod_OrdComp=@Term)
     OR (@IsReference=0 AND @IsNumeric=1 AND LEN(@Term)<>6 AND o.Cod_OrdComp LIKE @Prefix)
     OR (@IsReference=0 AND @IsNumeric=0 AND @Term<>N'' AND p.Des_Proveedor LIKE @Pattern)) OPTION (RECOMPILE);
END;
GO

/*
 Catálogo de registro: LG_CONDVENT
 Origen: backend/src/Intranet.Data/Repositories/RegistrationReadRepository.cs:39
 Nota: Consulta 1 de las seis que hoy ejecuta RegistrationReadRepository.Catalogs.
*/
CREATE PROCEDURE [dbo].[PA_LG_CONDVENT_S0001]

AS
BEGIN
    SET NOCOUNT ON;

    SELECT Cod_CondVent Code,Des_CondVent Description FROM dbo.LG_CONDVENT WHERE ISNULL(FLG_ACTIVO,'S')<>'N' ORDER BY Des_CondVent ;
END;
GO

/*
 Catálogo de registro: LG_DSCTOS
 Origen: backend/src/Intranet.Data/Repositories/RegistrationReadRepository.cs:39
 Nota: Consulta 2 de las seis que hoy ejecuta RegistrationReadRepository.Catalogs.
*/
CREATE PROCEDURE [dbo].[PA_LG_DSCTOS_S0001]

AS
BEGIN
    SET NOCOUNT ON;

    SELECT Cod_Descuento Code,CONVERT(varchar(30),CAST(ISNULL(Porcentaje1,0) AS decimal(18,2)))+' - '+CONVERT(varchar(30),CAST(ISNULL(Porcentaje2,0) AS decimal(18,2))) Description FROM dbo.LG_DSCTOS ORDER BY Cod_Descuento ;
END;
GO

/*
 Catálogo de registro: TG_MONEDA
 Origen: backend/src/Intranet.Data/Repositories/RegistrationReadRepository.cs:39
 Nota: Consulta 3 de las seis que hoy ejecuta RegistrationReadRepository.Catalogs.
*/
CREATE PROCEDURE [dbo].[PA_TG_MONEDA_S0001]

AS
BEGIN
    SET NOCOUNT ON;

    SELECT Cod_Moneda Code,Nom_Moneda Description FROM dbo.TG_MONEDA WHERE ISNULL(Flg_Activa,'S')<>'N' ORDER BY Cod_Moneda ;
END;
GO

/*
 Catálogo de registro: LG_LUGENTR
 Origen: backend/src/Intranet.Data/Repositories/RegistrationReadRepository.cs:39
 Nota: Consulta 4 de las seis que hoy ejecuta RegistrationReadRepository.Catalogs.
*/
CREATE PROCEDURE [dbo].[PA_LG_LUGENTR_S0001]

AS
BEGIN
    SET NOCOUNT ON;

    SELECT Cod_LugEntr Code,Des_LugEntr Description FROM dbo.LG_LUGENTR ORDER BY Des_LugEntr ;
END;
GO

/*
 Catálogo de registro: TG_CENCOSTO
 Origen: backend/src/Intranet.Data/Repositories/RegistrationReadRepository.cs:39
 Nota: Consulta 5 de las seis que hoy ejecuta RegistrationReadRepository.Catalogs.
*/
CREATE PROCEDURE [dbo].[PA_TG_CENCOSTO_S0001]

AS
BEGIN
    SET NOCOUNT ON;

    SELECT Cod_CenCost Code,Des_CenCost Description FROM dbo.TG_CENCOSTO WHERE ISNULL(Flg_Activo,'S')<>'N' ORDER BY Des_CenCost ;
END;
GO

/*
 Catálogo de registro: LG_CLAORDCOMP
 Origen: backend/src/Intranet.Data/Repositories/RegistrationReadRepository.cs:39
 Nota: Consulta 6 de las seis que hoy ejecuta RegistrationReadRepository.Catalogs.
*/
CREATE PROCEDURE [dbo].[PA_LG_CLAORDCOMP_S0001]

AS
BEGIN
    SET NOCOUNT ON;

    SELECT Cod_ClaOrdComp Code,Des_ClaOrdComp Description FROM dbo.LG_CLAORDCOMP ORDER BY Des_ClaOrdComp ;
END;
GO

/*
 Grupos de manufactura asociados a la OC
 Origen: backend/src/Intranet.Data/Repositories/RegistrationReadRepository.cs:53
*/
CREATE PROCEDURE [dbo].[PA_ES_GRUPOLOG_S0001]
    @Series char(3),
    @Number char(6)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DISTINCT RTRIM(g.Cod_GrupoLog) Code,RTRIM(g.Des_Grupo) Description FROM dbo.ES_GRUPOLOG g WHERE EXISTS(SELECT 1 FROM dbo.LG_OrdComp_Grupos x WHERE x.Ser_OrdComp=@Series AND x.Cod_OrdComp=@Number AND x.Cod_Grupo=g.Cod_GrupoLog) ORDER BY Code ;
END;
GO

/*
 OP del grupo asignado a la OC
 Origen: backend/src/Intranet.Data/Repositories/RegistrationReadRepository.cs:59
*/
CREATE PROCEDURE [dbo].[PA_ES_ORDPRO_S0002]
    @Series char(3),
    @Number char(6),
    @Group char(8)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DISTINCT RTRIM(p.Cod_OrdPro) Code,RTRIM(ISNULL(e.Des_EstPro,'')) Description FROM dbo.ES_ORDPRO p LEFT JOIN dbo.ES_ESTPRO e ON e.Cod_EstPro=p.Cod_EstPro WHERE p.Cod_GrupoLog=@Group AND EXISTS(SELECT 1 FROM dbo.LG_OrdComp_Grupos g WHERE g.Ser_OrdComp=@Series AND g.Cod_OrdComp=@Number AND g.Cod_Grupo=@Group) ORDER BY Code ;
END;
GO

/*
 Valores editables del detalle de OC
 Origen: backend/src/Intranet.Data/Repositories/RegistrationReadRepository.cs:77
*/
CREATE PROCEDURE [dbo].[PA_LG_ORDCOMPITEM_S0001]
    @Series char(3),
    @Number char(6),
    @Sequence char(3)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Can_Comprada Purchased,Pre_Unitario UnitPrice,RTRIM(Cod_Descuento) DiscountCode,Porc_IGV TaxPercent,Fec_Entrega_Inicio DeliveryStart,Fec_Entrega_Fin DeliveryEnd,ISNULL(CONVERT(varchar(2000),Observaciones),'') Observations FROM dbo.LG_ORDCOMPITEM WHERE Ser_OrdComp=@Series AND Cod_OrdComp=@Number AND Sec_OrdComp=@Sequence ;
END;
GO

/*
 Valores editables de cabecera y nombre del proveedor
 Origen: backend/src/Intranet.Data/Repositories/RegistrationReadRepository.cs:82
 Nota: Conserva los dos conjuntos de resultados y su orden.
*/
CREATE PROCEDURE [dbo].[PA_LG_ORDCOMP_S0006]
    @Series char(3),
    @Number char(6)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT RTRIM(o.Cod_Proveedor) SupplierCode,RTRIM(o.Cod_CondVent) ConditionCode,RTRIM(o.Cod_Descuento) DiscountCode,
      RTRIM(o.Cod_Moneda) CurrencyCode,RTRIM(o.Cod_LugEntr) DeliveryLocationCode,RTRIM(o.Cod_CenCost) CostCenterCode,
      o.Porc_IGV TaxPercent,o.Fec_Entrega_Inicio DeliveryStart,o.Fec_Entrega_Fin DeliveryEnd,ISNULL(CONVERT(varchar(2000),o.Observaciones),'') Observations
    FROM dbo.LG_ORDCOMP o WHERE o.Ser_OrdComp=@Series AND o.Cod_OrdComp=@Number;
    SELECT RTRIM(p.Des_Proveedor) FROM dbo.LG_ORDCOMP o INNER JOIN dbo.LG_PROVEEDOR p ON p.Cod_Proveedor=o.Cod_Proveedor WHERE o.Ser_OrdComp=@Series AND o.Cod_OrdComp=@Number;
END;
GO

/*
 Catálogo de fábricas
 Origen: backend/src/Intranet.Data/Repositories/RegistrationReadRepository.cs:102
*/
CREATE PROCEDURE [dbo].[PA_TG_FABRICA_S0001]

AS
BEGIN
    SET NOCOUNT ON;

    SELECT RTRIM(Abr_Fabrica) Code,RTRIM(Nom_Fabrica) Description FROM dbo.TG_FABRICA WHERE ISNULL(Abr_Fabrica,'')<>'' ORDER BY Abr_Fabrica ;
END;
GO

/*
 Buscar grupos por fábrica y OP
 Origen: backend/src/Intranet.Data/Repositories/RegistrationReadRepository.cs:107
*/
CREATE PROCEDURE [dbo].[PA_ES_GRUPOLOG_S0002]
    @Factory char(4),
    @Op char(5)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DISTINCT RTRIM(g.Cod_GrupoLog) Code,RTRIM(g.Des_Grupo) Description
    FROM dbo.ES_GRUPOLOG g
    INNER JOIN dbo.ES_ORDPRO p ON p.Cod_GrupoLog=g.Cod_GrupoLog
    INNER JOIN dbo.TG_FABRICA f ON f.Cod_Fabrica=p.Cod_Fabrica
    WHERE f.Abr_Fabrica=@Factory AND p.Cod_OrdPro=@Op
    ORDER BY Code ;
END;
GO

/*
 Valores por defecto del proveedor: condición, descuento e IGV
 Origen: backend/src/Intranet.Data/Repositories/RegistrationReadRepository.cs:120
*/
CREATE PROCEDURE [dbo].[PA_LG_PROVEEDOR_S0001]
    @Code char(12)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT RTRIM(ISNULL(Cod_CondVent,'')) ConditionCode,RTRIM(ISNULL(Cod_Descuento,'')) DiscountCode,RTRIM(ISNULL(Origen,'')) Origin,
      CAST(CASE WHEN Origen='E' THEN 0 WHEN Origen='N' THEN 18 ELSE NULL END AS decimal(18,2)) TaxPercent
    FROM dbo.LG_PROVEEDOR WHERE Cod_Proveedor=@Code AND ISNULL(Flg_Activo,'S')<>'N' ;
END;
GO

/*
 Buscar proveedores activos
 Origen: backend/src/Intranet.Data/Repositories/RegistrationReadRepository.cs:131
*/
CREATE PROCEDURE [dbo].[PA_LG_PROVEEDOR_S0002]
    @Search nvarchar(72)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 30 Cod_Proveedor Code,Des_Proveedor Description FROM dbo.LG_PROVEEDOR WHERE ISNULL(Flg_Activo,'S')<>'N' AND (Cod_Proveedor LIKE @Search OR Des_Proveedor LIKE @Search) ORDER BY Des_Proveedor ;
END;
GO

/*
 Buscar grupos de manufactura
 Origen: backend/src/Intranet.Data/Repositories/RegistrationReadRepository.cs:132
*/
CREATE PROCEDURE [dbo].[PA_ES_GRUPOLOG_S0003]
    @Search nvarchar(72)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 30 Cod_GrupoLog Code,Des_Grupo Description FROM dbo.ES_GRUPOLOG WHERE (Cod_GrupoLog LIKE @Search OR Des_Grupo LIKE @Search) AND EXISTS(SELECT 1 FROM dbo.ES_ORDPRO p WHERE p.Cod_GrupoLog=ES_GRUPOLOG.Cod_GrupoLog) ORDER BY Cod_GrupoLog DESC ;
END;
GO

/*
 Cabecera y líneas de impresión de OC
 Origen: backend/src/Intranet.Data/Repositories/RegistrationReadRepository.cs:148
 Nota: Conserva los dos conjuntos de resultados y su orden.
*/
CREATE PROCEDURE [dbo].[PA_LG_ORDCOMP_S0007]
    @Series char(3),
    @Number char(6)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT o.Cod_Moneda,o.Cod_Grupo,o.Observaciones,o.Porc_IGV,o.Fec_Entrega_Inicio,o.Fec_Entrega_Fin,c.Des_CondVent,l.Des_LugEntr
    FROM dbo.LG_ORDCOMP o LEFT JOIN dbo.LG_CONDVENT c ON c.Cod_CondVent=o.Cod_CondVent LEFT JOIN dbo.LG_LUGENTR l ON l.Cod_LugEntr=o.Cod_LugEntr
    WHERE o.Ser_OrdComp=@Series AND o.Cod_OrdComp=@Number;
    SELECT i.Sec_OrdComp,i.Cod_Item,a.Des_Item,i.Cod_Talla,i.Can_Comprada,i.Pre_Unitario FROM dbo.LG_ORDCOMPITEM i LEFT JOIN dbo.LG_ITEM a ON a.Cod_Item=i.Cod_Item
    WHERE i.Ser_OrdComp=@Series AND i.Cod_OrdComp=@Number ORDER BY i.Sec_OrdComp;
END;
GO

/*
 Desglose de impresión por requerimiento
 Origen: backend/src/Intranet.Data/Repositories/RegistrationReadRepository.cs:173
*/
CREATE PROCEDURE [dbo].[PA_LG_ORDCOMPITEM_S0002]
    @Series char(3),
    @Number char(6)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT i.Sec_OrdComp Sequence,RTRIM(i.Cod_Item)+'-'+RTRIM(ISNULL(a.Des_Item,'')) Item,
      RTRIM(ISNULL(NULLIF(ic.Des_Comb,''),ep.Des_Present)) Combination,
      RTRIM(ISNULL(NULLIF(r.Cod_Medida,''),NULLIF(r.Cod_Talla,''))) MeasureCode,
      RTRIM(ISNULL(im.Descripcion,'')) MeasureDescription,
      RTRIM(ISNULL(NULLIF(r.Cod_EstCli,''),i.Cod_EstCli)) CustomerStyle,
      RTRIM(ISNULL(r.Cod_OrdPro,i.Cod_OrdPro)) ProductionOrder,
      ISNULL(r.Can_Requerida,i.Can_Requerida) Required,i.Can_Comprada TotalPurchased,
      CASE WHEN r.Sec_OrdComp IS NULL THEN 0 ELSE 1 END HasRequirement
    FROM LG_ORDCOMPITEM i LEFT JOIN LG_ITEM a ON a.Cod_Item=i.Cod_Item
    LEFT JOIN LG_ORDCOMPITEMREQ r ON r.Ser_OrdComp=i.Ser_OrdComp AND r.Cod_OrdComp=i.Cod_OrdComp AND r.Sec_OrdComp=i.Sec_OrdComp
    LEFT JOIN LG_ITEMCOMB ic ON ic.Cod_Item=i.Cod_Item AND ic.Cod_Comb=ISNULL(NULLIF(r.Cod_Comb,''),i.Cod_Comb)
    LEFT JOIN ES_ORDPRO op ON op.Cod_Fabrica=r.Cod_Fabrica AND op.Cod_OrdPro=r.Cod_OrdPro
    LEFT JOIN ES_ESTPROPRE ep ON ep.Cod_EstPro=op.Cod_EstPro AND ep.Cod_Present=r.Cod_Present
    LEFT JOIN LG_ITEMMED im ON im.Cod_Item=i.Cod_Item AND im.Cod_Medida=ISNULL(NULLIF(r.Cod_Medida,''),ISNULL(NULLIF(r.Cod_Talla,''),i.Cod_Talla))
    WHERE i.Ser_OrdComp=@Series AND i.Cod_OrdComp=@Number
    ORDER BY i.Sec_OrdComp,r.Cod_OrdPro,r.Cod_Present,r.Cod_Talla ;
END;
GO

/*
 Validar permiso de escritura de OC
 Origen: backend/src/Intranet.Data/Repositories/SqlOrderWriteGateway.cs:19
*/
CREATE PROCEDURE [dbo].[PA_LG_SEGORDCOMP_S0001]
    @WindowsUser nvarchar(256),
    @GestionUser varchar(15)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CASE WHEN (ORIGINAL_LOGIN()=@WindowsUser AND IS_SRVROLEMEMBER('sysadmin')=1)
        OR EXISTS(SELECT 1 FROM dbo.LG_SEGORDCOMP WHERE Cod_Usuario=@GestionUser AND Cod_ClaOrdComp='01' AND Tip_Act='A')
        THEN 1 ELSE 0 END ;
END;
GO

/*
 Bloquear numeración de OC
 Origen: backend/src/Intranet.Data/Repositories/SqlOrderWriteGateway.cs:45
 Nota: Conserva los bloqueos originales: usar la misma transacción del llamador.
*/
CREATE PROCEDURE [dbo].[PA_LG_NUMORDCOMP_S0001]

AS
BEGIN
    SET NOCOUNT ON;

    SELECT n.ULT_ORDCOMP FROM dbo.LG_NUMORDCOMP n WITH (UPDLOCK,HOLDLOCK) INNER JOIN dbo.LG_CLAORDCOMP c ON c.Tip_Item=n.Tip_Item AND c.Tip_Presentacion=n.Tip_Presentacion WHERE c.Cod_ClaOrdComp='01' ;
END;
GO

/*
 Verificar existencia del grupo
 Origen: backend/src/Intranet.Data/Repositories/SqlOrderWriteGateway.cs:60
*/
CREATE PROCEDURE [dbo].[PA_ES_GRUPOLOG_S0004]
    @Group char(8)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) FROM dbo.ES_GRUPOLOG WHERE Cod_GrupoLog=@Group ;
END;
GO

/*
 Verificar vínculo grupo-OC con bloqueo
 Origen: backend/src/Intranet.Data/Repositories/SqlOrderWriteGateway.cs:61
 Nota: Conserva los bloqueos originales: usar la misma transacción del llamador.
*/
CREATE PROCEDURE [dbo].[PA_LG_ORDCOMP_GRUPOS_S0001]
    @Series char(3),
    @Number char(6),
    @Group char(8)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) FROM dbo.LG_OrdComp_Grupos WITH (UPDLOCK,HOLDLOCK) WHERE Ser_OrdComp=@Series AND Cod_OrdComp=@Number AND Cod_Grupo=@Group ;
END;
GO

/*
 Consultar cantidad recibida con bloqueo
 Origen: backend/src/Intranet.Data/Repositories/SqlOrderWriteGateway.cs:66
 Nota: Conserva los bloqueos originales: usar la misma transacción del llamador.
*/
CREATE PROCEDURE [dbo].[PA_LG_ORDCOMPITEM_S0003]
    @Series char(3),
    @Number char(6),
    @Sequence char(3)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Can_Recibida FROM dbo.LG_ORDCOMPITEM WITH(UPDLOCK,HOLDLOCK) WHERE Ser_OrdComp=@Series AND Cod_OrdComp=@Number AND Sec_OrdComp=@Sequence ;
END;
GO

/*
 Leer detalle completo con bloqueo
 Origen: backend/src/Intranet.Data/Repositories/SqlOrderWriteGateway.cs:81
 Nota: Conserva los bloqueos originales: usar la misma transacción del llamador.
*/
CREATE PROCEDURE [dbo].[PA_LG_ORDCOMPITEM_S0004]
    @Series char(3),
    @Number char(6),
    @Sequence char(3)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM dbo.LG_ORDCOMPITEM WITH(UPDLOCK,HOLDLOCK) WHERE Ser_OrdComp=@Series AND Cod_OrdComp=@Number AND Sec_OrdComp=@Sequence ;
END;
GO

/*
 Contar requerimientos vinculados con bloqueo
 Origen: backend/src/Intranet.Data/Repositories/SqlOrderWriteGateway.cs:85
 Nota: Conserva los bloqueos originales: usar la misma transacción del llamador.
*/
CREATE PROCEDURE [dbo].[PA_LG_ORDCOMPITEMREQ_S0003]
    @Series char(3),
    @Number char(6),
    @Sequence char(3)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) FROM dbo.LG_ORDCOMPITEMREQ WITH(UPDLOCK,HOLDLOCK) WHERE Ser_OrdComp=@Series AND Cod_OrdComp=@Number AND Sec_OrdComp=@Sequence ;
END;
GO

/*
 Leer cabecera completa para preservar campos
 Origen: backend/src/Intranet.Data/Repositories/SqlOrderWriteGateway.cs:128
*/
CREATE PROCEDURE [dbo].[PA_LG_ORDCOMP_S0008]
    @Series char(3),
    @Number char(6)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM dbo.LG_ORDCOMP WHERE Ser_OrdComp=@Series AND Cod_OrdComp=@Number ;
END;
GO

/*
 Bloquear requerimientos por OP e ítem
 Origen: backend/src/Intranet.Data/Repositories/SqlOrderWriteGateway.cs:148
 Nota: Conserva los bloqueos originales: usar la misma transacción del llamador.
*/
CREATE PROCEDURE [dbo].[PA_ES_ORDPROREQ_ITEMS_S0001]
    @Op char(5),
    @Item char(8)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Cod_OrdPro FROM dbo.ES_ORDPROREQ_ITEMS WITH (UPDLOCK,HOLDLOCK) WHERE Cod_OrdPro=@Op AND Cod_Item=@Item ;
END;
GO

/*
 Leer secuencias, requeridos y precio con bloqueo
 Origen: backend/src/Intranet.Data/Repositories/SqlOrderWriteGateway.cs:153
 Nota: Conserva los bloqueos originales: usar la misma transacción del llamador.
*/
CREATE PROCEDURE [dbo].[PA_LG_ORDCOMPITEM_S0005]
    @Series char(3),
    @Number char(6)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Sec_OrdComp,Can_Requerida,Pre_Unitario FROM dbo.LG_ORDCOMPITEM WITH (UPDLOCK,HOLDLOCK) WHERE Ser_OrdComp=@Series AND Cod_OrdComp=@Number ;
END;
GO



