-- Revisión: búsqueda por OT con el mismo alcance que Gestión. Pendiente aplicar ALTER.
-- Mantiene columnas diferentes por filtro y cuenta/pagina el resultado final.
-- Elimina solo nombres de columnas repetidos dentro de la misma proyeccion.
-- Opciones: 0 catalogos; 1 cliente/OC/color; 2 cliente/pendientes;
-- 3 cliente/creacion; 4 numero; 5 estado; 6 fabrica/NP; 7 fibra/primer envio.
-- Resultado 1: TotalRows. Resultado 2: grilla de la variante (opcion 0: solo catalogos).
-- Permisos por cliente: filtros 1, 2 y 3; catálogo según TI_SEG_PARTIDAS_USUARIO_CLIENTE.
-- Fechas finales inclusivas; no modifica tablas de negocio ni SP existentes.
USE [HIALPESA];


GO
CREATE PROCEDURE [dbo].[PA_TI_ORDTRA_TINTORERIA_ITEMS_S0001]
@OPCION CHAR (1),
    @COD_CLIENTE_TEX COD_CLIENTE,
    @SER_ORDCOMP SER_ORDCOMP,
    @COD_ORDCOMP COD_ORDCOMP,
    @COD_COLOR COD_COLOR,
    @COD_ORDTRA COD_ORDTRA,
    @FECHA_DESDE DATETIME,
    @FECHA_HASTA DATETIME,
    @FLG_STATUS CHAR (1),
    @COD_FABRICA COD_FABRICA,
    @COD_ORDPRO COD_ORDPRO,
    @FEC_INICIO DATETIME=NULL,
    @FEC_FIN DATETIME=NULL,
    @COD_FIBRA CHAR (1)='',
    @COD_USUARIO COD_USUARIO='',
    @PAGINA INT=1,
    @TAMANO_PAGINA INT=50
AS
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF @OPCION NOT IN ('0', '1', '2', '3', '4', '5', '6', '7')
    BEGIN
        RAISERROR ('Filtro inválido.', 16, 1);
        RETURN;
    END
IF @PAGINA < 1
   OR @PAGINA > 100000
   OR @TAMANO_PAGINA NOT BETWEEN 1 AND 100
    BEGIN
        RAISERROR ('Paginación inválida.', 16, 1);
        RETURN;
    END
IF LTRIM(RTRIM(@COD_USUARIO)) = ''
    BEGIN
        RAISERROR ('Usuario requerido.', 16, 1);
        RETURN;
    END
SELECT DISTINCT C.COD_CLIENTE_TEX
INTO   #CLIENTES_PERMITIDOS
FROM   TX_CLIENTE AS C
WHERE  EXISTS (SELECT 1
               FROM   TI_SEG_PARTIDAS_USUARIO_CLIENTE AS S
               WHERE  S.COD_USUARIO = @COD_USUARIO
                      AND (S.COD_CLIENTE = '*****'
                           OR S.COD_CLIENTE = C.COD_CLIENTE_TEX));
IF @OPCION = '0'
    BEGIN
        SELECT   'clients' AS Kind,
                 RTRIM(C.COD_CLIENTE_TEX) AS Code,
                 RTRIM(C.NOM_CLIENTE) AS Description
        FROM     TX_CLIENTE AS C
                 INNER JOIN
                 #CLIENTES_PERMITIDOS AS P
                 ON P.COD_CLIENTE_TEX = C.COD_CLIENTE_TEX
        UNION ALL
        SELECT   'factories',
                 RTRIM(COD_FABRICA),
                 RTRIM(NOM_FABRICA)
        FROM     TG_FABRICA
        UNION ALL
        SELECT   'statuses',
                 RTRIM(FLG_STATUS),
                 RTRIM(DESCRIPCION)
        FROM     TI_STATUS_ORDENES_TINTORERIA
        UNION ALL
        SELECT   'fibers',
                 RTRIM(COD_FIBRA),
                 RTRIM(DESCRIPCION)
        FROM     HI_FIBRAS
        ORDER BY Kind, Description;
        RETURN;
    END
IF @OPCION IN ('1', '2', '3')
   AND NOT EXISTS (SELECT 1
                   FROM   #CLIENTES_PERMITIDOS
                   WHERE  COD_CLIENTE_TEX = @COD_CLIENTE_TEX)
    BEGIN
        RAISERROR ('Cliente no permitido.', 16, 1);
        RETURN;
    END
IF (@OPCION = '1'
    AND (LTRIM(RTRIM(@SER_ORDCOMP)) = ''
         OR LTRIM(RTRIM(@COD_ORDCOMP)) = ''))
   OR (@OPCION = '4'
       AND LTRIM(RTRIM(@COD_ORDTRA)) = '')
   OR (@OPCION = '5'
       AND NOT EXISTS (SELECT 1
                       FROM   TI_STATUS_ORDENES_TINTORERIA
                       WHERE  FLG_STATUS = @FLG_STATUS))
   OR (@OPCION = '6'
       AND (LTRIM(RTRIM(@COD_FABRICA)) = ''
            OR LTRIM(RTRIM(@COD_ORDPRO)) = ''))
   OR (@OPCION = '7'
       AND NOT EXISTS (SELECT 1
                       FROM   HI_FIBRAS
                       WHERE  COD_FIBRA = @COD_FIBRA))
    BEGIN
        RAISERROR ('Completa los campos del filtro.', 16, 1);
        RETURN;
    END
IF (@OPCION = '3'
    AND (@FECHA_DESDE IS NULL
         OR @FECHA_HASTA IS NULL
         OR @FECHA_DESDE > @FECHA_HASTA
         OR @FECHA_HASTA >= '21000101'))
   OR (@OPCION = '7'
       AND (@FEC_INICIO IS NULL
            OR @FEC_FIN IS NULL
            OR @FEC_INICIO > @FEC_FIN
            OR @FEC_FIN >= '21000101'))
    BEGIN
        RAISERROR ('Rango de fechas inválido.', 16, 1);
        RETURN;
    END
SELECT TOP 0 *
INTO   #ITEMS
FROM   TI_ORDTRA_TINTORERIA_ITEMS;
IF @OPCION = '1'
    BEGIN
        INSERT #ITEMS
        SELECT *
        FROM   TI_ORDTRA_TINTORERIA_ITEMS
        WHERE  COD_CLIENTE_TEX = @COD_CLIENTE_TEX
               AND SER_ORDCOMP = @SER_ORDCOMP
               AND COD_ORDCOMP = @COD_ORDCOMP
               AND (COD_COLOR = @COD_COLOR
                    OR @COD_COLOR = '');
    END
IF @OPCION = '2'
    BEGIN
        INSERT #ITEMS
        SELECT *
        FROM   TI_ORDTRA_TINTORERIA_ITEMS
        WHERE  FLG_STATUS IN ('P', 'C', 'E')
               AND COD_CLIENTE_TEX = @COD_CLIENTE_TEX;
    END
IF @OPCION = '3'
    BEGIN
        INSERT #ITEMS
        SELECT A.*
        FROM   TI_ORDTRA_TINTORERIA_ITEMS AS A, TI_ORDTRA_TINTORERIA AS B
        WHERE  A.COD_ORDTRA = B.COD_ORDTRA
               AND A.COD_CLIENTE_TEX = @COD_CLIENTE_TEX
               AND B.FEC_CREACION >= CONVERT (DATE, @FECHA_DESDE)
               AND B.FEC_CREACION < DATEADD(DAY, 1, CONVERT (DATE, @FECHA_HASTA));
    END
