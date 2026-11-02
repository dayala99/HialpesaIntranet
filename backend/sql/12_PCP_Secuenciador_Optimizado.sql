/* Secuenciador PCP: consulta propia de solo lectura, SQL Server 2008 R2 / compatibilidad 80.
 Conserva la inclusión de Programa General / Resumido / Todos / Sin OC=N.
 No ejecuta el reporte original ni modifica objetos/datos de Gestión.
 No calcula stocks, avances, cuotas, tiempos, imágenes ni subtotales del reporte impreso. */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID('dbo.PA_LG_PCP_SECUENCIADOR_S0001','P') IS NULL EXEC('CREATE PROCEDURE dbo.PA_LG_PCP_SECUENCIADOR_S0001 AS RETURN 0');
GO
ALTER PROCEDURE dbo.PA_LG_PCP_SECUENCIADOR_S0001 @Cod_Fabrica char(3)='001'
AS
BEGIN
 SET NOCOUNT ON;SET ARITHABORT ON;
 -- Los mismos INNER JOIN del detalle del reporte; el tipo R no filtra OPCION=2.
 SELECT DISTINCT work.Cod_OrdPro,work.Cod_TipOrdPro,work.Cod_TipLinPro,work.Cod_LinPro,work.Fase,line.Descripcion AS Des_Linea
 INTO #Trabajos
 FROM dbo.PL_TRABAJOS work
 INNER JOIN dbo.PL_ORDENES_A_PROGRAMAR programmed ON programmed.Cod_TipOrdPro=work.Cod_TipOrdPro AND programmed.Cod_OrdPro=work.Cod_OrdPro
 INNER JOIN dbo.TG_CLIENTE customer ON customer.Cod_Cliente=programmed.Cod_Cliente
 INNER JOIN dbo.ES_ORDPRO op ON op.Cod_Fabrica=@Cod_Fabrica AND op.Cod_OrdPro=work.Cod_OrdPro
 INNER JOIN dbo.PL_LINEAS_PRODUCCION line ON line.Cod_TipLinPro=work.Cod_TipLinPro AND line.Cod_LinPro=work.Cod_LinPro
 WHERE programmed.Flg_Vigente='S' AND work.Secuencia<>0
 AND LEN(RTRIM(work.Cod_OrdPro))=5 AND RTRIM(work.Cod_OrdPro) NOT LIKE '%[^A-Za-z0-9]%';
 CREATE INDEX IX_Trabajos_Orden ON #Trabajos(Cod_OrdPro);
 SELECT DISTINCT Cod_OrdPro,CAST(NULL AS int) AS Num_Cierre INTO #Cierres FROM #Trabajos;
 -- Una evaluación por NP; el original la repite por cada trabajo.
 UPDATE #Cierres SET Num_Cierre=dbo.CO_REVISA_CIERRE_SECTOR_CORTE_HABILITADO_COSTURA(1,@Cod_Fabrica,Cod_OrdPro,0,'CS');
 DELETE work FROM #Trabajos work INNER JOIN #Cierres closed ON closed.Cod_OrdPro=work.Cod_OrdPro WHERE closed.Num_Cierre=1;
 CREATE TABLE #Nps(Cod_OrdPro char(5) NOT NULL PRIMARY KEY);
 INSERT #Nps SELECT DISTINCT Cod_OrdPro FROM #Trabajos;
SELECT RTRIM(dbo.CF_STATUS_TARIFADO(selected.Cod_OrdPro)) AS TariffStatus,
 RTRIM(dbo.CF_STATUS_FICHA_TECNICA(@Cod_Fabrica,selected.Cod_OrdPro)) AS TechnicalSheetStatus,
 CAST(COALESCE(NULLIF(assigned.Price,0),quoted.Price) AS decimal(18,4)) AS CommercialPrice,CAST(NULL AS decimal(18,2)) AS SewingTime,RTRIM(selected.Cod_OrdPro) AS Np,ISNULL(op.Num_PrenPro,0) AS ProgrammedQuantity,RTRIM(customer.Nom_Cliente) AS ClientName,op.Fec_DespachoAct AS DispatchDate,NULLIF(RTRIM(ver.Des_Tela_Cliente),'') AS FabricDescription,
 STUFF((SELECT DISTINCT N' / '+RTRIM(work.Des_Linea) FROM #Trabajos work WHERE work.Cod_OrdPro=selected.Cod_OrdPro FOR XML PATH(''),TYPE).value('.','nvarchar(max)'),1,3,N'') AS LineName,
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

