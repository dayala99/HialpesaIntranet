/* Actualización: precio y tiempo para filtros locales de Pendientes.
 Pendientes de Analíticas. SQL Server 2008 R2. Ejecutar en HIALPESA.
 El backend ejecuta PMP_POSICION_COSTURA_LINEA_OP con OPCION=2,TODOS=T,R y SinOC=N,
 descarta subtotales y envía las NP como XML. Este procedimiento completa las cabeceras en lote.
 No modifica tablas ni datos de negocio. No ejecuta la opción 6 de Planeamiento. */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID('dbo.PA_LG_ANALITICAS_PENDIENTES_S0001','P') IS NULL EXEC('CREATE PROCEDURE dbo.PA_LG_ANALITICAS_PENDIENTES_S0001 AS RETURN 0');
GO
ALTER PROCEDURE dbo.PA_LG_ANALITICAS_PENDIENTES_S0001
 @Cod_Fabrica char(3)='001',@Txt_Ordenes xml=NULL
AS
BEGIN
 SET NOCOUNT ON;SET ARITHABORT ON;
 CREATE TABLE #Nps(Cod_OrdPro char(5) NOT NULL PRIMARY KEY);
 INSERT #Nps SELECT DISTINCT Np.value('(@codigo)[1]','char(5)') FROM @Txt_Ordenes.nodes('/nps/np') AS N(Np);
SELECT CAST(COALESCE(NULLIF(assigned.Price,0),quoted.Price) AS decimal(18,4)) AS CommercialPrice,CAST(dbo.CF_TIEMPO_TOTAL_COSTURA_OP(op.Cod_Fabrica,op.Cod_OrdPro) AS decimal(18,2)) AS SewingTime,RTRIM(selected.Cod_OrdPro) AS Np,ISNULL(op.Num_PrenPro,0) AS ProgrammedQuantity,RTRIM(customer.Nom_Cliente) AS ClientName,op.Fec_DespachoAct AS DispatchDate,NULLIF(RTRIM(ver.Des_Tela_Cliente),'') AS FabricDescription,
 STUFF((SELECT DISTINCT N' / '+RTRIM(line.Descripcion) FROM dbo.PL_TRABAJOS work INNER JOIN dbo.CF_Lineas_Produccion line ON line.Cod_LinPro=work.Cod_LinPro WHERE work.Cod_OrdPro=op.Cod_OrdPro FOR XML PATH(''),TYPE).value('.','nvarchar(max)'),1,3,N'') AS LineName,
 NULLIF(RTRIM(e.icono1),'') AS ImagePath,
 RTRIM(garment.Des_TipPre) AS GarmentType,RTRIM(op.Cod_EstCli) AS ClientStyle,op.Num_PrenReq AS RequiredQuantity,RTRIM(division.Nom_DivCli) AS DivisionName,
 STUFF((SELECT DISTINCT N' / '+RTRIM(p.Des_Present)
        FROM dbo.ES_OrdProColTal colors
        INNER JOIN dbo.ES_EstProPre p ON p.Cod_EstPro=op.Cod_EstPro AND p.Cod_Present=colors.Cod_Present
        WHERE colors.Cod_Fabrica=op.Cod_Fabrica AND colors.Cod_OrdPro=op.Cod_OrdPro
          AND NULLIF(RTRIM(p.Des_Present),'') IS NOT NULL
        ORDER BY N' / '+RTRIM(p.Des_Present)
        FOR XML PATH(''),TYPE).value('.','nvarchar(max)'),1,3,N'') AS ColorName,
 (SELECT MIN(work.Fec_Salida_Inicio) FROM dbo.PL_TRABAJOS work WHERE work.Cod_OrdPro=op.Cod_OrdPro) AS SewingStartDate,
 (SELECT MAX(work.Fec_Salida_Fin) FROM dbo.PL_TRABAJOS work WHERE work.Cod_OrdPro=op.Cod_OrdPro) AS SewingEndDate
FROM #Nps selected LEFT JOIN dbo.ES_OrdPro op ON selected.Cod_OrdPro=op.Cod_OrdPro AND op.Cod_Fabrica=@Cod_Fabrica
LEFT JOIN dbo.ES_EstPro e ON e.Cod_EstPro=op.Cod_EstPro
LEFT JOIN dbo.TG_TipPre garment ON garment.Cod_TipPre=e.Cod_TipPre
LEFT JOIN dbo.TG_PurOrd po ON po.Cod_Cliente=op.Cod_Cliente AND po.Cod_PurOrd=op.Cod_PurOrd AND po.Cod_ClaPurOrd=op.Cod_ClaPurOrd
LEFT JOIN dbo.TG_DivCli division ON division.Cod_Cliente=po.Cod_Cliente AND division.Cod_DivCli=po.Cod_DivCli
LEFT JOIN dbo.TG_CLIENTE customer ON customer.Cod_Cliente=op.Cod_Cliente
LEFT JOIN dbo.ES_EstProVer ver ON ver.Cod_EstPro=op.Cod_EstPro AND ver.Cod_Version=op.Cod_Version
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

ORDER BY selected.Cod_OrdPro DESC;
END;
GO

