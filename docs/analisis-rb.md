# Análisis-Rb: cantidad programada

Ruta: /logistica/analisis-rb. Permiso: orders.query (Logística / Consultas).

Entrada: NP exacta de cinco caracteres. La búsqueda normaliza espacios externos y mayúsculas. Buscar y Enter ejecutan GET /api/v1/logistica/analisis-rb?np=J1907. Devuelve filas con np y programmedQuantity; una NP inexistente devuelve una lista vacía.

Fuente: dbo.ES_OrdPro.num_prenpro. Fábrica 001 por defecto, consistente con las NP del cuadro recibido. Puede configurarse mediante AnalisisRb:Factory en el backend. No se suman fábricas ni cantidades de corte o despacho.

Registro SQL pendiente: ejecutar backend/sql/PA_ES_ORDPRO_S0003.sql en la base de datos usada por el backend. El desarrollo no creó ni modificó objetos de HIALPESA. El procedimiento solo consulta datos.

Validación real de lectura sobre HIALPESA: J1907=438, J1901=1432, J1900=1101, J1897=1432. Coinciden con la columna Programado del cuadro proporcionado. Se conserva el contexto de sesión y autorización de la intranet. No hay operaciones de escritura.

Publicar Front, Back y registrar el SQL antes de utilizar la consulta. No se incluyen datos de demostración en la respuesta de producción.

## Precio comercial y tiempo de costura

Volver a registrar PA_ES_ORDPRO_S0003.sql para agregar CommercialPrice y SewingTime. Precio de TG_COTIZACION_ESTILOSPROPIOS.MObra_Confec, priorizando la cotización aprobada asignada y usando el máximo de las cotizaciones aprobadas relacionadas como alternativa, de acuerdo con Gestión. No se recalcula a partir del costo por minuto para evitar cambiar el precio registrado. Los OUTER APPLY mantienen una fila por NP y preservan las NP sin cotización. La ausencia de precio se muestra como Sin dato. Tiempo de CF_TIEMPO_TOTAL_COSTURA_OP; incluye exclusivamente costura y se presenta con dos decimales. J1825 coincide con el Excel: 1.4040 y 11.51.

## Formato de cotización del Excel

Se reproducen los bloques en el orden del archivo Libro1.xlsx: base (fila 10), cotización completa/parcial (fila 13), resumen parcial (fila 18), otros gastos (fila 19) y total (fila 20).

Entradas de la NP: A10=CommercialPrice; B10=SewingTime. B13 (tiempo considerado) inicia con B10 y puede editarse. C13 (precio por servicio) inicia vacío; C19 (otros gastos) inicia en 0.03 y puede editarse. No se copia el 1.30 del ejemplo como dato real. Estos valores se incorporan al historial de Gestión cuando el usuario pulsa Guardar.

Fórmulas conservadas:
- C10=A10/B10; H10=B10-B13.
- B12=SI(B10=B13,TIEMPO COMPLETO,TIEMPO PARCIAL).
- A13=B13*A10/B10; D13=C13/B13; E13=A13-C13; F13=E13/A13.
- H13=SI(B12=TIEMPO COMPLETO,COSTURA COMPLETA,COSTURA PARCIAL).
- Fila 18 copia A13,B13,C13,D13,E13,F13,H13.
- C20=C18+C19; el resto de fila 20 copia la fila 18. En particular D20/E20/F20 no recalculan con los gastos.

Se calcula con precisión completa y solo se redondea al mostrar, con los formatos del Excel. Si falta un dato o el divisor es cero, se muestra Sin dato en el cálculo afectado. Las diferencias y rentabilidades negativas se conservan.

La cabecera usa: cliente de TG_Cliente; despacho de ES_OrdPro.Fec_DespachoAct; líneas distintas de PL_TRABAJOS/CF_Lineas_Produccion; categoría de TG_LOTESTPRO/TG_LOTCOLTAL/TG_DIVPRE; descripción de tela de ES_EstProVer.Des_Tela_Cliente. La fecha actual se muestra en America/Bogota. La cantidad de cabecera corresponde a la cantidad programada actual: J1825 tiene 217, aunque el Excel de ejemplo contiene 285.

