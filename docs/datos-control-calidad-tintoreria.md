# Datos Control Calidad — Tintorería

Origen: `Gestion/Tintoreria/Tintoreria/frmShowTI_OrdTra.frm`, acción CONTROLCALIDAD. El formulario activo es `frmDetDatosTecnicos.frm`; el antiguo `frmDatCrtCalidad` está comentado y no se utilizó como referencia.

## Angular

- `features/dyeing/components/dialogs/quality-control-dialog`: formulario independiente, HTML, SCSS y TypeScript.
- `features/dyeing/components/dialogs/quality-lookup-dialog`: selección de tela alternativa y consulta de consumo por orden de corte, con datos de entrada y resultado tipados.
- `features/dyeing/models/quality-control.models.ts`: contratos y definición de mediciones.
- `features/dyeing/services/quality-control-api.service.ts`: llamadas HTTP.

Se abre desde una fila seleccionada por OT/secuencia. Incluye datos programados de solo lectura, acabado final, tercera lavada, rama, residuales, adicionales, estado de aprobación, kilos y motivo de rechazo, observaciones, tela alternativa y recomendaciones. Copiar rama actualiza gramaje, ancho, revirado, gramaje lavado, tipo y encogimientos del acabado final como el VB6.

Los motivos de rechazo se eligen del catálogo existente. Esta entrega no incorpora el mantenimiento independiente de altas/bajas/modificaciones de ese catálogo. La consulta Partidas muestra consumo por orden de corte, no el listado principal de OTs.

## Backend por capas

Api: `QualityControlController`, con rutas de lectura y PUT por OT/secuencia, consulta de telas y consumo.
Entity: contratos tipados y `IQualityControlRepository`.
Service: `QualityControlService`, permisos de la aplicación, validación de rangos y precisión y coordinación.
Data: `QualityControlRepository`, lectura mediante SP y guardado con los procedimientos existentes.

Se reutilizan sin alterar:

- `TI_UP_ACT_DATOSTECNICOS_TINTO_ITEM`: sus 41 parámetros se envían por nombre. Se preserva la grafía original `COD_TELA_RELACIONDA`.
- `TI_RECOMENDACIONES_CONTROL_CALIDAD_select`.
- `UP_MAN_TI_ORDTRA_TINTORERIA_ITEMS_RECOMENDACIONES`.
- `ti_muestra_tendidos_donde_se_uso_partida`.

El guardado requiere identidad autorizada y cabecera X-Intranet-Write. El SP original conserva su control de acceso a datos técnicos y sus reglas. La lectura calcula si el usuario puede editar según la misma tabla y condiciones del SP, combinadas con el permiso de escritura de la aplicación. El estado se vuelve a leer con bloqueo dentro de una transacción; si cambió FEC_ULTMODDATTEC desde la apertura, se pide reabrir. Datos técnicos y recomendaciones se guardan en la misma transacción. La tela, combinación y color del detalle se toman del servidor, no de campos editables del navegador.

## SQL pendiente de creación

`backend/sql/PA_TI_ORDTRA_TINTORERIA_ITEMS_S0002.sql` reúne las consultas adicionales del formulario: detalle, catálogos de ancho/encogimiento/rechazo y búsqueda de telas. Es compatible con SQL Server 2008 R2 y fue validado localmente con ScriptDom TSql100Parser. No usa OFFSET/FETCH ni altera los SP existentes. La secuencia S0002 estaba libre al inspeccionar metadatos.

El archivo NO fue ejecutado ni instalado por el agente. Hace falta crearlo en HIALPESA para habilitar la carga real del formulario. Si falta, la API y el diálogo muestran un mensaje explícito con su nombre.

## Validación realizada

- Lectura de código VB6, definiciones SQL y parámetros del SP original.
- Compilación Angular de producción y backend .NET.
- 128 pruebas de backend, incluidas validación de aprobación, precisión, recomendaciones, parámetros SQL y bloqueo de escritura sin permisos/cabecera.
- `frontend/tests/quality-control.cjs`: carga, cancelar sin PUT, copiar rama, recomendaciones, tela alternativa, consumo, conflicto de actualización, guardado simulado, modo consulta y móvil.
- `frontend/tests/dyeing-partidas.cjs`: regresión de los siete filtros.

Las pruebas de guardado de interfaz usan API simulada. No se ejecutó el SP de actualización sobre datos reales de HIALPESA durante el desarrollo.

## Habilitación y selección automática

El usuario creó S0002. Su definición fue contrastada con el archivo revisado y se cargó la OT Y2035, secuencia 1, con 11 opciones de catálogo y cuatro recomendaciones mediante QualityControlRepository. La prueba fue de solo lectura. La grilla selecciona automáticamente el primer detalle al buscar, permite cambiar la selección y conserva la OT/secuencia seleccionada al refrescar si sigue presente, para que Datos Control Calidad no requiera un clic inicial adicional.

## Presentación Real / Programado

El formulario conserva ahora columnas alineadas Medición, Programado, Real y 3ra. lavada. Encogimientos agrupa ancho, largo y tipo en una sección propia con esa misma comparación. Los encogimientos de rama y residuales se muestran en grupos identificados. En pantallas pequeñas la comparación conserva las columnas con desplazamiento horizontal. El botón de copia se denomina Copiar valores de rama a Real. Los contratos HTTP, campos persistidos y procedimientos de guardado no cambian.

## Usuario global del aplicativo

GestionIdentity.IsGlobalUser reconoce programador04 desde el código de Gestión de una sesión autenticada, o desde una asignación Windows explícita. El nombre visible o una identidad sin autenticar no conceden privilegios. La excepción central habilita la lectura general donde el contrato de Data ya admite ReadAll, y la escritura del aplicativo sin listas de usuarios por módulo, conservando el interruptor general de escritura y las cabeceras exigidas.

Control Calidad permite editar el formulario al usuario global aunque el indicador de Gestión sea de solo lectura. El guardado mantiene la identidad original, los controles de Data y los procedimientos existentes: no se concede acceso SQL, no se cambian tablas de permisos ni se suplanta un usuario autorizado. Las reglas y autorizaciones internas de Gestión continúan aplicándose. El usuario indicó que no usará esos botones de SP de Gestión con programador04.

Verificación: 136 pruebas del backend, incluyendo sesión global válida, rechazo de identidad sin autenticar, nombres visibles sin claim, usuario normal y permisos de calidad. Lectura real de Y2035/2 confirma el formulario editable para la identidad global; no se hicieron escrituras de prueba.
