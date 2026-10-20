# Importaciones desde OC

Ruta: /logistica/importaciones, o enlace Crear importación en la consulta de OC.
Alcance inicial: OC clase 01 y proveedor extranjero. Conserva año + correlativo existente. Precarga proveedor, anexo P validado por RUC y moneda. Origen, tipo de ítem y datos de embarque se revisan explícitamente.

API GET catalogos / GET ordenes/{serie}/{numero} solo consulta. POST requiere Windows autenticado, mapeo, EnableOrderRegistration y WriteWindowsUsers existentes; además exige que ORIGINAL_LOGIN coincida con Windows y sea sysadmin. Esta limitación se mantiene hasta mapear permisos específicos de importación para otros usuarios. No se suplanta a erondon.

POST reconsulta y bloquea la OC, rechaza importación existente, valida modo automático y anexo, bloquea correlativo anual, ejecuta UP_MAN_LG_IMPORTACIONES y UP_MAN_ORDCOMP_IMPORTACION con Dapper y parámetros nombrados en transacción exterior. Verifica vínculo antes de commit. Los SP mantienen sus efectos existentes de provisión y bitácora. No se alteran objetos SQL. Las liquidaciones parciales quedan fuera de este flujo inicial.

No hay reintentos automáticos. Tras error al guardar, consultar nuevamente antes de repetir. Para pruebas del agente se utilizan dobles/intercepciones; no se autorizó crear registros reales. Validación real de grabación, provisión y rollback pendiente en copia de pruebas de BD.