Volver a registrar PA_ES_ORDPRO_S0003.sql para obtener la cabecera ampliada. SET ARITHABORT ON dentro del SP asegura compatibilidad con los métodos XML utilizados para listar líneas y categorías, sin duplicar la fila de NP. El DTO conserva compatibilidad con las cuatro columnas de la versión anterior.

## Guardar e historial (2026-10-28)

Guardar registra una copia completa de lo que está viendo el usuario: NP, cantidad, precios y tiempo de origen, cabecera, tres valores editables, resultados calculados, fecha del análisis y versión de las reglas. No se vuelven a consultar los datos de la NP al guardar. Los resultados se calculan nuevamente en el backend usando las mismas reglas del Excel; la API no acepta resultados enviados por el navegador. La identidad de Gestión se obtiene de la sesión autenticada.

Ver historial lista 50 guardados por página, con fecha, usuario, número de registro y revisión. Ver muestra los datos y resultados almacenados, en modo de lectura; Volver al análisis actual recupera los valores que el usuario estaba editando. No recalcula guardados antiguos con los datos actuales de la NP. Un RequestId único permite reintentar un guardado fallido sin duplicarlo.

Registrar manualmente, en este orden, en la base utilizada por la API:
1. `backend/sql/01_LG_ANALISIS_RB_HISTORIAL_Tabla.sql`: crea `dbo.LG_ANALISIS_RB_HISTORIAL`.
2. `backend/sql/02_LG_ANALISIS_RB_HISTORIAL_CRUD.sql`: crea I0001, U0001, D0001 y S0001.

Los scripts son compatibles con SQL Server 2008 R2. SnapshotJson contiene una copia completa; los campos principales se almacenan también en columnas para filtrar el historial. SQL Server 2008 no interpreta el JSON: la API lo serializa y la web lo lee para mostrar el detalle. Las fechas de auditoría se guardan en UTC y se presentan con desplazamiento -05:00.

I0001 crea un guardado independiente. U0001 añade una revisión que referencia el Id anterior y conserva el historial. D0001 añade una revisión marcada D (eliminación lógica), conservando la copia original. Solo se puede corregir o eliminar la revisión más reciente de una cadena; se rechazan cambios concurrentes sobre una revisión que ya tiene sucesora. La web expone Guardar y Ver historial; U0001 y D0001 quedan suministrados para las operaciones administrativas solicitadas. El historial incluye todas las acciones, también las marcadas D.

GET `/api/v1/logistica/analisis-rb/historial?np=J1825&page=1` lista resúmenes; añadir `id=42` devuelve la copia completa de ese registro. POST sobre la misma ruta exige permiso de consulta de logística, identidad autenticada habilitada para escribir y cabecera `X-Intranet-Write: 1`. Guarda `requestId`, `source`, `usedTime`, `servicePrice` y `otherExpenses`. Si los scripts aún no se registraron, se informa que faltan los objetos del historial.

Validación: 247 pruebas backend aprobadas; prueba de navegador con API simulada cubre guardado completo, error y reintento con el mismo identificador, bloqueo de doble guardado sin cambios, detalle histórico de lectura, recuperación del borrador y vista móvil. Los scripts no se ejecutaron contra HIALPESA ni se insertaron análisis reales durante esta implementación.

## Pantalla de historial independiente
Ver historial navega a /logistica/analisis-rb/historial?np=J1825 (AnalysisHistoryComponent). La búsqueda principal ya no contiene el listado ni el detalle histórico. Volver al análisis regresa a la NP y recupera el borrador en memoria, incluidos los valores manuales y el identificador de reintento. La recarga directa del historial vuelve a consultar la NP indicada en la URL. Ambas rutas exigen orders.query. Verificado en navegador: navegación, detalle de lectura, retorno con valores conservados, recarga directa y móvil.


