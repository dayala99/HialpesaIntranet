# Secuenciador optimizado

1. Ejecutar `12_PCP_Secuenciador_Optimizado.sql` en HIALPESA.
2. Actualizar Back y Front con los compilados y reiniciar el backend para limpiar la caché.

Crea o actualiza únicamente `dbo.PA_LG_PCP_SECUENCIADOR_S0001`, procedimiento propio de consulta. No modifica tablas, datos, procedimientos ni funciones originales de Gestión. Compatible con SQL Server 2008 R2 y nivel 80.

El backend deja de ejecutar `PMP_POSICION_COSTURA_LINEA_OP` y la segunda consulta de cabeceras; llama una sola vez al nuevo procedimiento. Conserva orden vigente, trabajo con secuencia no cero, exclusión de cierre de costura y las uniones de orden, cliente y línea del detalle original. El tipo de línea R no filtra la opción 2 del reporte original, por eso no se agrega ese filtro. Los estados se calculan con las mismas funciones originales, una vez por NP.

Se consultan precio comercial y cabeceras visibles. No se calculan tiempos, imágenes, stocks, cuotas, avances ni subtotales de impresión. El detalle de procesos y el tiempo para enviar a Logística siguen consultándose al abrir cada NP. La exclusión de enviadas/finalizadas sigue vigente.

Comparación de solo lectura el 30/10/2026: original 51,18 s, optimizado 3,19 s; mismos 862 códigos NP, sin faltantes/adicionales, mismos estados de tarifado y ficha técnica. Son mediciones de esta ejecución, no una garantía de tiempo fijo. Se probaron los lotes sin instalar cambios persistentes. El script debe ejecutarlo el administrador antes de usar el nuevo backend.
