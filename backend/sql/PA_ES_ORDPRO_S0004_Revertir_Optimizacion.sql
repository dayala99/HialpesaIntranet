/* Solo lectura. SQL Server 2008 R2. Misma lógica de precio/tiempo que S0003.
   Filtros opcionales combinados con AND. Paginación por NP descendente: 50 filas + una para detectar siguiente página.
   No altera procedimientos existentes ni datos. Ejecutar este script antes de publicar el backend. */
IF OBJECT_ID('dbo.PA_ES_ORDPRO_S0004','P') IS NULL EXEC('CREATE PROCEDURE dbo.PA_ES_ORDPRO_S0004 AS RETURN 0');
GO
ALTER PROCEDURE dbo.PA_ES_ORDPRO_S0004
 @Imp_Precio_Comercial decimal(18,4)=NULL,
 @Min_Tiempo_Total decimal(18,2)=NULL,
 @Cod_Tipo varchar(20)=NULL,
 @Cod_Np_Anterior char(5)=NULL,
 @Cod_Fabrica char(3)='001'
AS
BEGIN
 SET NOCOUNT ON; SET ARITHABORT ON;
 SET @Cod_Tipo=NULLIF(LTRIM(RTRIM(@Cod_Tipo)),'');
 IF @Imp_Precio_Comercial IS NULL AND @Min_Tiempo_Total IS NULL AND @Cod_Tipo IS NULL
 BEGIN RAISERROR('Indica al menos un filtro de busqueda.',16,1); RETURN; END;
 IF @Imp_Precio_Comercial<0 OR @Min_Tiempo_Total<0
 BEGIN RAISERROR('Los valores no pueden ser negativos.',16,1); RETURN; END;
 ;WITH Datos AS (
    SELECT NULLIF(RTRIM(e.icono1),'') AS ImagePath,
 RTRIM(garment.Des_TipPre) AS GarmentType,RTRIM(op.Cod_EstCli) AS ClientStyle,op.Num_PrenReq AS RequiredQuantity,RTRIM(division.Nom_DivCli) AS DivisionName,
 STUFF((SELECT DISTINCT N' / '+RTRIM(p.Des_Present)
        FROM dbo.ES_OrdProColTal colors
        INNER JOIN dbo.ES_EstProPre p ON p.Cod_EstPro=op.Cod_EstPro AND p.Cod_Present=colors.Cod_Present
        WHERE colors.Cod_Fabrica=op.Cod_Fabrica AND colors.Cod_OrdPro=op.Cod_OrdPro
          AND NULLIF(RTRIM(p.Des_Present),'') IS NOT NULL
        ORDER BY N' / '+RTRIM(p.Des_Present)
        FOR XML PATH(''),TYPE).value('.','nvarchar(max)'),1,3,N'') AS ColorName,
 (SELECT MIN(work.Fec_Salida_Inicio) FROM dbo.PL_TRABAJOS work WHERE work.Cod_OrdPro=op.Cod_OrdPro) AS SewingStartDate,
 (SELECT MAX(work.Fec_Salida_Fin) FROM dbo.PL_TRABAJOS work WHERE work.Cod_OrdPro=op.Cod_OrdPro) AS SewingEndDate,
 RTRIM(op.cod_ordpro) AS Np,
           ISNULL(op.num_prenpro,0) AS ProgrammedQuantity,
           CAST(COALESCE(NULLIF(assigned.Price,0),quoted.Price) AS decimal(18,4)) AS CommercialPrice,
           CAST(dbo.CF_TIEMPO_TOTAL_COSTURA_OP(op.cod_fabrica,op.cod_ordpro) AS decimal(18,2)) AS SewingTime,
           RTRIM(customer.Nom_Cliente) AS ClientName,
           op.Fec_DespachoAct AS DispatchDate,
           NULLIF(RTRIM(ver.Des_Tela_Cliente),'') AS FabricDescription,
           STUFF((SELECT DISTINCT N' / '+RTRIM(line.Descripcion)
                  FROM dbo.PL_TRABAJOS work
                  INNER JOIN dbo.CF_Lineas_Produccion line ON line.Cod_LinPro=work.Cod_LinPro
                  WHERE work.Cod_OrdPro=op.cod_ordpro
                  FOR XML PATH(''),TYPE).value('.','nvarchar(max)'),1,3,N'') AS LineName,
           STUFF((SELECT DISTINCT N' / '+RTRIM(category.Des_DivPre)
                  FROM dbo.TG_LOTESTPRO lot
                  INNER JOIN dbo.TG_LOTCOLTAL detail ON detail.Cod_Cliente=lot.Cod_Cliente
                     AND detail.Cod_Purord=lot.Cod_Purord AND detail.Cod_EstCli=lot.Cod_EstCli
                     AND detail.Cod_LotPurord=lot.Cod_LotPurord
                  INNER JOIN dbo.TG_DIVPRE category ON category.Cod_DivPre=detail.Cod_DivPre
                  WHERE lot.Cod_Fabrica=op.cod_fabrica AND lot.Cod_OrdPro=op.cod_ordpro
                  FOR XML PATH(''),TYPE).value('.','nvarchar(max)'),1,3,N'') AS ProductCategory
    FROM dbo.ES_OrdPro op
    LEFT JOIN dbo.ES_EstPro e ON e.Cod_EstPro=op.Cod_EstPro
    LEFT JOIN dbo.TG_TipPre garment ON garment.Cod_TipPre=e.Cod_TipPre
    LEFT JOIN dbo.TG_PurOrd po ON po.Cod_Cliente=op.Cod_Cliente AND po.Cod_PurOrd=op.Cod_PurOrd AND po.Cod_ClaPurOrd=op.Cod_ClaPurOrd
    LEFT JOIN dbo.TG_DivCli division ON division.Cod_Cliente=po.Cod_Cliente AND division.Cod_DivCli=po.Cod_DivCli
    LEFT JOIN dbo.TG_CLIENTE customer ON customer.Cod_Cliente=op.cod_cliente
    LEFT JOIN dbo.ES_ESTPROVER ver ON ver.Cod_EstPro=op.cod_estpro AND ver.Cod_Version=op.cod_version
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
    WHERE op.cod_fabrica=@Cod_Fabrica AND (@Cod_Tipo IS NULL OR garment.Cod_TipPre=@Cod_Tipo) AND (@Cod_Np_Anterior IS NULL OR op.Cod_OrdPro<@Cod_Np_Anterior)
 )
 SELECT TOP (51) * FROM Datos
 WHERE (@Imp_Precio_Comercial IS NULL OR CommercialPrice=@Imp_Precio_Comercial)
 AND (@Min_Tiempo_Total IS NULL OR SewingTime=@Min_Tiempo_Total)
 ORDER BY Np DESC OPTION (RECOMPILE);
END;
GO