## Imagen referencial por NP
La pantalla principal muestra la imagen del estilo al lado de la cabecera de cotización. Se replica la relación de Tarifado Variantes de Gestión: ES_OrdPro.Cod_EstPro -> ES_EstPro.Cod_EstPro -> ES_EstPro.icono1. Ejemplo J1825: estilo 99863, \\hialpesa103\EstPro$\EP99863.jpg. Gestión carga ese archivo con LoadPicture en Tarifado/Forms/frmTarifadoVariantes.frm. No se modifica Gestión ni se registra un nuevo procedimiento SQL.

GET /api/v1/logistica/analisis-rb/imagen?np=J1825 entrega los bytes desde el backend con la sesión y permiso orders.query. El navegador no recibe ni abre rutas UNC. Se valida la NP, se parametriza el SELECT y se restringen las rutas a AnalisisRb:ImageRoots (arreglo configurable; por defecto \\hialpesa103\EstPro$). Admite JPG, PNG, GIF, WebP y BMP hasta 10 MB. Respuesta privada sin caché y nosniff. La falta de archivo/NP/imagen devuelve 404; la web muestra un mensaje y no conserva la imagen de otra NP. Los errores de acceso se registran con el mecanismo existente.

En IIS, la identidad que ejecuta el backend necesita permiso de lectura tanto en el recurso compartido EstPro$ como en la carpeta NTFS del servidor Hialpesa103. La autenticación SQL de la aplicación no otorga permisos a archivos. No se modificaron permisos del recurso compartido. La imagen se consulta al abrir la NP; no forma parte de los snapshots históricos ya guardados.

Validación: lectura real de J1825 (127882 bytes) y J1907 (16874 bytes); NP inexistente sin imagen. 256 pruebas backend aprobadas, incluidas rutas permitidas, rechazo de rutas externas, autenticación y normalización. Navegador con imagen real como fixture: carga, ausencia de imagen, cambio de NP y móvil.

## Ruta de imagen en cada guardado y ampliación
ImagePath se incorpora a la fila consultada desde ES_EstPro.icono1, y al guardar se conserva ese valor con el resto del snapshot. La columna nullable ImagePath de LG_ANALISIS_RB_HISTORIAL permite consultarlo directamente en SQL; también permanece dentro de SnapshotJson.Source. Los procedimientos de inserción, actualización, eliminación lógica y selección conservan/devuelven la ruta correspondiente. El detalle histórico muestra Ruta de imagen guardada.

Para una instalación existente ejecutar solamente backend/sql/03_LG_ANALISIS_RB_HISTORIAL_Imagen.sql: añade ImagePath nvarchar(1024) si falta y actualiza los cuatro procedimientos. Los scripts 01 y 02 también están actualizados para instalaciones nuevas. Los guardados anteriores conservan NULL para no asignarles una ruta que no se conocía entonces. Esta migración se validó en SQL Server con PARSEONLY; no se ejecutó contra los objetos reales. Aplicarla antes de utilizar el nuevo backend.

La ruta permite comparar cambios de ubicación/nombre; no conserva una copia del archivo ni detecta modificaciones de sus bytes en la misma ruta. La imagen principal abre un diálogo ampliado al hacer clic o pulsar Enter sobre ella, con cierre mediante Cerrar, Escape o el fondo. Respeta el tamaño de pantalla y recupera el foco al cerrar.

Validación: 258 pruebas backend aprobadas; lectura real de la ruta de J1825; navegador con payload de guardado que incluye ImagePath y detalle histórico que muestra su valor; popup con la imagen real, cierre por botón y Escape. No se insertaron ni modificaron registros reales durante las pruebas.

## Imagen en el detalle histórico
El detalle histórico muestra la imagen y su popup, en lugar del texto de la ruta. GET /api/v1/logistica/analisis-rb/imagen/historial?np=J1907&id=3 resuelve exclusivamente ImagePath de LG_ANALISIS_RB_HISTORIAL, filtrando Id, NP y fábrica; aplica las mismas restricciones de carpeta, tamaño, formato y autenticación. No usa icono1 actual como alternativa. Si un guardado carece de ruta o archivo, se muestra el mensaje de imagen no disponible.
Verificación real de solo lectura: registro 3 de J1907 entrega 16874 bytes desde su ruta histórica; otra NP con ese Id no obtiene la imagen. 263 pruebas backend aprobadas. Navegador verifica la URL por Id, carga, popup, retorno y móvil. No requiere cambios SQL adicionales al script 03 existente.

