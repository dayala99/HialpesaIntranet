# Notificaciones de envío PCP a Logística

1. Ejecutar 13_PCP_Notificaciones_Correo.sql en HIALPESA.
2. Publicar el Back compilado y reiniciar la API. No requiere Front nuevo.
3. Corregir el servicio Database Mail existente: los perfiles del servidor muestran errores de entrega. El perfil usado es GestionAutomaticos, con remitente emailsdelsistema@hialpesa.com. El destinatario temporal es dayalad@hialpesa.com.

Cada NUEVO envío guarda en LG_PCP_CORREO_ENVIO su propio aviso dentro de la misma transacción. La clave Num_Envio evita duplicar el aviso al reintentar la solicitud. No se generan avisos para envíos históricos al instalar. El correo contiene NP, Cliente, Cantidad programada, Cantidad requerida, Nombre Línea / Servicio y Costura Completa/Parcial, además de sector, usuario e identificador para seguimiento. Los datos son una instantánea del momento de enviar.

La API revisa la cola cada 30 segundos. La caída del servidor SMTP no bloquea el envío a Logística. Database Mail gestiona la entrega; que un aviso esté en cola no significa que ya se entregó. Solo se reintentan fallos confirmados, con cinco minutos de espera y hasta cinco intentos. No se reenvían elementos todavía pendientes, en reintento de Database Mail o entregados. SMTP no garantiza entrega exactamente una vez si el servidor acepta el mensaje y luego falla la confirmación.

Consulta de seguimiento (solo notificaciones propias):

```sql
EXEC dbo.PA_LG_PCP_CORREO_S0001;
-- O una sola:
EXEC dbo.PA_LG_PCP_CORREO_S0001 @Num_Envio=123;
```

Tip_Estado: P pendiente, Q registrado en Database Mail, F fallo final. Des_Estado_Entrega contiene sent / failed / unsent / retrying según Database Mail. Para confirmar entrega al servidor, revisar sent; la bandeja y políticas del destinatario quedan fuera de la API.

Tras corregir el servicio se puede reintentar un aviso fallido específico:

```sql
EXEC dbo.PA_LG_PCP_CORREO_U0002 @Num_Envio=123;
```

La identidad SQL del backend necesita acceso autorizado al perfil GestionAutomaticos y a los estados de msdb. No se crean usuarios, no se conceden roles automáticamente y no se cambian credenciales ni perfiles del servidor. Si faltan permisos quedan registrados en Txt_Error y en el log de la API.

PcpMail:Enabled=false permite detener el procesador; los avisos siguen registrados. En Demo no se inicia. No habilitarlo en instancias adicionales de pruebas conectadas a producción salvo que deban procesar la misma cola. El bloqueo transaccional evita que dos procesadores encolen simultáneamente el mismo aviso.

El script solo crea objetos propios PCP y actualiza nuestro PA_LG_PCP_ANALITICAS_I0001. No altera las tablas o SP originales de Gestión. Usa sintaxis compatible con SQL Server 2008 R2 / compatibilidad 80, conservando la corrección ARITHABORT. Si se vuelven a ejecutar scripts anteriores que redefinen I0001, ejecutar después el 13 para conservar la notificación.

Validación: pruebas del backend y prueba SQL en HIALPESA dentro de una transacción revertida, sustituyendo sp_send_dbmail por un simulador. No se enviaron correos reales de prueba. Se verificaron campos de J3224, destinatario, idempotencia y despacho único.
