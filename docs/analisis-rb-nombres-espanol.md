# Historial Análisis-Rb: nombres en español

La tabla conserva su nombre `LG_ANALISIS_RB_HISTORIAL`.
Prefijos: Cod código, Num número/cantidad, Tip tipo, Imp importe, Min minutos, Des descripción/ruta, Txt contenido de texto, Fec fecha.

| Antes | Después |
|---|---|
| `Id` | `Num_Registro` |
| `RootId` | `Cod_Grupo_Historial` |
| `Revision` | `Num_Version` |
| `PreviousId` | `Num_Registro_Anterior` |
| `Action` | `Tip_Accion` |
| `RequestId` | `Cod_Solicitud` |
| `Factory` | `Cod_Fabrica` |
| `Np` | `Cod_OrdPro` |
| `ProgrammedQuantity` | `Num_Prendas_Programadas` |
| `CommercialPrice` | `Imp_Precio_Comercial` |
| `SewingTime` | `Min_Costura` |
| `UsedTime` | `Min_Considerados` |
| `ServicePrice` | `Imp_Precio_Servicio` |
| `OtherExpenses` | `Imp_Otros_Gastos` |
| `ImagePath` | `Des_Ruta_Imagen` |
| `SnapshotJson` | `Txt_Datos_Analisis` |
| `RuleVersion` | `Cod_Version_Reglas` |
| `GestionUser` | `Cod_Usuario` |
| `SavedAtUtc` | `Fec_Registro_UTC` |
| `TraceId` | `Cod_Traza` |

## Aplicación

- Instalación existente: publicar el Back nuevo y ejecutar únicamente `04_LG_ANALISIS_RB_HISTORIAL_Nombres_Espanol.sql` en la base utilizada por la API. El backend admite el esquema anterior y el nuevo durante la transición.
- Instalación nueva: ejecutar `01_LG_ANALISIS_RB_HISTORIAL_Tabla.sql` y `02_LG_ANALISIS_RB_HISTORIAL_CRUD.sql`.
- No volver a aplicar el script antiguo 03 después del 04: queda como referencia de la migración previa.

El 04 renombra columnas e índices/restricciones y sustituye los cuatro procedimientos en una transacción. Comprueba cantidad de registros y longitud total del JSON antes y después; revierte ante errores. No borra ni reinserta registros y no cambia identidad, GUID, fechas ni contenido de las copias guardadas. Repetir el 04 es seguro si el esquema coincide.

I0001 inserta una copia; U0001 añade una revisión; D0001 registra eliminación lógica conservando la copia; S0001 consulta por fábrica/NP y opcionalmente registro, con paginación. Parámetros y resultados SQL ahora usan los nombres españoles. `PA_..._I0001/U0001/D0001/S0001` conserva la convención existente.

El backend traduce las columnas de almacenamiento a los contratos públicos existentes. El frontend y el JSON de los históricos conservan sus nombres anteriores para poder abrir las copias ya guardadas, incluidos parciales, procesos, recotización e imagen. No hace falta modificar el frontend para un cambio de nombres físicos en SQL.

Los scripts están preparados para ejecución manual; no se aplicaron a HIALPESA durante el desarrollo.

Validación: 304 pruebas backend aprobadas; scripts 01, 02 y 04 revisados con PARSEONLY, sin aplicarlos. El backend compatible leyó los cuatro históricos reales de J1907 y sus imágenes. La variante de columnas españolas se verificó con mapeo Dapper y conservación del contrato API.


## Corrección de dependencias forzadas (2026-10-28)
La primera versión intentaba renombrar columnas antes de retirar sus CHECK. SQL Server 2008 bloqueó Revision por la dependencia de CK_LG_ANALISIS_RB_REVISION. La transacción fallida se revirtió: se comprobó que HIALPESA conserva las veinte columnas originales y los cuatro registros.

El script 04 actualizado retira los CHECK conocidos dentro de la transacción ANTES de renombrar, y después los reconstruye con los nombres nuevos y WITH CHECK. Las advertencias generales de sp_rename son informativas. Reabrir el archivo actualizado y ejecutarlo completo; no hace falta deshacer manualmente los dos primeros cambios del intento fallido.

Validación adicional real, aislada en tempdb y con ROLLBACK al terminar: crear esquema anterior, insertar snapshot ficticio de más de 9000 caracteres, ejecutar migración completa, comprobar veinte columnas y cinco CHECK activos y confiables, probar INSERT/reintento/UPDATE/DELETE lógico/SELECT paginado y repetir la migración. Todo aprobado, cuatro versiones ficticias conservadas y ningún objeto de prueba persistente. No se ejecutó la migración corregida sobre HIALPESA.