## Operaciones de costura desde el reporte de Gestión
El botón Operaciones de costura abre /logistica/analisis-rb/costura?np=J1825 en un componente independiente. Conserva el borrador de la cotización al volver. La pantalla muestra los bloques, secuencia, operación, variante de operación, descripción, familia/grupo de máquina, categoría y tiempo estándar, con subtotales por bloque y resumen total/máquina.

Rastreo: Tarifado/Forms/frmTarifadoVariantes.frm, Case IMPRIMIR -> Reporte -> Tarifado/RptTarifadoOpe.xlt -> macro REPORTE -> EXEC UP_REP_CFTARIFADO_OPERACIONES @COD_TARIFADO,@COD_VARIANTE_TARIFADO. Se inspeccionó la macro sin ejecutarla. El procedimiento devuelve todos los sectores; la API filtra exclusivamente COD_SECCONF='CS'. No se modificó el procedimiento ni el código de Gestión. Sus tablas temporales son internas al reporte; no escribe datos de negocio.

El tarifado y variante asignada se resuelven de ES_OrdPro por fábrica/NP. Se permite seleccionar otra variante de CF_TARIFADO_VARIANTES del mismo tarifado, como la lista de Gestión; las variantes ajenas se rechazan. Sin variante explícita se usa la asignada, sin elegir arbitrariamente otra variante. El orden recibido del reporte y la secuencia dentro del bloque se conservan. Tiempo máquina replica la macro: suma de tiempos donde COD_TIPOFAMOPE <> 'M'. Los tiempos se presentan con 3 decimales. Esta pantalla es de consulta; no selecciona operaciones ni modifica el tiempo considerado de la cotización.

GET /api/v1/logistica/analisis-rb/costura?np=J1825&variant=0 utiliza la sesión y permiso orders.query. Variante es opcional. NP y variante se validan y los parámetros SQL no se concatenan. No se requieren scripts SQL adicionales.

Validación de lectura real: J1825, tarifado 99863/0, 19 operaciones CS, total 11.510, máquina 10.270; I9337, tarifado A6298/0, 38 operaciones CS, total 28.044, máquina 25.204. Variante C de J1825 devuelve 21 operaciones CS. Variante ajena Z rechazada. 272 pruebas backend aprobadas; navegador verifica navegación, filas y totales con respuestas reales como fixtures, cambio de variante, retorno con borrador, recarga directa, error y móvil.

## Filtro de sectores del tarifado
La pantalla y el botón se denominan Operaciones del tarifado. El filtro Sector obtiene los códigos y nombres reales del resultado de UP_REP_CFTARIFADO_OPERACIONES (COD_SECCONF y DES_SECCONF), sin catálogo fijo en el frontend. La API recibe el parámetro opcional sector, devuelve solo sus filas y expone la lista completa de sectores disponibles de esa variante. Detalle, subtotales y tiempos total/máquina corresponden al sector elegido.

Sin sector explícito abre CS si existe; de lo contrario el primer sector disponible. Al cambiar de variante se vuelve a la selección predeterminada de la nueva variante. Un sector explícito inexistente se rechaza. Se conserva la ruta /logistica/analisis-rb/costura y el endpoint /api/v1/logistica/analisis-rb/costura por compatibilidad. No requiere scripts SQL.

Validación: J1825/0 devuelve CS COSTURA (19 operaciones, 11.510 min), CO CORTE (9, 1.557), AC ACABADO (7, 2.743), CL ASEG.CALIDAD (4, 1.010). 275 pruebas backend aprobadas. Navegador verifica los cuatro sectores con respuestas reales como fixtures, filas y totales, cambio de variante, móvil y retorno con borrador.

