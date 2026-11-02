# Detalle múltiple de órdenes de compra

Los combos de Grupo, NP e Ítem admiten búsqueda, checkboxes, selección de todos los resultados filtrados y limpieza de selección. Las NP se deduplican entre grupos. Los ítems se muestran como unión de los disponibles para las NP seleccionadas, conservando los pares válidos NP/ítem. Solo esos pares se consultan; cada requerimiento se incluye una vez por su clave.

Cambiar un filtro elimina la revisión anterior para evitar guardar datos de otra selección. La tabla de revisión incluye grupos asociados, NP, ítem, presentación, talla y cantidades. Cada ítem tiene un precio editable; si sus requerimientos traen precios diferentes, debe ingresarse explícitamente. Se conservan las cantidades requeridas y el redondeo por secuencia que genera Gestión.

POST /api/v1/logistica/avios/ordenes/{series}/{number}/detalles/lote recibe lines con productionOrder, itemCode, requirementKeys y unitPrice. El endpoint anterior de detalle simple se conserva y reutiliza la misma lógica.

El servidor exige hasta 500 requerimientos por lote, claves únicas y un precio consistente por ítem. Abre una transacción, bloquea la OC, revalida y bloquea todos los saldos en orden estable NP/ítem antes de insertar. Solo admite OC clase 01 Planeada y permisos de registro existentes. Agrupa los requerimientos por ítem, reutiliza UP_ACTUALIZA_REQ_OC, obtiene las nuevas secuencias y aplica sus cantidades de compra y el precio correspondiente. Una falla revierte toda la transacción. No se paralelizan guardados ni se generan confirmaciones parciales.

No requiere cambios de esquema, nuevas tablas ni nuevos scripts SQL. Todas las consultas y escrituras siguen usando procedimientos almacenados existentes. Publicar Back y Front juntos para disponer del endpoint nuevo.

Validación: pruebas de backend para distintas NP y distintos ítems, redondeo tras agrupación, precios independientes, claves duplicadas, cambio de saldo en la última NP, límite combinado y falla después del primer ítem. Pruebas de navegador con API simulada para selección múltiple, filtrado, deduplicación, pares válidos y un solo POST para el lote. No se insertaron detalles en una OC real durante las pruebas.