IF @OPCION = '4'
   OR @OPCION = '9'
    BEGIN
        INSERT #ITEMS
        SELECT *
        FROM   TI_ORDTRA_TINTORERIA_ITEMS
        WHERE  COD_ORDTRA = @COD_ORDTRA;
    END
IF @OPCION = '5'
    BEGIN
        INSERT #ITEMS
        SELECT *
        FROM   TI_ORDTRA_TINTORERIA_ITEMS
        WHERE  FLG_STATUS = @FLG_STATUS;
    END
IF @OPCION = '6'
    BEGIN
        INSERT #ITEMS
        SELECT A.*
        FROM   TI_ORDTRA_TINTORERIA_ITEMS AS A, TI_ORDTRA_TINTORERIA AS B
        WHERE  A.COD_ORDTRA = B.COD_ORDTRA
               AND B.COD_FABRICA = @COD_FABRICA
               AND B.COD_ORDPRO = @COD_ORDPRO;
    END
IF @OPCION = '5'
    BEGIN
        IF @FLG_STATUS = 'E'
            BEGIN
                SELECT *
                INTO   #XX
                FROM   TI_ORDTRA_TINTORERIA
                WHERE  FLG_TEMPORAL_EN_REVISION = 'S';
                DELETE #ITEMS
                WHERE  COD_ORDTRA IN (SELECT COD_ORDTRA
                                      FROM   #XX);
            END
    END
IF @OPCION = '7'
    BEGIN
        SELECT *
        INTO   #HILADOS
        FROM   HI_HILADOS
        WHERE  COD_FIBRA = @COD_FIBRA;
        SELECT   C.COD_TELA
        INTO     #TELAS
        FROM     #HILADOS AS A, TX_ORDTRA_HILOSTEL AS B, TX_ORDTRA_TEJEDURIA AS C
        WHERE    A.COD_HILADO = B.COD_HILTEL_TEJEDURIA
                 AND B.COD_ORDTRA = C.COD_ORDTRA
        GROUP BY C.COD_TELA;
        INSERT #ITEMS
        SELECT A.*
        FROM   TI_ORDTRA_TINTORERIA_ITEMS AS A, #TELAS AS B
        WHERE  FEC_1ER_ENVIO >= CONVERT (DATE, @FEC_INICIO)
               AND FEC_1ER_ENVIO < DATEADD(DAY, 1, CONVERT (DATE, @FEC_FIN))
               AND A.COD_TELA = B.COD_TELA;
    END
-- Como Gestión: los permisos por cliente se validan en filtros 1, 2 y 3.
-- Los filtros por número OT, estado, N/P y fibra conservan el alcance original.
DECLARE @FECHA_DESPACHO AS DATETIME;
IF @OPCION <> '4'
   AND @OPCION <> '9'
    BEGIN
        SELECT A.COD_ORDTRA,
               DBO.TI_MUESTRA_OP_NP_PARTIDA(A.COD_ORDTRA) AS NP,
               A.NUM_SECUENCIA,
               A.COD_TELA AS TELA,
               CASE WHEN SUBSTRING(A.COD_TELA, 1, 2) = 'DE'
                         AND A.COD_COMB <> '' THEN RTRIM(ISNULL(DES_COMB, '')) ELSE RTRIM(DES_TELA) END AS NOMBRE,
               A.COD_COLOR,
               A.COD_COLOR + '-' + SUBSTRING(DES_COLOR, 1, 40) AS COLOR,
               DBO.TI_MUESTRA_PARTIDAS_RELACIONADAS(A.COD_ORDTRA) AS Relacionadas,
               DBO.TI_MUESTRA_PARTIDAS_MADRE(A.COD_ORDTRA) AS Madre,
               A.COD_COMB,
               A.COD_COMB AS COMBO,
               ISNULL(DES_COMB, '') AS COMB,
               A.COD_TALLA,
               CONVERT (BIT, 0) AS Sel,
               DBO.TI_CALCULA_HORAS_POR_PARTIDA_PROCESO_ITEM(A.COD_ORDTRA, '01', A.NUM_SECUENCIA) AS HRS_TENIDO,
               DBO.[TI_OBTIENE_FECHA_HORA_PRIMER_TENIDO_SIN_TURNO](A.COD_ORDTRA) AS FEC_1ER_TENIDO,
               DBO.TI_OBTIENE_FECHA_HORA_FIN_PRIMER_TENIDO_SIN_TURNO(A.COD_ORDTRA) AS FEC_FIN_1ER_TENIDO,
               FEC_ULT_TENIDO,
               B.FEC_CREACION,
               A.KGS_PROGR,
               A.KGS_ASIGNADOS,
               A.KGS_CRUDO,
               KGS_TENIDOS_1RAS,
               KGS_DESPACHADOS,
               UNI_PROGR,
               UNI_ASIGNADOS,
               UNI_CRUDO,
               UNI_TENIDAS_1RAS,
               UNI_DESPACHADOS,
               A.FEC_ULT_RECEPCION AS FECHA_ULTIMO_INGRESO_ALMACEN_TELA,
               ROLLOS_CRUDO AS ROLLOS_A_TENIR,
               ROLLOS_TENIDOS AS ROLLOS_TENIDOS,
               ROLLOS_DESPACHADOS AS ROLLOS_DESPACHADOS,
               A.FLG_STATUS + '-' + RTRIM(F.DESCRIPCION) AS STATUS,
               A.COD_CLIENTE_TEX,
               RTRIM(H.NOM_CLIENTE) AS CLIENTE,
               A.SER_ORDCOMP,
               A.COD_ORDCOMP,
               A.SEC_ORDCOMP,
               A.SER_ORDCOMP + '-' + A.COD_ORDCOMP AS OCOMPRA,
               B.COD_OPCION + '-' + RTRIM(G.DESCRIPCION) AS RECETA,
               B.COD_ORDPROV_CLIENTE AS PARTIDA_CLIENTE,
               FLG_REPROCESO AS REPROCESO,
               B.COD_MAQUINA_TINTO,
               A.FLG_STATUS,
               @FECHA_DESPACHO AS FECHA_DESPACHO,
               A.GRAMAJE_ACAB,
               ISNULL(K.FACTOR, 1) * A.ANCHO_ACAB AS ANCHO_ACAB,
               A.ENCOG_ANCHO,
               A.ENCOG_ANCHO_3RALAV,
               A.ENCOG_LARGO,
               A.ENCOG_LARGO_3RALAV,
               A.FEC_ULTMODDATTEC,
               A.COD_USUARIOULTMODDATTEC,
               A.COD_TIPENC,
               A.REVIRADO,
               A.REVIRADO_3RALAV,
               A.GRAMAJE_LAVADO,
               A.FLG_STATUS_APRO,
               A.OBSERVACIONES,
               A.ELONGACIONLARGO,
               A.COD_MOTRECHAZO,
               ISNULL(I.DES_MOTRECHAZO, '') AS DES_MOTRECHAZO,
               A.ELONGACIONANCHO,
               A.KGS_RECHAZADOS,
               ISNULL(J.COD_ORDTRA, '') AS PARTIDA_PRINCIPAL,
               TIP_ANCHO_INFORMADO,
               ANCHO_ACAB_INFORMADO,
               DBO.TI_ENCUENTRA_CLASE_OC_PARTIDA(A.COD_ORDTRA) AS CLASE_OC,
               DBO.TH_OBTIENES_KGS_CRUDO_PARTIDA_SECUENCIA(A.COD_ORDTRA, A.NUM_SECUENCIA) AS GUIAS,
               A.KGS_EN_PANOS,
               A.KGS_LAVADOS,
               A.NUM_PANOS_CORTADOS,
               A.NUM_PANOS_LAVADOS,
               COD_MAQUINA_TINTO AS MAQUINA,
               B.FLG_HABILITADA_PARA_DESPACHO AS FLAGHABILITADAPARADESPACHO,
               A.FLG_VALIDACION_ENCOGIMIENTO_CORTE AS VALIDADO_ENCOGIMIENTO_CORTE,
               TIPO_TIZADO,
               (SELECT DBO.FN_DEVUELVE_DESCRIPCION_DE_TIZADO(TIPO_TIZADO)) AS [TIZADO],
               GRAMAJE_DESPUES_LAVADO_TELA_ANTES_CORTE,
               a.GRAMAJE_LAVANDERIA,
               A.COD_TELA_RELACIONADA,
               DBO.TG_EXTRAE_DESCRIPCION_TX_TELA(A.COD_TELA_RELACIONADA) AS DESCTELARELACIONADA,
               A.COD_TELA_RELACIONADA_TERMOFIJADO,
               DBO.TG_EXTRAE_DESCRIPCION_TX_TELA(A.COD_TELA_RELACIONADA_TERMOFIJADO) AS DESCTELARELACIONADA_TERMOFIJADO,
               B.COD_GRUPOTEX,
               TIEMPO_ESTANDAR_TENIDO,
               ISNULL(B.PROGRAMA_PRINICIPAL, 0) AS PROGRAMA,
               ISNULL(T.DESCRIPCION, '') AS DES_PROGRAMA,
               CASE WHEN B.COD_CLIENTE_TEX = '00001' THEN DBO.TI_MUESTRA_CLASE_ORDEN_COMPRA_CLIENTE_PRINCIPAL(B.COD_ORDTRA) ELSE '' END AS CLASE_ORDEN_COMPRA,
               A.COD_USUARIO_ULTIMA_ACT_TIPO_TIZADO AS USUARIO,
               A.FEC_ULTIMA_ACT_TIPO_TIZADO AS FECHA_MODIF,
               DBO.TI_ORDTRA_ANCHO_PLANTA(A.COD_ORDTRA, A.COD_TELA, A.COD_COMB, A.COD_COLOR) AS ANCHO_PLANTA,
               DBO.[TX_ENCUENTRA_CLIENTE_TEMPORADA_OP_PARTIDA](A.COD_ORDTRA) AS TEMPORADA,
               KGS_A_TENIR,
               A.GRAMAJE_TERMO_PRODUCCION,
               A.ANCHO_TERMO_PRODUCCION,
               A.ENCOG_ANCHO_PRODUCCION,
               A.ENCOG_LARGO_PRODUCCION,
               A.REVIRADO_PRODUCCION,
               A.GRAMAJE_LAVADO_PRODUCCION,
               A.TIP_ANCHO_INFORMADO_PRODUCCION,
               a.PESO_PANO,
               a.GRAMAJE_ESTAMPADO_PRODUCCION,
               a.ANCHO_ESTAMPADO_PRODUCCION,
               a.ENCOG_ANCHO_ESTAMPADO_PRODUCCION,
               a.ENCOG_LARGO_ESTAMPADO_PRODUCCION,
               a.REVIRADO_ESTAMPADO_PRODUCCION,
               B.COD_ORDPRO,
               X.COD_ESTPRO,
               X.COD_VERSION,
               C.REVIRADO AS REVIRADO_TELA,
               FLG_ESREPOSICION,
               COD_ORDTRA_ORIGEN_REPOSICION,
               NRO_NOCONFORMIDAD,
               TIPO_NO_CONFORMIDAD,
               '' AS AREA_REPOSICION,
               '' AS MOTIVO_REPOSICION,
               A.OBS_ESTAMPADO,
               ISNULL(Y.COD_RUTA, '') + '-' + ISNULL(Y.DESCRIPCION, '') AS RUTA,
               K.TIP_ANCHO,
               z.ENCOG_ANCHO_RESIDUALES,
               z.ENCOG_LARGO_RESIDUALES,
               z.REVIRADO_RESIDUALES,
               a.Resistencia_Tela_Real,
               A.COD_RUTA,
               B.Fec_Cierre_Proceso_Productivo,
               ISNULL((SELECT pri.Prioridad_Manufactura
                       FROM   es_grupotex AS pri
                       WHERE  B.COD_GRUPOTEX = pri.Cod_GrupoTex), 0) AS PRIORIDAD,
               a.Gramaje_Real_Rama,
               a.Ancho_Real_Rama,
               a.encog_ancho_real_Rama,
               a.encog_largo_real_Rama,
               a.Revirado_Rama,
               a.Tipo_Rama,
               a.Tipo_Lavado,
               a.Gramaje_Lavado_Rama,
               z1.Cod_GrupoTex AS GRUPO,
               Z2.NOM_CLIENTE AS Cliente_Manuf,
               a.Cod_Estado_Avance,
               ISNULL(XX.DES_Estado_Avance, '') AS DES_Estado_Avance,
               a.Observaciones_Estado_Avance,
               a.Cod_Usuario_Estado_Avance,
               a.Cod_Estacion_Estado_Avance,
               a.Fec_Sistema_Estado_Avance,
               b.Flg_Transferido_a_TexManager,
               b.Fec_Sistema_Transferencia_a_TexManager,
               b.Cod_Usuario_Transferencia_a_TexManager,
               b.Cod_Estacion_Transferencia_a_TexManager,
               Flg_Status_Prueba_Costura,
               Fec_Status_Prueba_Costura,
               Cod_usuario_Prueba_Costura
        INTO   #PARTIDAS_RESULTADO1
        FROM   #ITEMS AS A
               INNER JOIN
               TI_ORDTRA_TINTORERIA AS B
               ON A.COD_ORDTRA = B.COD_ORDTRA
               INNER JOIN
               TX_TELA AS C
               ON A.COD_TELA = C.COD_TELA
               LEFT OUTER JOIN
               TX_TELACOMB AS D
               ON A.COD_TELA = D.COD_TELA
                  AND A.COD_COMB = D.COD_COMB
               INNER JOIN
               LB_COLOR AS E
               ON A.COD_COLOR = E.COD_COLOR
               INNER JOIN
               TI_STATUS_ORDENES_TINTORERIA AS F
               ON A.FLG_STATUS = F.FLG_STATUS
               LEFT OUTER JOIN
               TI_RECETAS_TINTORERIA AS G
               ON A.COD_COLOR = G.COD_COLOR
                  AND B.COD_OPCION = G.COD_OPCION
               INNER JOIN
               TX_CLIENTE AS H
               ON A.COD_CLIENTE_TEX = H.COD_CLIENTE_TEX
               LEFT OUTER JOIN
               TX_MOTIVO_RECHAZO AS I
               ON A.COD_MOTRECHAZO = I.COD_MOTRECHAZO
               LEFT OUTER JOIN
               TI_ORDTRA_TINTORERIA_RELACIONADAS AS J
               ON A.COD_ORDTRA = J.COD_ORDTRA_RELACIONADA
               LEFT OUTER JOIN
               TG_TIPANC AS K
               ON C.TIP_ANCHO = K.TIP_ANCHO
               LEFT OUTER JOIN
               TI_PROGRAMAS_TENIDO AS T
               ON B.PROGRAMA_PRINICIPAL = T.NUM_PROGRAMA
               LEFT OUTER JOIN
               ES_ORDPRO AS X
               ON B.COD_FABRICA = X.COD_FABRICA
                  AND B.COD_ORDPRO = X.COD_ORDPRO
               LEFT OUTER JOIN
               TI_ORDTRA_TINTORERIA_DATTECNICOS_CABECERA AS Y
               ON A.COD_ORDTRA = Y.COD_ORDTRA
                  AND A.COD_TELA = Y.COD_TELA
                  AND A.COD_RUTA = Y.COD_RUTA
               LEFT OUTER JOIN
               TX_ORDTRA_TELAS AS Z
               ON Z.COD_ORDTRA = A.COD_ORDTRA
                  AND Z.COD_TELA = A.COD_TELA
               LEFT OUTER JOIN
               ES_GRUPOTEX AS Z1
               ON B.Cod_GrupoTex = Z1.Cod_GrupoTex
               LEFT OUTER JOIN
               TG_CLIENTE AS Z2
               ON Z1.Cod_Cliente = Z2.Cod_Cliente
               LEFT OUTER JOIN
               TX_Estado_Avance_Telas_Muestras_Solicitudes AS XX
               ON a.Cod_Estado_Avance = XX.Cod_Estado_Avance;
        SELECT COUNT(*) AS TotalRows
        FROM   #PARTIDAS_RESULTADO1;
        ;WITH PaginaPartidas AS
            (
                SELECT R.*, ROW_NUMBER() OVER (ORDER BY COD_ORDTRA DESC, NUM_SECUENCIA) AS NumeroFilaPagina
                FROM #PARTIDAS_RESULTADO1 AS R
            )
            SELECT [COD_ORDTRA], [NP], [NUM_SECUENCIA], [TELA], [NOMBRE], [COD_COLOR], [COLOR], [Relacionadas], [Madre], [COD_COMB], [COMBO], [COMB], [COD_TALLA], [Sel], [HRS_TENIDO], [FEC_1ER_TENIDO], [FEC_FIN_1ER_TENIDO], [FEC_ULT_TENIDO], [FEC_CREACION], [KGS_PROGR], [KGS_ASIGNADOS], [KGS_CRUDO], [KGS_TENIDOS_1RAS], [KGS_DESPACHADOS], [UNI_PROGR], [UNI_ASIGNADOS], [UNI_CRUDO], [UNI_TENIDAS_1RAS], [UNI_DESPACHADOS], [FECHA_ULTIMO_INGRESO_ALMACEN_TELA], [ROLLOS_A_TENIR], [ROLLOS_TENIDOS], [ROLLOS_DESPACHADOS], [STATUS], [COD_CLIENTE_TEX], [CLIENTE], [SER_ORDCOMP], [COD_ORDCOMP], [SEC_ORDCOMP], [OCOMPRA], [RECETA], [PARTIDA_CLIENTE], [REPROCESO], [COD_MAQUINA_TINTO], [FLG_STATUS], [FECHA_DESPACHO], [GRAMAJE_ACAB], [ANCHO_ACAB], [ENCOG_ANCHO], [ENCOG_ANCHO_3RALAV], [ENCOG_LARGO], [ENCOG_LARGO_3RALAV], [FEC_ULTMODDATTEC], [COD_USUARIOULTMODDATTEC], [COD_TIPENC], [REVIRADO], [REVIRADO_3RALAV], [GRAMAJE_LAVADO], [FLG_STATUS_APRO], [OBSERVACIONES], [ELONGACIONLARGO], [COD_MOTRECHAZO], [DES_MOTRECHAZO], [ELONGACIONANCHO], [KGS_RECHAZADOS], [PARTIDA_PRINCIPAL], [TIP_ANCHO_INFORMADO], [ANCHO_ACAB_INFORMADO], [CLASE_OC], [GUIAS], [KGS_EN_PANOS], [KGS_LAVADOS], [NUM_PANOS_CORTADOS], [NUM_PANOS_LAVADOS], [MAQUINA], [FLAGHABILITADAPARADESPACHO], [VALIDADO_ENCOGIMIENTO_CORTE], [TIPO_TIZADO], [TIZADO], [GRAMAJE_DESPUES_LAVADO_TELA_ANTES_CORTE], [GRAMAJE_LAVANDERIA], [COD_TELA_RELACIONADA], [DESCTELARELACIONADA], [COD_TELA_RELACIONADA_TERMOFIJADO], [DESCTELARELACIONADA_TERMOFIJADO], [COD_GRUPOTEX], [TIEMPO_ESTANDAR_TENIDO], [PROGRAMA], [DES_PROGRAMA], [CLASE_ORDEN_COMPRA], [USUARIO], [FECHA_MODIF], [ANCHO_PLANTA], [TEMPORADA], [KGS_A_TENIR], [GRAMAJE_TERMO_PRODUCCION], [ANCHO_TERMO_PRODUCCION], [ENCOG_ANCHO_PRODUCCION], [ENCOG_LARGO_PRODUCCION], [REVIRADO_PRODUCCION], [GRAMAJE_LAVADO_PRODUCCION], [TIP_ANCHO_INFORMADO_PRODUCCION], [PESO_PANO], [GRAMAJE_ESTAMPADO_PRODUCCION], [ANCHO_ESTAMPADO_PRODUCCION], [ENCOG_ANCHO_ESTAMPADO_PRODUCCION], [ENCOG_LARGO_ESTAMPADO_PRODUCCION], [REVIRADO_ESTAMPADO_PRODUCCION], [COD_ORDPRO], [COD_ESTPRO], [COD_VERSION], [REVIRADO_TELA], [FLG_ESREPOSICION], [COD_ORDTRA_ORIGEN_REPOSICION], [NRO_NOCONFORMIDAD], [TIPO_NO_CONFORMIDAD], [AREA_REPOSICION], [MOTIVO_REPOSICION], [OBS_ESTAMPADO], [RUTA], [TIP_ANCHO], [ENCOG_ANCHO_RESIDUALES], [ENCOG_LARGO_RESIDUALES], [REVIRADO_RESIDUALES], [Resistencia_Tela_Real], [COD_RUTA], [Fec_Cierre_Proceso_Productivo], [PRIORIDAD], [Gramaje_Real_Rama], [Ancho_Real_Rama], [encog_ancho_real_Rama], [encog_largo_real_Rama], [Revirado_Rama], [Tipo_Rama], [Tipo_Lavado], [Gramaje_Lavado_Rama], [GRUPO], [Cliente_Manuf], [Cod_Estado_Avance], [DES_Estado_Avance], [Observaciones_Estado_Avance], [Cod_Usuario_Estado_Avance], [Cod_Estacion_Estado_Avance], [Fec_Sistema_Estado_Avance], [Flg_Transferido_a_TexManager], [Fec_Sistema_Transferencia_a_TexManager], [Cod_Usuario_Transferencia_a_TexManager], [Cod_Estacion_Transferencia_a_TexManager], [Flg_Status_Prueba_Costura], [Fec_Status_Prueba_Costura], [Cod_usuario_Prueba_Costura]
            FROM PaginaPartidas
            WHERE NumeroFilaPagina > ((@PAGINA - 1) * @TAMANO_PAGINA)
              AND NumeroFilaPagina <= (@PAGINA * @TAMANO_PAGINA)
            ORDER BY NumeroFilaPagina;
    END
ELSE
    IF @OPCION = '9'
        BEGIN
            SELECT A.COD_ORDTRA,
                   DBO.TI_MUESTRA_OP_NP_PARTIDA(A.COD_ORDTRA) AS NP,
                   A.NUM_SECUENCIA,
                   A.COD_TELA AS TELA,
                   CASE WHEN SUBSTRING(A.COD_TELA, 1, 2) = 'DE'
                             AND A.COD_COMB <> '' THEN RTRIM(ISNULL(DES_COMB, '')) ELSE RTRIM(DES_TELA) END AS NOMBRE,
                   A.COD_COLOR,
                   A.COD_COLOR + '-' + SUBSTRING(DES_COLOR, 1, 40) AS COLOR,
                   Flg_Status_Prueba_Costura,
                   DBO.TI_MUESTRA_PARTIDAS_RELACIONADAS(A.COD_ORDTRA) AS Relacionadas,
                   DBO.TI_MUESTRA_PARTIDAS_MADRE(A.COD_ORDTRA) AS Madre,
                   B.COD_GRUPOTEX,
                   A.KGS_PROGR,
                   A.KGS_ASIGNADOS,
                   KGS_DESPACHADOS,
                   A.COD_COMB,
                   A.COD_COMB AS COMBO,
                   ISNULL(DES_COMB, '') AS COMB,
                   A.COD_TALLA,
                   DBO.TI_CALCULA_HORAS_POR_PARTIDA_PROCESO_ITEM(A.COD_ORDTRA, '01', A.NUM_SECUENCIA) AS HRS_TENIDO,
                   DBO.[TI_OBTIENE_FECHA_HORA_PRIMER_TENIDO_SIN_TURNO](A.COD_ORDTRA) AS FEC_1ER_TENIDO,
                   DBO.TI_OBTIENE_FECHA_HORA_FIN_PRIMER_TENIDO_SIN_TURNO(A.COD_ORDTRA) AS FEC_FIN_1ER_TENIDO,
                   FEC_ULT_TENIDO,
                   B.FEC_CREACION,
                   A.KGS_CRUDO,
                   KGS_TENIDOS_1RAS,
                   UNI_PROGR,
                   UNI_ASIGNADOS,
                   UNI_CRUDO,
                   UNI_TENIDAS_1RAS,
                   UNI_DESPACHADOS,
                   A.FEC_ULT_RECEPCION AS FECHA_ULTIMO_INGRESO_ALMACEN_TELA,
                   ROLLOS_CRUDO AS ROLLOS_A_TENIR,
                   ROLLOS_TENIDOS AS ROLLOS_TENIDOS,
                   ROLLOS_DESPACHADOS AS ROLLOS_DESPACHADOS,
                   A.FLG_STATUS + '-' + RTRIM(F.DESCRIPCION) AS STATUS,
                   A.COD_CLIENTE_TEX,
                   RTRIM(H.NOM_CLIENTE) AS CLIENTE,
                   A.SER_ORDCOMP,
                   A.COD_ORDCOMP,
                   A.SEC_ORDCOMP,
                   a.Cod_Estado_Avance,
                   XX.DES_Estado_Avance,
                   a.Observaciones_Estado_Avance,
                   a.Cod_Usuario_Estado_Avance,
                   a.Cod_Estacion_Estado_Avance,
                   a.Fec_Sistema_Estado_Avance,
                   b.Flg_Transferido_a_TexManager,
                   b.Fec_Sistema_Transferencia_a_TexManager,
                   b.Cod_Usuario_Transferencia_a_TexManager,
                   b.Cod_Estacion_Transferencia_a_TexManager,
                   Fec_Status_Prueba_Costura,
                   Cod_usuario_Prueba_Costura
            INTO   #PARTIDAS_RESULTADO2
            FROM   #ITEMS AS A
                   INNER JOIN
                   TI_ORDTRA_TINTORERIA AS B
                   ON A.COD_ORDTRA = B.COD_ORDTRA
                   INNER JOIN
                   TX_TELA AS C
                   ON A.COD_TELA = C.COD_TELA
                   LEFT OUTER JOIN
                   TX_TELACOMB AS D
                   ON A.COD_TELA = D.COD_TELA
                      AND A.COD_COMB = D.COD_COMB
                   INNER JOIN
                   LB_COLOR AS E
                   ON A.COD_COLOR = E.COD_COLOR
                   INNER JOIN
                   TI_STATUS_ORDENES_TINTORERIA AS F
                   ON A.FLG_STATUS = F.FLG_STATUS
                   LEFT OUTER JOIN
                   TI_RECETAS_TINTORERIA AS G
                   ON A.COD_COLOR = G.COD_COLOR
                      AND B.COD_OPCION = G.COD_OPCION
                   INNER JOIN
                   TX_CLIENTE AS H
                   ON A.COD_CLIENTE_TEX = H.COD_CLIENTE_TEX
                   LEFT OUTER JOIN
                   TX_MOTIVO_RECHAZO AS I
                   ON A.COD_MOTRECHAZO = I.COD_MOTRECHAZO
                   LEFT OUTER JOIN
                   TI_ORDTRA_TINTORERIA_RELACIONADAS AS J
                   ON A.COD_ORDTRA = J.COD_ORDTRA_RELACIONADA
                   LEFT OUTER JOIN
                   TG_TIPANC AS K
                   ON C.TIP_ANCHO = K.TIP_ANCHO
                   LEFT OUTER JOIN
                   TI_PROGRAMAS_TENIDO AS T
                   ON B.PROGRAMA_PRINICIPAL = T.NUM_PROGRAMA
                   LEFT OUTER JOIN
                   ES_ORDPRO AS X
                   ON B.COD_FABRICA = X.COD_FABRICA
                      AND B.COD_ORDPRO = X.COD_ORDPRO
                   LEFT OUTER JOIN
                   TI_ORDTRA_TINTORERIA_DATTECNICOS_CABECERA AS Y
                   ON A.COD_ORDTRA = Y.COD_ORDTRA
                      AND A.COD_TELA = Y.COD_TELA
                      AND A.COD_RUTA = Y.COD_RUTA
                   LEFT OUTER JOIN
                   TX_ORDTRA_TELAS AS Z
                   ON Z.COD_ORDTRA = A.COD_ORDTRA
                      AND Z.COD_TELA = A.COD_TELA
                   LEFT OUTER JOIN
                   TX_Estado_Avance_Telas_Muestras_Solicitudes AS XX
                   ON a.Cod_Estado_Avance = XX.Cod_Estado_Avance;
            SELECT COUNT(*) AS TotalRows
            FROM   #PARTIDAS_RESULTADO2;
            ;WITH PaginaPartidas AS
            (
                SELECT R.*, ROW_NUMBER() OVER (ORDER BY COD_ORDTRA DESC, NUM_SECUENCIA) AS NumeroFilaPagina
                FROM #PARTIDAS_RESULTADO2 AS R
            )
            SELECT [COD_ORDTRA], [NP], [NUM_SECUENCIA], [TELA], [NOMBRE], [COD_COLOR], [COLOR], [Flg_Status_Prueba_Costura], [Relacionadas], [Madre], [COD_GRUPOTEX], [KGS_PROGR], [KGS_ASIGNADOS], [KGS_DESPACHADOS], [COD_COMB], [COMBO], [COMB], [COD_TALLA], [HRS_TENIDO], [FEC_1ER_TENIDO], [FEC_FIN_1ER_TENIDO], [FEC_ULT_TENIDO], [FEC_CREACION], [KGS_CRUDO], [KGS_TENIDOS_1RAS], [UNI_PROGR], [UNI_ASIGNADOS], [UNI_CRUDO], [UNI_TENIDAS_1RAS], [UNI_DESPACHADOS], [FECHA_ULTIMO_INGRESO_ALMACEN_TELA], [ROLLOS_A_TENIR], [ROLLOS_TENIDOS], [ROLLOS_DESPACHADOS], [STATUS], [COD_CLIENTE_TEX], [CLIENTE], [SER_ORDCOMP], [COD_ORDCOMP], [SEC_ORDCOMP], [Cod_Estado_Avance], [DES_Estado_Avance], [Observaciones_Estado_Avance], [Cod_Usuario_Estado_Avance], [Cod_Estacion_Estado_Avance], [Fec_Sistema_Estado_Avance], [Flg_Transferido_a_TexManager], [Fec_Sistema_Transferencia_a_TexManager], [Cod_Usuario_Transferencia_a_TexManager], [Cod_Estacion_Transferencia_a_TexManager], [Fec_Status_Prueba_Costura], [Cod_usuario_Prueba_Costura]
            FROM PaginaPartidas
            WHERE NumeroFilaPagina > ((@PAGINA - 1) * @TAMANO_PAGINA)
              AND NumeroFilaPagina <= (@PAGINA * @TAMANO_PAGINA)
            ORDER BY NumeroFilaPagina;
        END
    ELSE
        BEGIN
            SELECT A.COD_ORDTRA,
                   DBO.TI_MUESTRA_OP_NP_PARTIDA(A.COD_ORDTRA) AS NP,
                   A.NUM_SECUENCIA,
                   A.COD_TELA AS TELA,
                   CASE WHEN SUBSTRING(A.COD_TELA, 1, 2) = 'DE'
                             AND A.COD_COMB <> '' THEN RTRIM(ISNULL(DES_COMB, '')) ELSE RTRIM(DES_TELA) END AS NOMBRE,
                   A.COD_COLOR,
                   A.COD_COLOR + '-' + SUBSTRING(DES_COLOR, 1, 40) AS COLOR,
                   Flg_Status_Prueba_Costura,
                   DBO.TI_MUESTRA_PARTIDAS_RELACIONADAS(A.COD_ORDTRA) AS Relacionadas,
                   DBO.TI_MUESTRA_PARTIDAS_MADRE(A.COD_ORDTRA) AS Madre,
                   B.COD_GRUPOTEX,
                   B.Cod_GrupoTex AS GRUPO,
                   Z2.NOM_CLIENTE AS Cliente_Manuf,
                   A.KGS_PROGR,
                   A.KGS_ASIGNADOS,
                   KGS_DESPACHADOS,
                   A.COD_COMB,
                   A.COD_COMB AS COMBO,
                   ISNULL(DES_COMB, '') AS COMB,
                   A.COD_TALLA,
                   CONVERT (BIT, 0) AS Sel,
                   DBO.TI_CALCULA_HORAS_POR_PARTIDA_PROCESO_ITEM(A.COD_ORDTRA, '01', A.NUM_SECUENCIA) AS HRS_TENIDO,
                   DBO.[TI_OBTIENE_FECHA_HORA_PRIMER_TENIDO_SIN_TURNO](A.COD_ORDTRA) AS FEC_1ER_TENIDO,
                   DBO.TI_OBTIENE_FECHA_HORA_FIN_PRIMER_TENIDO_SIN_TURNO(A.COD_ORDTRA) AS FEC_FIN_1ER_TENIDO,
                   FEC_ULT_TENIDO,
                   B.FEC_CREACION,
                   A.KGS_CRUDO,
                   KGS_TENIDOS_1RAS,
                   UNI_PROGR,
                   UNI_ASIGNADOS,
                   UNI_CRUDO,
                   UNI_TENIDAS_1RAS,
                   UNI_DESPACHADOS,
                   A.FEC_ULT_RECEPCION AS FECHA_ULTIMO_INGRESO_ALMACEN_TELA,
                   ROLLOS_CRUDO AS ROLLOS_A_TENIR,
                   ROLLOS_TENIDOS AS ROLLOS_TENIDOS,
                   ROLLOS_DESPACHADOS AS ROLLOS_DESPACHADOS,
                   A.FLG_STATUS + '-' + RTRIM(F.DESCRIPCION) AS STATUS,
                   A.COD_CLIENTE_TEX,
                   RTRIM(H.NOM_CLIENTE) AS CLIENTE,
                   A.SER_ORDCOMP,
                   A.COD_ORDCOMP,
                   A.SEC_ORDCOMP,
                   A.SER_ORDCOMP + '-' + A.COD_ORDCOMP AS OCOMPRA,
                   B.COD_OPCION + '-' + RTRIM(G.DESCRIPCION) AS RECETA,
                   B.COD_ORDPROV_CLIENTE AS PARTIDA_CLIENTE,
                   FLG_REPROCESO AS REPROCESO,
                   B.COD_MAQUINA_TINTO,
                   A.FLG_STATUS,
                   @FECHA_DESPACHO AS FECHA_DESPACHO,
                   A.GRAMAJE_ACAB,
                   ISNULL(K.FACTOR, 1) * A.ANCHO_ACAB AS ANCHO_ACAB,
                   A.ENCOG_ANCHO,
                   A.ENCOG_ANCHO_3RALAV,
                   A.ENCOG_LARGO,
                   A.ENCOG_LARGO_3RALAV,
                   A.FEC_ULTMODDATTEC,
                   A.COD_USUARIOULTMODDATTEC,
                   A.COD_TIPENC,
                   A.REVIRADO,
                   A.REVIRADO_3RALAV,
                   A.GRAMAJE_LAVADO,
                   A.FLG_STATUS_APRO,
                   A.OBSERVACIONES,
                   A.ELONGACIONLARGO,
                   A.COD_MOTRECHAZO,
                   ISNULL(I.DES_MOTRECHAZO, '') AS DES_MOTRECHAZO,
                   A.ELONGACIONANCHO,
                   A.KGS_RECHAZADOS,
                   ISNULL(J.COD_ORDTRA, '') AS PARTIDA_PRINCIPAL,
                   TIP_ANCHO_INFORMADO,
                   ANCHO_ACAB_INFORMADO,
                   DBO.TI_ENCUENTRA_CLASE_OC_PARTIDA(A.COD_ORDTRA) AS CLASE_OC,
                   DBO.TH_OBTIENES_KGS_CRUDO_PARTIDA_SECUENCIA(A.COD_ORDTRA, A.NUM_SECUENCIA) AS GUIAS,
                   A.KGS_EN_PANOS,
                   A.KGS_LAVADOS,
                   A.NUM_PANOS_CORTADOS,
                   A.NUM_PANOS_LAVADOS,
                   COD_MAQUINA_TINTO AS MAQUINA,
                   B.FLG_HABILITADA_PARA_DESPACHO AS FLAGHABILITADAPARADESPACHO,
                   A.FLG_VALIDACION_ENCOGIMIENTO_CORTE AS VALIDADO_ENCOGIMIENTO_CORTE,
                   TIPO_TIZADO,
                   (SELECT DBO.FN_DEVUELVE_DESCRIPCION_DE_TIZADO(TIPO_TIZADO)) AS [TIZADO],
                   GRAMAJE_DESPUES_LAVADO_TELA_ANTES_CORTE,
                   a.GRAMAJE_LAVANDERIA,
                   A.COD_TELA_RELACIONADA,
                   DBO.TG_EXTRAE_DESCRIPCION_TX_TELA(A.COD_TELA_RELACIONADA) AS DESCTELARELACIONADA,
                   A.COD_TELA_RELACIONADA_TERMOFIJADO,
                   DBO.TG_EXTRAE_DESCRIPCION_TX_TELA(A.COD_TELA_RELACIONADA_TERMOFIJADO) AS DESCTELARELACIONADA_TERMOFIJADO,
                   TIEMPO_ESTANDAR_TENIDO,
                   ISNULL(B.PROGRAMA_PRINICIPAL, 0) AS PROGRAMA,
                   ISNULL(T.DESCRIPCION, '') AS DES_PROGRAMA,
                   CASE WHEN B.COD_CLIENTE_TEX = '00001' THEN DBO.TI_MUESTRA_CLASE_ORDEN_COMPRA_CLIENTE_PRINCIPAL(B.COD_ORDTRA) ELSE '' END AS CLASE_ORDEN_COMPRA,
                   A.COD_USUARIO_ULTIMA_ACT_TIPO_TIZADO AS USUARIO,
                   A.FEC_ULTIMA_ACT_TIPO_TIZADO AS FECHA_MODIF,
                   DBO.TI_ORDTRA_ANCHO_PLANTA(A.COD_ORDTRA, A.COD_TELA, A.COD_COMB, A.COD_COLOR) AS ANCHO_PLANTA,
                   DBO.[TX_ENCUENTRA_CLIENTE_TEMPORADA_OP_PARTIDA](A.COD_ORDTRA) AS TEMPORADA,
                   KGS_A_TENIR,
                   A.GRAMAJE_TERMO_PRODUCCION,
                   A.ANCHO_TERMO_PRODUCCION,
                   A.ENCOG_ANCHO_PRODUCCION,
                   A.ENCOG_LARGO_PRODUCCION,
                   A.REVIRADO_PRODUCCION,
                   A.GRAMAJE_LAVADO_PRODUCCION,
                   A.TIP_ANCHO_INFORMADO_PRODUCCION,
                   a.PESO_PANO,
                   a.GRAMAJE_ESTAMPADO_PRODUCCION,
                   a.ANCHO_ESTAMPADO_PRODUCCION,
                   a.ENCOG_ANCHO_ESTAMPADO_PRODUCCION,
                   a.ENCOG_LARGO_ESTAMPADO_PRODUCCION,
                   a.REVIRADO_ESTAMPADO_PRODUCCION,
                   B.COD_ORDPRO,
                   X.COD_ESTPRO,
                   X.COD_VERSION,
                   C.REVIRADO AS REVIRADO_TELA,
                   FLG_ESREPOSICION,
                   COD_ORDTRA_ORIGEN_REPOSICION,
                   NRO_NOCONFORMIDAD,
                   TIPO_NO_CONFORMIDAD,
                   '' AS AREA_REPOSICION,
                   '' AS MOTIVO_REPOSICION,
                   A.OBS_ESTAMPADO,
                   ISNULL(Y.COD_RUTA, '') + '-' + ISNULL(Y.DESCRIPCION, '') AS RUTA,
                   K.TIP_ANCHO,
                   z.ENCOG_ANCHO_RESIDUALES,
                   z.ENCOG_LARGO_RESIDUALES,
                   z.REVIRADO_RESIDUALES,
                   a.Resistencia_Tela_Real,
                   A.COD_RUTA,
                   B.Fec_Cierre_Proceso_Productivo,
                   ISNULL((SELECT pri.Prioridad_Manufactura
                           FROM   es_grupotex AS pri
                           WHERE  B.COD_GRUPOTEX = pri.Cod_GrupoTex), 0) AS PRIORIDAD,
                   a.Gramaje_Real_Rama,
                   a.Ancho_Real_Rama,
                   a.encog_ancho_real_Rama,
                   a.encog_largo_real_Rama,
                   a.Revirado_Rama,
                   a.Tipo_Rama,
                   a.Tipo_Lavado,
                   a.Gramaje_Lavado_Rama,
                   a.Cod_Estado_Avance,
                   XX.DES_Estado_Avance,
                   a.Observaciones_Estado_Avance,
                   a.Cod_Usuario_Estado_Avance,
                   a.Cod_Estacion_Estado_Avance,
                   a.Fec_Sistema_Estado_Avance,
                   b.Flg_Transferido_a_TexManager,
                   b.Fec_Sistema_Transferencia_a_TexManager,
                   b.Cod_Usuario_Transferencia_a_TexManager,
                   b.Cod_Estacion_Transferencia_a_TexManager,
                   Fec_Status_Prueba_Costura,
                   Cod_usuario_Prueba_Costura
            INTO   #PARTIDAS_RESULTADO3
            FROM   #ITEMS AS A
                   INNER JOIN
                   TI_ORDTRA_TINTORERIA AS B
                   ON A.COD_ORDTRA = B.COD_ORDTRA
                   INNER JOIN
                   TX_TELA AS C
                   ON A.COD_TELA = C.COD_TELA
                   LEFT OUTER JOIN
                   TX_TELACOMB AS D
                   ON A.COD_TELA = D.COD_TELA
                      AND A.COD_COMB = D.COD_COMB
                   INNER JOIN
                   LB_COLOR AS E
                   ON A.COD_COLOR = E.COD_COLOR
                   INNER JOIN
                   TI_STATUS_ORDENES_TINTORERIA AS F
                   ON A.FLG_STATUS = F.FLG_STATUS
                   LEFT OUTER JOIN
                   TI_RECETAS_TINTORERIA AS G
                   ON A.COD_COLOR = G.COD_COLOR
                      AND B.COD_OPCION = G.COD_OPCION
                   INNER JOIN
                   TX_CLIENTE AS H
                   ON A.COD_CLIENTE_TEX = H.COD_CLIENTE_TEX
                   LEFT OUTER JOIN
                   TX_MOTIVO_RECHAZO AS I
                   ON A.COD_MOTRECHAZO = I.COD_MOTRECHAZO
                   LEFT OUTER JOIN
                   TI_ORDTRA_TINTORERIA_RELACIONADAS AS J
                   ON A.COD_ORDTRA = J.COD_ORDTRA_RELACIONADA
                   LEFT OUTER JOIN
                   TG_TIPANC AS K
                   ON C.TIP_ANCHO = K.TIP_ANCHO
                   LEFT OUTER JOIN
                   TI_PROGRAMAS_TENIDO AS T
                   ON B.PROGRAMA_PRINICIPAL = T.NUM_PROGRAMA
                   LEFT OUTER JOIN
                   ES_ORDPRO AS X
                   ON B.COD_FABRICA = X.COD_FABRICA
                      AND B.COD_ORDPRO = X.COD_ORDPRO
                   LEFT OUTER JOIN
                   TI_ORDTRA_TINTORERIA_DATTECNICOS_CABECERA AS Y
                   ON A.COD_ORDTRA = Y.COD_ORDTRA
                      AND A.COD_TELA = Y.COD_TELA
                      AND A.COD_RUTA = Y.COD_RUTA
                   LEFT OUTER JOIN
                   TX_ORDTRA_TELAS AS Z
                   ON Z.COD_ORDTRA = A.COD_ORDTRA
                      AND Z.COD_TELA = A.COD_TELA
                   LEFT OUTER JOIN
                   ES_GRUPOTEX AS Z1
                   ON B.Cod_GrupoTex = Z1.Cod_GrupoTex
                   LEFT OUTER JOIN
                   TG_CLIENTE AS Z2
                   ON Z1.Cod_Cliente = Z2.Cod_Cliente
                   LEFT OUTER JOIN
                   TX_Estado_Avance_Telas_Muestras_Solicitudes AS XX
                   ON a.Cod_Estado_Avance = XX.Cod_Estado_Avance;
            SELECT COUNT(*) AS TotalRows
            FROM   #PARTIDAS_RESULTADO3;
            ;WITH PaginaPartidas AS
            (
                SELECT R.*, ROW_NUMBER() OVER (ORDER BY COD_ORDTRA DESC, NUM_SECUENCIA) AS NumeroFilaPagina
                FROM #PARTIDAS_RESULTADO3 AS R
            )
            SELECT [COD_ORDTRA], [NP], [NUM_SECUENCIA], [TELA], [NOMBRE], [COD_COLOR], [COLOR], [Flg_Status_Prueba_Costura], [Relacionadas], [Madre], [COD_GRUPOTEX], [GRUPO], [Cliente_Manuf], [KGS_PROGR], [KGS_ASIGNADOS], [KGS_DESPACHADOS], [COD_COMB], [COMBO], [COMB], [COD_TALLA], [Sel], [HRS_TENIDO], [FEC_1ER_TENIDO], [FEC_FIN_1ER_TENIDO], [FEC_ULT_TENIDO], [FEC_CREACION], [KGS_CRUDO], [KGS_TENIDOS_1RAS], [UNI_PROGR], [UNI_ASIGNADOS], [UNI_CRUDO], [UNI_TENIDAS_1RAS], [UNI_DESPACHADOS], [FECHA_ULTIMO_INGRESO_ALMACEN_TELA], [ROLLOS_A_TENIR], [ROLLOS_TENIDOS], [ROLLOS_DESPACHADOS], [STATUS], [COD_CLIENTE_TEX], [CLIENTE], [SER_ORDCOMP], [COD_ORDCOMP], [SEC_ORDCOMP], [OCOMPRA], [RECETA], [PARTIDA_CLIENTE], [REPROCESO], [COD_MAQUINA_TINTO], [FLG_STATUS], [FECHA_DESPACHO], [GRAMAJE_ACAB], [ANCHO_ACAB], [ENCOG_ANCHO], [ENCOG_ANCHO_3RALAV], [ENCOG_LARGO], [ENCOG_LARGO_3RALAV], [FEC_ULTMODDATTEC], [COD_USUARIOULTMODDATTEC], [COD_TIPENC], [REVIRADO], [REVIRADO_3RALAV], [GRAMAJE_LAVADO], [FLG_STATUS_APRO], [OBSERVACIONES], [ELONGACIONLARGO], [COD_MOTRECHAZO], [DES_MOTRECHAZO], [ELONGACIONANCHO], [KGS_RECHAZADOS], [PARTIDA_PRINCIPAL], [TIP_ANCHO_INFORMADO], [ANCHO_ACAB_INFORMADO], [CLASE_OC], [GUIAS], [KGS_EN_PANOS], [KGS_LAVADOS], [NUM_PANOS_CORTADOS], [NUM_PANOS_LAVADOS], [MAQUINA], [FLAGHABILITADAPARADESPACHO], [VALIDADO_ENCOGIMIENTO_CORTE], [TIPO_TIZADO], [TIZADO], [GRAMAJE_DESPUES_LAVADO_TELA_ANTES_CORTE], [GRAMAJE_LAVANDERIA], [COD_TELA_RELACIONADA], [DESCTELARELACIONADA], [COD_TELA_RELACIONADA_TERMOFIJADO], [DESCTELARELACIONADA_TERMOFIJADO], [TIEMPO_ESTANDAR_TENIDO], [PROGRAMA], [DES_PROGRAMA], [CLASE_ORDEN_COMPRA], [USUARIO], [FECHA_MODIF], [ANCHO_PLANTA], [TEMPORADA], [KGS_A_TENIR], [GRAMAJE_TERMO_PRODUCCION], [ANCHO_TERMO_PRODUCCION], [ENCOG_ANCHO_PRODUCCION], [ENCOG_LARGO_PRODUCCION], [REVIRADO_PRODUCCION], [GRAMAJE_LAVADO_PRODUCCION], [TIP_ANCHO_INFORMADO_PRODUCCION], [PESO_PANO], [GRAMAJE_ESTAMPADO_PRODUCCION], [ANCHO_ESTAMPADO_PRODUCCION], [ENCOG_ANCHO_ESTAMPADO_PRODUCCION], [ENCOG_LARGO_ESTAMPADO_PRODUCCION], [REVIRADO_ESTAMPADO_PRODUCCION], [COD_ORDPRO], [COD_ESTPRO], [COD_VERSION], [REVIRADO_TELA], [FLG_ESREPOSICION], [COD_ORDTRA_ORIGEN_REPOSICION], [NRO_NOCONFORMIDAD], [TIPO_NO_CONFORMIDAD], [AREA_REPOSICION], [MOTIVO_REPOSICION], [OBS_ESTAMPADO], [RUTA], [TIP_ANCHO], [ENCOG_ANCHO_RESIDUALES], [ENCOG_LARGO_RESIDUALES], [REVIRADO_RESIDUALES], [Resistencia_Tela_Real], [COD_RUTA], [Fec_Cierre_Proceso_Productivo], [PRIORIDAD], [Gramaje_Real_Rama], [Ancho_Real_Rama], [encog_ancho_real_Rama], [encog_largo_real_Rama], [Revirado_Rama], [Tipo_Rama], [Tipo_Lavado], [Gramaje_Lavado_Rama], [Cod_Estado_Avance], [DES_Estado_Avance], [Observaciones_Estado_Avance], [Cod_Usuario_Estado_Avance], [Cod_Estacion_Estado_Avance], [Fec_Sistema_Estado_Avance], [Flg_Transferido_a_TexManager], [Fec_Sistema_Transferencia_a_TexManager], [Cod_Usuario_Transferencia_a_TexManager], [Cod_Estacion_Transferencia_a_TexManager], [Fec_Status_Prueba_Costura], [Cod_usuario_Prueba_Costura]
            FROM PaginaPartidas
            WHERE NumeroFilaPagina > ((@PAGINA - 1) * @TAMANO_PAGINA)
              AND NumeroFilaPagina <= (@PAGINA * @TAMANO_PAGINA)
            ORDER BY NumeroFilaPagina;
        END