## Selección de operaciones y cotización parcial
Cada bloque tiene un checkbox que marca/desmarca todas sus operaciones, con estado intermedio cuando solo algunas están seleccionadas. Cada fila tiene su checkbox; Seleccionar todo el sector controla todos los bloques. La selección inicial incluye todas las operaciones. Se muestran la suma seleccionada por bloque y la suma del sector; los cálculos de selección se redondean a 6 decimales antes de transferir el tiempo.

Siguiente exige al menos una operación y tiempo positivo. Abre la cotización existente y coloca la suma de las operaciones marcadas en Tiempo parcial. Se identifica explícitamente como análisis parcial, incluso si el usuario marca todo. Mantiene el precio comercial y el tiempo total de la NP como base de las fórmulas del Excel; solo inicializa el tiempo considerado con la nueva suma. Precio por servicio, gastos y tiempo siguen siendo editables conforme al requerimiento anterior. El resumen de selección conserva la suma original aunque el usuario ajuste el tiempo manualmente.

Modificar selección vuelve a operaciones con la variante, sector y checks conservados en memoria. Cada combinación NP/tarifado/variante/sector mantiene su selección durante la sesión del componente. Las selecciones de distintos sectores no se suman entre sí: Siguiente utiliza únicamente el sector visible. Guardar registra el análisis parcial en el historial existente; no se ejecuta un guardado automático al pulsar Siguiente.

PartialSelection se guarda dentro de SnapshotJson: tarifado, variante, sector y nombre, tiempo total del sector y las operaciones elegidas (bloque, secuencia, código, variante de operación, descripción y tiempo estándar). UsedTime continúa almacenando el tiempo considerado de la cotización. El backend valida las operaciones, suma, duplicados y límites; calcula las fórmulas y conserva una copia del listado. El historial permite desplegar Ver operaciones seleccionadas y mantiene los resultados guardados. Los análisis anteriores, sin PartialSelection, conservan el comportamiento de completos/manuales. No requiere cambios SQL.

Validación: 287 pruebas backend aprobadas, incluidas selección parcial, serialización, copia independiente, ajuste manual, sector diferente, selección vacía/duplicada/negativa y validación MVC de records anidados. Navegador: marcar/desmarcar sector, bloque y fila; estados intermedios; vacío bloquea Siguiente; dos operaciones de 0.650 y 0.420 transfieren 1.070 min a Tiempo parcial y precio proporcional 0.131; Guardar incluye las dos operaciones; historial las muestra; regreso/cambio de sector conserva checks; móvil. Fórmulas completas/parciales previas también verificadas. Las pruebas de guardado fueron simuladas, sin insertar registros reales.

## Listado inferior de procesos seleccionados
El componente SelectedProcessesComponent muestra al pie de la cotización, tanto actual como histórica, el listado completo y visible de procesos marcados. Incluye bloque y nombre, secuencia, código, variante de operación, descripción y tiempo estándar, con total de los procesos seleccionados y cabecera de sector/tarifado/variante. Los completos sin selección no muestran este bloque.

Persistencia: el guardado parcial utiliza LG_ANALISIS_RB_HISTORIAL, mediante el mecanismo existente. SnapshotJson.partialSelection.operations contiene el detalle seleccionado, junto con el sector y variante del análisis. Al consultar un Id, el listado se reconstruye de ese snapshot; no se vuelve a consultar el tarifado vigente para obtener descripciones ni tiempos. No necesita otra tabla ni migración SQL. Los guardados anteriores que no contienen PartialSelection no pueden reconstruir una selección que nunca se registró.

Validación de navegador con API simulada: dos procesos de 0.650 y 0.420 aparecen debajo de la cotización con total 1.070; POST conserva el detalle; el historial reproduce esas dos filas y su descripción guardada, también debajo de la cotización. Tras editar la selección aparecen tres filas, sin desbordamiento de página en móvil. La consulta real de solo lectura de los últimos 30 registros no encontró análisis con selección parcial todavía; no se insertaron guardados de prueba reales. Backend sin cambios en esta entrega.

