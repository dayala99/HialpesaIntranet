# Eventos Tintorería

Ruta: /aseguramiento-calidad/eventos-tintoreria.
Origen VB6: Planeamiento/FrmPartidaItemsEventos.frm y FrmAddPartidaItemsEventos.frm.

## Instalación pendiente
Crear manualmente backend/sql/PA_TI_CC_EVENTOS_S0001.sql en HIALPESA. No fue ejecutado por el agente.
El listado utiliza el SP original y puede consultarse sin este nuevo SP. Los catálogos y las operaciones de escritura requieren el nuevo SP.

## Capas
Api: QualityEventsController. Service: QualityEventService. Entity: contratos e interfaz IQualityEventRepository. Data: QualityEventRepository.
Angular: feature quality-assurance; listado DyeingEventsComponent y registro EventRegisterComponent separado.

## Comportamiento
Cuatro modos de consulta del SP original: fecha, partida, rango y por completar. El SP ignora el filtro de evento en modos 2 y 4, igual que Gestión.
Adicionar permite múltiples ítems y kilos individuales. Modificar y eliminar usan la clave partida, secuencia, evento y secuencia de evento.
El registro oculta el listado; Regresar/Cancelar descartan cambios. Impresión e Impresión Calidad están deshabilitadas.
Se requiere permiso de escritura y cabecera X-Intranet-Write. Las escrituras se ejecutan en una transacción serializable. El nuevo SP bloquea los registros y permite confirmar duplicados aprobados y detectar cambios concurrentes antes de modificar/eliminar.
Se reutiliza ti_up_man_ti_ordtra_tintoreria_items_cc sin alterar su definición. Envía Desviación/Causa también al insertar.

## Reglas existentes del SP
No permite insertar/modificar fechas anteriores al día del servidor. Valida ítems, eventos, estado, auditor, motivo y ubicación, y reglas productivas adicionales.
Por Completar devuelve rechazos sin motivo desde 01/07/2024.
La definición existente contiene una posible anomalía: valida Nro_Causa comparándolo con Nro_Desviacion. Se conserva el SP y sus errores se muestran al usuario; no se corrigió en la BD.

## Validación
142 pruebas backend aprobadas. frontend/tests/quality-events.cjs prueba filtros, insertar selección, modificar/cancelar, eliminación confirmada, impresión deshabilitada y ancho móvil con APIs simuladas.
Ocho consultas de catálogos validadas en modo lectura sobre HIALPESA. No se insertaron/modificaron/eliminaron eventos reales.

## Tela Por Acabar
Ruta: /aseguramiento-calidad/eventos-tintoreria-tela-por-acabar.
Origen: ControlCalidad/FrmPartidaItemsEventos_TelaAcabada.frm y FrmPartidaItemsEventos_AddTelaAcabada.frm.
Utiliza ti_muestra_TI_ORDTRA_TINTORERIA_ITEMS_CC_POR_ACABAR para leer y ti_up_man_ti_ordtra_tintoreria_items_cc_Por_Acabar para I/U/D. Comparte los catálogos, cliente e ítems del primer submenú. El contexto se determina por la ruta en frontend y por un controller independiente en backend, no por el cuerpo del request.
Crear manualmente backend/sql/PA_TI_ORDTRA_TINTORERIA_ITEMS_CC_POR_ACABAR_S0001.sql antes de guardar. Este SP solo lee/bloquea la tabla TI_ORDTRA_TINTORERIA_ITEMS_CC_POR_ACABAR y no modifica procedimientos de Gestión.
Se mantienen confirmación condicional de aprobados, versión para conflictos, selección global, guardado solo por botón y consulta de cliente con Enter. Impresión deshabilitada.
Verificación: 142 pruebas backend; frontend/tests/quality-events.cjs para primer menú y EVENTS_UNFINISHED=1 para segundo menú. APIs simuladas en operaciones de escritura. Consulta real de 23/10/2026 devuelve 3 filas, sin cambios en datos de negocio.
