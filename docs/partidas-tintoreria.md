# Partidas de Tintorería: filtro y grilla

La entrada sigue en `/tintoreria/generacion-partidas`. Corresponde a `frmShowTI_OrdTra` (Partidas [mnuPartOT]), no a la pantalla de grupos/colores de Tintoreria2.

Se implementaron los cinco filtros y los tres submodos de cliente. La grilla toma las columnas del resultado correspondiente, conserva la selección por partida/secuencia y muestra páginas de 50 filas. El rango de fibra corresponde al primer envío; el rango de cliente corresponde a creación. Se mantienen los códigos de partida alfanuméricos.

Api: `GET /api/v1/tintoreria/partidas/catalogos` y `GET /api/v1/tintoreria/partidas`. Service valida el modo, los campos requeridos y las fechas. Data utiliza exclusivamente CommandType.StoredProcedure para este módulo. El usuario se obtiene de la sesión en el servidor.

## SQL pendiente de revisión y creación

Archivo: `backend/sql/PA_TI_ORDTRA_TINTORERIA_ITEMS_S0001.sql`. La secuencia S0001 estaba libre al consultar metadatos el 22/10/2026. Es un CREATE PROCEDURE independiente, sin ALTER del SP de Gestión. Este cambio NO lo ejecuta ni lo instala automáticamente.

El SP propuesto conserva las proyecciones por filtro de `ti_sm_muestra_partidas_principal`, elimina nombres repetidos dentro de una misma proyección, valida entradas, aplica permisos de TI_SEG_PARTIDAS_USUARIO_CLIENTE a todos los modos, y cuenta/pagina el resultado final con orden por partida descendente y secuencia. Usa tablas temporales para el resultado de cada variante. No escribe tablas de negocio.

La opción 0 devuelve catálogos. Las opciones 1 a 7 devuelven dos resultsets: total de filas y filas de la página. El backend ya está preparado para este contrato. Si el SP no existe, responde con un mensaje explícito de habilitación pendiente. No utiliza automáticamente el procedimiento anterior como reemplazo.

Antes de habilitar consultas reales, revisar y crear el SP en HIALPESA y comprobar resultados, permisos y rendimiento con datos representativos. El esquema de columnas variable se conserva; no se han modificado las cardinalidades de los joins originales. La paginación limita la respuesta HTTP, pero la consulta aún calcula el resultado filtrado completo para obtener el total.

## Verificación

- Compilación Angular de producción y compilación .NET.
- DyeingFilterTests: campos por modo, rango inválido, rango del mismo día y códigos alfanuméricos.
- `node frontend/tests/dyeing-partidas.cjs`: siete variantes con API simulada, selección, columnas variables, fechas, paginación, móvil y mensaje de SP pendiente.
- Sintaxis SQL validada con ScriptDom localmente, sin ejecutar CREATE ni consultas de negocio.

Esta etapa no incluye acciones de creación/modificación de partidas, transferencia o impresión del cliente VB6.

## Compatibilidad SQL Server
Servidor verificado: SQL Server 2008 R2 (10.50.6000.34), base con compatibilidad 80. La paginación del archivo usa ROW_NUMBER y CTE, sin OFFSET/FETCH. Se verificó esta construcción en el servidor con cuatro filas literales ficticias, sin leer tablas de negocio ni ejecutar el procedimiento. Las columnas auxiliares de paginación no se devuelven a la grilla.

## Verificación posterior al registro
El usuario registró el SP. Se confirmó en HIALPESA que su definición coincide con el archivo corregido y que expone los 17 parámetros esperados. Se invocó mediante DyeingRepository: catálogos devuelven una fábrica, 37 fibras y seis estados; el filtro por número devuelve correctamente total y grilla. La identidad local utilizada no tenía clientes habilitados en el catálogo, por lo que esa prueba retornó cero filas y no acredita resultados con una cuenta de negocio con permisos. No se modificaron datos de negocio.

## Ajuste de búsqueda por OT (pendiente de aplicar)

La OT Y2035 tiene dos detalles en HIALPESA. La restricción de clientes que se había añadido globalmente al SP nuevo los eliminaba para la identidad local sin asignaciones. Gestión aplica su consulta por número sin esa restricción global. El borrador se corrigió para conservar el alcance de Gestión en número OT, estado, N/P y fibra; los filtros de cliente 1, 2 y 3 mantienen su validación de cliente permitido. La base no distingue mayúsculas/minúsculas (Modern_Spanish_CI_AI), por lo que y2035/Y2035 no era la causa.

Archivo para actualizar el procedimiento existente: backend/sql/PA_TI_ORDTRA_TINTORERIA_ITEMS_S0001_Actualizar.sql (ALTER PROCEDURE). Se preparó y validó localmente; NO se aplicó en HIALPESA. El archivo CREATE también refleja esta revisión. El frontend ahora etiqueta el campo Número de OT y usa Y2035 como ejemplo.