## Reinicio de checks al entrar a operaciones
Cada entrada a Operaciones del tarifado inicia con todas las filas y bloques marcados. La memoria de checks queda limitada a la instancia de esa pantalla: permite cambiar de sector y volver durante la misma visita, pero no restaura los checks de visitas anteriores. La selección ya enviada a la cotización y el detalle guardado en el historial permanecen independientes de ese reinicio.
Verificado en navegador: guardar un parcial de dos operaciones, verlo en el historial, regresar a Modificar selección y encontrar las 19 operaciones marcadas; volver a seleccionar tres operaciones y transferir 2.270 min correctamente. Compilación de producción correcta. Cambio exclusivo de frontend.

## Prenda nueva / recotización
Encima de la tabla principal de cotización se muestra Es Prenda Nueva / Recotización. Al marcar se muestran Precio Servicio (base) y Porcentaje. Precio por Servicio $ = base * porcentaje, directamente: 10 * 0.10 = 1; 10 * 0.07 = 0.7; no se divide entre 100. Cambiar cualquiera de los dos campos actualiza el precio final (6 decimales). Editar directamente Precio por Servicio $ recalcula porcentaje = precio final / base, con hasta 16 decimales para conservar precisión. Si la base es cero al editar el precio final, se deja el porcentaje vacío y se requiere completar una base válida; no se genera Infinity/NaN.

Al desmarcar, los dos campos se ocultan y sus valores se conservan, junto al precio final actual. El precio final vuelve a poder editarse de forma independiente; al volver a marcar se aplica el producto de los valores guardados en los campos. La opción y sus valores se conservan al navegar a operaciones o historial, y se reinician al consultar otra NP.

Requotation {Enabled,BaseServicePrice,Percentage} se guarda dentro de SnapshotJson de LG_ANALISIS_RB_HISTORIAL. El backend valida datos activos y coherencia del producto con el precio final; acepta redondeo a 6 decimales y una tolerancia de una millonésima. El historial muestra el checkbox deshabilitado y los valores de lectura si estaba marcado. Los guardados previos, sin Requotation, se muestran desmarcados. No se necesitan cambios SQL.

Validación: 299 pruebas backend aprobadas; producto directo, 0.10/0.07/0.80, división inversa periódica, estado desmarcado con valores conservados, campos faltantes/negativos/incoherentes, compatibilidad previa y combinación con selección parcial. Navegador verifica ambos sentidos, divisor cero, guardar marcado y desmarcado, histórico de lectura, ocultar/mostrar sin pérdida, retorno con borrador, reset por NP y móvil. Regresión del flujo parcial y listado inferior aprobada. Los guardados de navegador se probaron con API simulada, sin insertar registros de negocio reales.

## Presentación de recotización con dos decimales
Precio Servicio y Porcentaje se muestran con dos decimales al salir del campo y en el historial. El porcentaje recalculado se presenta redondeado (ejemplo 1.10 / 1.30 se ve 0.85); la precisión interna se conserva para no cambiar el precio final introducido ni alterar el guardado. La edición permite escribir normalmente y aplica el formato al salir del campo. Verificado en navegador: ejemplo 1.30/1.10, cálculo inverso periódico visible como 0.33, payload con precisión interna y valores históricos con dos decimales. Backend sin cambios.

## Guardar recotización únicamente si está marcada (regla vigente)
Al desmarcar Es Prenda Nueva / Recotización se limpian Precio Servicio base y Porcentaje, además de ocultarse. Al volver a marcar aparecen vacíos. El precio final de la columna Precio por Servicio $ se mantiene como dato independiente del análisis.

El payload guarda Enabled=false y ambos valores NULL cuando no está marcada. El backend también descarta valores enviados por un cliente antiguo o manipulados si Enabled=false. Si está marcada, conserva y valida los dos campos para su historial. Requotation continúa dentro de SnapshotJson en LG_ANALISIS_RB_HISTORIAL; la vista histórica muestra la opción y sus valores solo cuando estaban activos. No requiere migración ni modifica registros históricos previos.

Esta regla sustituye la conservación de valores ocultos descrita anteriormente. Verificación: 300 pruebas backend aprobadas; navegador confirma guardado marcado con valores, desmarcado con NULL, consulta de ambos históricos y campos vacíos al volver a marcar. Sin escrituras de prueba en datos de negocio reales.

