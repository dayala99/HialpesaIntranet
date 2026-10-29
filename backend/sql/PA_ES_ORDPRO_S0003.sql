/* Análisis-Rb por NP. Solo lectura. Compatible con SQL Server 2008 R2.
   Cantidad: ES_OrdPro.num_prenpro.
   Precio: MObra_Confec de la cotización aprobada asignada; si no hay precio
   asignado, MAX de las cotizaciones aprobadas del estilo/temporada, como Gestión.
   Se conservan los 3 decimales de origen y se exponen 4 decimales.
   Tiempo: CF_TIEMPO_TOTAL_COSTURA_OP (solo costura).
   Verificado: J1825 -> precio 1.4040, tiempo 11.51.
*/
IF OBJECT_ID('dbo.PA_ES_ORDPRO_S0003','P') IS NULL
    EXEC('CREATE PROCEDURE dbo.PA_ES_ORDPRO_S0003 AS RETURN 0');
GO
ALTER PROCEDURE dbo.PA_ES_ORDPRO_S0003
    @Np char(5),
    @Factory char(3) = '001'
AS
BEGIN
    SET NOCOUNT ON;
    SET ARITHABORT ON;
    SELECT RTRIM(op.cod_ordpro) AS Np,
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
    WHERE op.cod_fabrica=@Factory AND op.cod_ordpro=@Np;
END;
GO
