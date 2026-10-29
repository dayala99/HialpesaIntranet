SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
/* Solo lectura. SQL Server 2008 R2. Misma lógica de precio/tiempo que S0003.
   Filtros opcionales combinados con AND. Paginación por NP descendente: 50 filas + una para detectar siguiente página.
   Actualiza S0004 y conserva S0005. No cambia tablas ni datos de negocio. */
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

 -- 1. Aplicar tipo y cursor antes de cotizaciones y minutos.
 SELECT op.Cod_OrdPro,op.Cod_EstPro,op.Cod_Version,op.Cod_Variante_Tarifado,op.Cod_ClaPurOrd
 INTO #Candidatas FROM dbo.ES_OrdPro op
 LEFT JOIN dbo.ES_EstPro e ON e.Cod_EstPro=op.Cod_EstPro
 WHERE op.Cod_Fabrica=@Cod_Fabrica AND (@Cod_Tipo IS NULL OR e.Cod_TipPre=@Cod_Tipo)
 AND (@Cod_Np_Anterior IS NULL OR op.Cod_OrdPro<@Cod_Np_Anterior) OPTION(RECOMPILE);
 CREATE UNIQUE CLUSTERED INDEX IX_Candidatas ON #Candidatas(Cod_OrdPro);
 -- Si solo se filtra tipo, no hacen falta cotizaciones ni tiempos de NP fuera de la página.
 IF @Imp_Precio_Comercial IS NULL AND @Min_Tiempo_Total IS NULL
 DELETE FROM #Candidatas WHERE Cod_OrdPro NOT IN (SELECT TOP(51) Cod_OrdPro FROM #Candidatas ORDER BY Cod_OrdPro DESC);
 -- Un MAX igual al precio exige al menos una cotización con ese valor.
 -- Este descarte previo no sustituye la validación posterior de MAX y prioridad asignada.
 IF @Imp_Precio_Comercial IS NOT NULL
 BEGIN
 SELECT DISTINCT Cod_OrdPro INTO #Posibles FROM (
        SELECT a.Cod_OrdPro
        FROM dbo.TG_LOTESTPRO a INNER JOIN #Candidatas n ON n.Cod_OrdPro=a.Cod_OrdPro
        INNER JOIN dbo.TG_COTIZACION_ESTILOSPROPIOS f
            ON f.Num_Solicitud_Cons=a.NUM_SOLICITUD_COSTEO_ASIGNADA
           AND f.Cod_EstPro=a.COD_ESTPRO_ASIGNADA
           AND f.Cod_Version=a.COD_VERSION_ASIGNADA
        WHERE a.Cod_Fabrica=@Cod_Fabrica
          AND f.Flg_Status='C'
 AND CAST(f.MObra_Confec AS decimal(18,4))=@Imp_Precio_Comercial
 UNION
        SELECT a.Cod_OrdPro
        FROM dbo.TG_LOTESTPRO a INNER JOIN #Candidatas n ON n.Cod_OrdPro=a.Cod_OrdPro
        INNER JOIN dbo.TG_PURORD c ON a.Cod_Cliente=c.Cod_Cliente AND a.Cod_Purord=c.Cod_Purord
        INNER JOIN dbo.TG_ESTCLIEST d ON a.Cod_Cliente=d.Cod_Cliente AND c.Cod_TemCli=d.Cod_TemCli
            AND a.Cod_EstCli=d.Cod_EstCli AND a.Cod_EstPro=d.Cod_EstPro
        INNER JOIN dbo.TG_ESTCLITEM e ON a.Cod_Cliente=e.Cod_Cliente AND c.Cod_TemCli=e.Cod_TemCli
            AND a.Cod_EstCli=e.Cod_EstCli
        INNER JOIN dbo.TG_COTIZACION_ESTILOSPROPIOS f ON e.Num_Solicitud_Cons=f.Num_Solicitud_Cons
            AND a.Cod_EstPro=f.Cod_EstPro AND d.Cod_Version_Costeo=f.Cod_Version
        WHERE a.Cod_Fabrica=@Cod_Fabrica AND f.Flg_Status='C'
 AND CAST(f.MObra_Confec AS decimal(18,4))=@Imp_Precio_Comercial
 ) posibles;
 CREATE UNIQUE CLUSTERED INDEX IX_Posibles ON #Posibles(Cod_OrdPro);
 DELETE n FROM #Candidatas n WHERE NOT EXISTS(SELECT 1 FROM #Posibles p WHERE p.Cod_OrdPro=n.Cod_OrdPro);
 END;

        SELECT a.Cod_OrdPro,MAX(f.MObra_Confec) AS Imp_Precio INTO #Asignados
        FROM dbo.TG_LOTESTPRO a INNER JOIN #Candidatas n ON n.Cod_OrdPro=a.Cod_OrdPro
        INNER JOIN dbo.TG_COTIZACION_ESTILOSPROPIOS f
            ON f.Num_Solicitud_Cons=a.NUM_SOLICITUD_COSTEO_ASIGNADA
           AND f.Cod_EstPro=a.COD_ESTPRO_ASIGNADA
           AND f.Cod_Version=a.COD_VERSION_ASIGNADA
        WHERE a.Cod_Fabrica=@Cod_Fabrica
          AND f.Flg_Status='C'
 GROUP BY a.Cod_OrdPro;
 CREATE UNIQUE CLUSTERED INDEX IX_Asignados ON #Asignados(Cod_OrdPro);
        SELECT a.Cod_OrdPro,MAX(f.MObra_Confec) AS Imp_Precio INTO #Cotizados
        FROM dbo.TG_LOTESTPRO a INNER JOIN #Candidatas n ON n.Cod_OrdPro=a.Cod_OrdPro
        LEFT JOIN #Asignados asignado ON asignado.Cod_OrdPro=n.Cod_OrdPro
        INNER JOIN dbo.TG_PURORD c ON a.Cod_Cliente=c.Cod_Cliente AND a.Cod_Purord=c.Cod_Purord
        INNER JOIN dbo.TG_ESTCLIEST d ON a.Cod_Cliente=d.Cod_Cliente AND c.Cod_TemCli=d.Cod_TemCli
            AND a.Cod_EstCli=d.Cod_EstCli AND a.Cod_EstPro=d.Cod_EstPro
        INNER JOIN dbo.TG_ESTCLITEM e ON a.Cod_Cliente=e.Cod_Cliente AND c.Cod_TemCli=e.Cod_TemCli
            AND a.Cod_EstCli=e.Cod_EstCli
        INNER JOIN dbo.TG_COTIZACION_ESTILOSPROPIOS f ON e.Num_Solicitud_Cons=f.Num_Solicitud_Cons
            AND a.Cod_EstPro=f.Cod_EstPro AND d.Cod_Version_Costeo=f.Cod_Version
        WHERE a.Cod_Fabrica=@Cod_Fabrica AND ISNULL(asignado.Imp_Precio,0)=0 AND f.Flg_Status='C'
 GROUP BY a.Cod_OrdPro;
 CREATE UNIQUE CLUSTERED INDEX IX_Cotizados ON #Cotizados(Cod_OrdPro);
 SELECT n.*,CAST(COALESCE(NULLIF(a.Imp_Precio,0),q.Imp_Precio) AS decimal(18,4)) AS Imp_Precio_Comercial
 INTO #Precios FROM #Candidatas n LEFT JOIN #Asignados a ON a.Cod_OrdPro=n.Cod_OrdPro
 LEFT JOIN #Cotizados q ON q.Cod_OrdPro=n.Cod_OrdPro;
 IF @Imp_Precio_Comercial IS NOT NULL DELETE FROM #Precios WHERE Imp_Precio_Comercial IS NULL OR Imp_Precio_Comercial<>@Imp_Precio_Comercial;
 CREATE UNIQUE CLUSTERED INDEX IX_Precios ON #Precios(Cod_OrdPro);
 -- 2. Respetar NP de reserva y producción; los vínculos ambiguos mantienen la función original.
 SELECT n.Cod_OrdPro,n.Imp_Precio_Comercial,ISNULL(rel.Num_Relaciones,0) AS Num_Relaciones,
 origen.Cod_EstPro,origen.Cod_Version,origen.Cod_Variante_Tarifado,origen.Cod_ClaPurOrd
 INTO #Fuentes FROM #Precios n
 LEFT JOIN (SELECT r.Cod_OrdPro_Destino,COUNT(*) AS Num_Relaciones,MAX(r.Cod_OrdPro_Origen) AS Cod_OrdPro_Origen
 FROM dbo.ES_ORDPRO_REL_NPS_RESERVA_NPS_PRODUCCION r INNER JOIN #Precios p ON p.Cod_OrdPro=r.Cod_OrdPro_Destino GROUP BY r.Cod_OrdPro_Destino) rel ON rel.Cod_OrdPro_Destino=n.Cod_OrdPro
 LEFT JOIN dbo.ES_OrdPro origen ON origen.Cod_Fabrica=@Cod_Fabrica AND origen.Cod_OrdPro=CASE WHEN rel.Num_Relaciones IS NOT NULL THEN rel.Cod_OrdPro_Origen ELSE n.Cod_OrdPro END;
 -- 3. Sumar operaciones una vez por tarifado/variante, con los mismos joins y redondeos de la función.
 SELECT DISTINCT Cod_EstPro,Cod_Variante_Tarifado INTO #Tarifados FROM #Fuentes WHERE Num_Relaciones<=1 AND ISNULL(Cod_ClaPurOrd,'')<>'PR';
 SELECT k.Cod_EstPro,k.Cod_Variante_Tarifado,CAST(ISNULL(SUM(vc.Tie_Estandar_Ope),0) AS numeric(9,3)) AS Min_Costura
 INTO #Tiempos FROM #Tarifados k
 INNER JOIN dbo.CF_TARIFADO_BLOQUES tb ON tb.Cod_Tarifado=k.Cod_EstPro AND tb.Cod_Variante_Tarifado=k.Cod_Variante_Tarifado
 INNER JOIN dbo.CF_TARIFADO_OPERACIONES ot ON ot.Cod_Tarifado=tb.Cod_Tarifado AND ot.Cod_Variante_Tarifado=tb.Cod_Variante_Tarifado AND ot.Cod_BlqPre=tb.Cod_BlqPre
 INNER JOIN dbo.CF_OPERACION_VARIANTE_COSTURA vc ON vc.Cod_Operacion_Costura=ot.Cod_Operacion_Costura AND vc.Cod_Variante_Operacion=ot.Cod_Variante_Operacion
 INNER JOIN dbo.CF_BLQPRE bl ON bl.Cod_BlqPre=ot.Cod_BlqPre AND bl.Cod_SecConf='CS'
 GROUP BY k.Cod_EstPro,k.Cod_Variante_Tarifado;
 SELECT f.Cod_OrdPro,f.Imp_Precio_Comercial,
 CAST(CASE WHEN f.Num_Relaciones>1 THEN dbo.CF_TIEMPO_TOTAL_COSTURA_OP(@Cod_Fabrica,f.Cod_OrdPro)
 WHEN f.Cod_ClaPurOrd='PR' THEN ISNULL(CAST(v.Min_CosCosteo AS numeric(9,3)),0)
 ELSE ISNULL(t.Min_Costura,0) END AS decimal(18,2)) AS Min_Tiempo_Total
 INTO #Calculadas FROM #Fuentes f LEFT JOIN #Tiempos t ON t.Cod_EstPro=f.Cod_EstPro AND t.Cod_Variante_Tarifado=f.Cod_Variante_Tarifado
 LEFT JOIN dbo.ES_EstProVer v ON v.Cod_EstPro=f.Cod_EstPro AND v.Cod_Version=f.Cod_Version;
 -- 4. Completar cabecera, color y fechas solamente para las 51 filas de la página.
 SELECT TOP(51) * INTO #Pagina FROM #Calculadas
 WHERE @Min_Tiempo_Total IS NULL OR Min_Tiempo_Total=@Min_Tiempo_Total
 ORDER BY Cod_OrdPro DESC OPTION(RECOMPILE);
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
           page.Imp_Precio_Comercial AS CommercialPrice,
           page.Min_Tiempo_Total AS SewingTime,
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
    FROM #Pagina page
    INNER JOIN dbo.ES_OrdPro op ON op.Cod_Fabrica=@Cod_Fabrica AND op.Cod_OrdPro=page.Cod_OrdPro
    LEFT JOIN dbo.ES_EstPro e ON e.Cod_EstPro=op.Cod_EstPro
    LEFT JOIN dbo.TG_TipPre garment ON garment.Cod_TipPre=e.Cod_TipPre
    LEFT JOIN dbo.TG_PurOrd po ON po.Cod_Cliente=op.Cod_Cliente AND po.Cod_PurOrd=op.Cod_PurOrd AND po.Cod_ClaPurOrd=op.Cod_ClaPurOrd
    LEFT JOIN dbo.TG_DivCli division ON division.Cod_Cliente=po.Cod_Cliente AND division.Cod_DivCli=po.Cod_DivCli
    LEFT JOIN dbo.TG_CLIENTE customer ON customer.Cod_Cliente=op.cod_cliente
    LEFT JOIN dbo.ES_ESTPROVER ver ON ver.Cod_EstPro=op.cod_estpro AND ver.Cod_Version=op.cod_version

 ORDER BY op.Cod_OrdPro DESC;

END;
GO