## Encabezado Analíticas de Costura
Se reemplaza la cabecera anterior por Analíticas de Costura con estos datos, en el orden solicitado: NP; Cliente; Nombre Línea / Servicio; Tipo de Tela; Color; Fecha Inicio Costura; Fecha Fin Costura; Fecha Despacho; Fecha Actual. La imagen se conserva al lado con su ampliación. AnalysisHeaderComponent se comparte entre la consulta y el detalle histórico.

Fuentes: cliente, línea/servicio, despacho y descripción de tela conservan las consultas existentes. Tipo de Tela muestra ES_EstProVer.Des_Tela_Cliente. Color enumera presentaciones distintas de ES_OrdProColTal -> ES_EstProPre del estilo de la NP, ordenadas y separadas por /. Inicio y fin son fechas programadas de salida de costura: MIN(PL_TRABAJOS.Fec_Salida_Inicio) y MAX(PL_TRABAJOS.Fec_Salida_Fin) por NP, para cubrir su rango de programación. No se presentan como fechas reales de ejecución. Fecha Actual usa America/Bogota en la consulta y la fecha del snapshot en el historial.

El repositorio amplía la consulta existente con ColorName, SewingStartDate y SewingEndDate sin modificar PA_ES_ORDPRO_S0003 ni otros objetos SQL. El guardado incorpora esos campos dentro de Source en SnapshotJson; al ver un historial se usan exclusivamente sus valores guardados. Los históricos previos sin esos campos muestran Sin dato; no se completan con valores actuales para no cambiar su significado.

Validación real de solo lectura: J1907, LILLY PULITZER, GAMBOA ANGELES ELIZABETH MIRIAM, Jersey Slub Printed F/Cob(Pigmento)/ 36/1 AP-ProgJ, color 413594P Fresh Water, inicio 05/11/2026, fin 06/11/2026, despacho 09/11/2026. También verificados J1825 e I9337. 302 pruebas backend aprobadas. Navegador verifica los nueve campos, persistencia mediante API simulada, histórico independiente de datos actuales, campos ausentes, imagen, móvil y regresión de recotización. No requiere scripts SQL nuevos.

## Nombres españoles del almacenamiento
Se preparó 04_LG_ANALISIS_RB_HISTORIAL_Nombres_Espanol.sql para renombrar las 20 columnas con prefijos de tres letras y actualizar el CRUD. El esquema nuevo usa Num_Registro, Cod_Grupo_Historial, Num_Version, Num_Registro_Anterior, Tip_Accion, Cod_Solicitud, Cod_Fabrica, Cod_OrdPro, Num_Prendas_Programadas, Imp_Precio_Comercial, Min_Costura, Min_Considerados, Imp_Precio_Servicio, Imp_Otros_Gastos, Des_Ruta_Imagen, Txt_Datos_Analisis, Cod_Version_Reglas, Cod_Usuario, Fec_Registro_UTC y Cod_Traza.

La migración es transaccional, conserva las filas y el JSON, y actualiza nombres de restricciones/índices y expresiones CHECK. El backend detecta esquema anterior o nuevo y mapea los resultados españoles al contrato API existente; también adapta la lectura de imágenes históricas. Frontend y contenido de snapshots previos no cambian. Consultar docs/analisis-rb-nombres-espanol.md para equivalencias y orden de aplicación.

Validación: 304 pruebas backend aprobadas, incluidos mapeo Dapper de columnas españolas, tipos/nulls/UTC y contrato API inalterado. Tabla nueva, CRUD y migración validados con PARSEONLY en SQL Server 2008 R2, sin aplicar DDL. Lectura real del esquema anterior: cuatro registros de J1907 y sus detalles/imágenes recuperados correctamente por el backend actualizado. Prueba de navegador del análisis e historial aprobada. No se modificó la estructura ni los datos de HIALPESA durante el desarrollo.

## Diseño y exportación a PDF
El título Analíticas de Costura queda centrado en toda la cabecera, y sus nueve etiquetas usan negrita. Exportar a PDF está disponible tanto en el análisis actual como en el detalle histórico. La descarga se genera en el navegador con jsPDF y AutoTable, cargados de forma diferida. Captura una copia de los datos al pulsar el botón y crea un PDF A4 horizontal con texto y tablas vectoriales, datos de cabecera, cálculos, descripción de tela y listado de procesos seleccionados si corresponde.

En el PDF, Es Prenda Nueva / Recotización y los recuadros de Precio Servicio y Porcentaje solo aparecen si Requotation.Enabled=true. Se muestran con dos decimales, sin alterar los cálculos internos. Cuando está desmarcada no aparece ninguna parte de ese bloque. La imagen se obtiene del endpoint autenticado correspondiente (NP actual o Id histórico), se inserta sin el rótulo Imagen referencial ni el texto de ampliar, y ocupa hasta 55x50 mm frente a los 158x160 px de la miniatura web. Se preserva su proporción.

Los PDF históricos usan exclusivamente el snapshot y la ruta de imagen de su registro. Los listados largos repiten encabezados y evitan cortar filas entre páginas; hay numeración de páginas y NP en el pie. Nombre de descarga: Analiticas_Costura_NP.pdf o Analiticas_Costura_NP_Historial_Id.pdf. La opción no crea guardados automáticamente ni modifica BD.

Verificación: descarga real de cuatro PDF en navegador con API simulada (desmarcado, marcado, histórico y parcial con 200 procesos). PDFs abiertos y renderizados con pypdfium2, texto e imágenes inspeccionados con pdfplumber; análisis con/sin recotización y el histórico en una página, sin recortes; el listado largo contiene hasta el proceso 200 y su total, con pie y encabezados repetidos. Confirmadas imagen ampliada de 155.91 puntos de ancho, etiquetas en Helvetica-Bold y ausencia de Imagen referencial en los PDF. Las regresiones de cabecera e historial/recotización también pasaron. Solo frontend; no necesita scripts SQL ni cambios del backend.

## Generación automática por sector: completo o parcial
Buscar una NP muestra únicamente resultados y acceso a Operaciones del tarifado / Ver historial. La cotización no aparece hasta pulsar Siguiente en operaciones. Si todas las operaciones del sector visible están marcadas, el análisis es completo y no se guarda ni muestra un listado parcial; si se desmarca alguna, se genera parcial y se conserva su listado. La clasificación depende del número de operaciones, no de que la suma coincida accidentalmente con el total. Sin selección o con tiempo cero Siguiente continúa deshabilitado.

SectorAnalysis conserva tarifado, variante, código/nombre de sector, tiempo total del sector, número total y seleccionado de operaciones e indicador IsComplete. PartialSelection solo se guarda para parciales. Ambos se almacenan en el JSON del historial, sin nuevas columnas ni scripts SQL. El tiempo base de la analítica es el total del sector elegido; el tiempo considerado es todo ese sector para completos o la suma marcada para parciales. La cantidad, precio comercial e información original de la NP se conservan. El tiempo generado es de lectura y se cambia mediante Modificar selección, para mantener la coherencia con las operaciones.

El backend valida la coherencia de cantidades, modo, suma, sector y variante, calcula con el tiempo base del sector y etiqueta la observación según el sector (por ejemplo CORTE COMPLETO). La regla v3-sector queda registrada en los snapshots nuevos; los históricos anteriores siguen abriendo con su criterio original. El historial conserva la distinción y solo muestra procesos en parciales. El PDF usa el sector y su tiempo base y omite procesos en completos.

Validación: 313 pruebas backend aprobadas; navegador verifica búsqueda sin analítica, volver de operaciones sin Siguiente, completo de Corte con todas/no listado, parcial con ocho de nueve/listado, guardado y consulta de ambos modos, retorno con estado conservado, nueva NP sin análisis y móvil. PDF completo y parcial descargados, texto/condición de listado comprobados y páginas renderizadas. Las pruebas de guardado usaron API simulada; no se insertaron registros reales.
