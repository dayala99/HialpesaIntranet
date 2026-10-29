# Agente VINCE para Windows: apertura directa desde la intranet

## Publicación

Copia Compilados\Back al BackEnd de IIS y Compilados\Front al FrontEnd. Incluye appsettings.Local.json del backend y recicla su grupo de aplicaciones. No copies el backend ni su configuración al frontend.

## Instalación o actualización en cada PC

1. Extrae la carpeta AgenteVince completa a una ubicación permanente de la PC. No la ejecutes desde el ZIP. Google Chrome debe estar instalado. Node y Playwright están incluidos.
2. Cierra el navegador y las ventanas del agente anterior. Sustituye los archivos del agente por esta versión.
3. Ejecuta "Instalar agente.cmd" con tu usuario Windows. No requiere administrador. Debes ejecutarlo de nuevo al actualizar desde la versión de descarga: registra el protocolo hialpesa-vince://. Conserva las credenciales Avery cifradas existentes.
4. Si no tienes acceso Avery configurado, el instalador lo solicita con contraseña oculta. Para cambiarlo ejecuta "Configurar acceso.cmd". El acceso se cifra con Windows DPAPI para esa cuenta y PC.

## Uso

Busca la OC en la intranet publicada, pulsa la acción Avery/VINCE y selecciona el PO. Pulsa "Abrir agente VINCE" en la confirmación de la intranet y acepta la pregunta del navegador para abrir la aplicación. Ya no se descarga un archivo.

La autorización dura dos minutos y es de un solo uso. Si cancelas, vence, se reinicia el backend o falla la apertura, selecciona nuevamente el PO. Si Windows no reconoce el enlace, vuelve a ejecutar Instalar agente.cmd desde la carpeta donde conservarás el agente.

El agente verifica la firma de los datos y confirma el PO antes de abrir Chrome. Las similitudes de color requieren Sí/No en la PC. Solo sobreescribe Quantity To Order de RFID y UPC Stickers, con las cantidades compradas del reporte Excel; conserva MSRP y no cambia el redondeo. No pulsa Save Order ni envía el pedido. Los eventos del portal pueden persistir campos automáticamente.

El mensaje final es únicamente "Cierra el navegador cuando termines.". Cierra la ventana de Chrome del agente para liberar otro llenado de esa cuenta Windows. Los errores y el resultado se muestran en la PC; no se sincronizan con la grilla de esta primera versión.

## Configuración

agent-config.json contiene los servidores autorizados. La configuración distribuida incluye http://192.168.70.109:8109/BackEnd/api/v1 y http://localhost:5080/api/v1. Si publicas con otro nombre o HTTPS, TI debe actualizar ese archivo con la URL API exacta antes de distribuirlo. No se permiten redirecciones de recuperación.

La clave privada de firma se conserva en ProviderAgent:SigningPrivateKey del appsettings.Local.json del backend. Nunca debe copiarse al agente o frontend. Cada agente contiene solo public.pem. Conserva la clave entre publicaciones; cambiarla requiere actualizar la clave pública de los agentes.

Las credenciales Avery no viajan en el enlace ni en los datos del llenado. El enlace contiene un token temporal de un solo uso. Las capturas de diagnóstico se guardan en %LOCALAPPDATA%\Hialpesa\Intranet\VinceAgent\test-results.

La apertura de archivos .hialpesa-vince anteriores se mantiene por compatibilidad si no han vencido. Si las políticas de la empresa bloquean protocolos externos o automatización de Chrome, TI debe autorizar su uso.

Desinstalación: TI puede quitar HKCU\Software\Classes\hialpesa-vince, HKCU\Software\Classes\.hialpesa-vince y HKCU\Software\Classes\Hialpesa.Vince.Job, y luego eliminar la carpeta. Las credenciales cifradas quedan separadas para no afectar el uso local previo.

Con OC, al terminar las cantidades pulsa Next y completa Customer Reference (serie-número), Courier, DHL (COURIER) e instrucciones 20418108151. Sin OC permanece en las cantidades y muestra "PO sin Orden de Compra, Proceso Terminado". Nunca pulsa Save Order.

Con OC: después de completar Shipping and Billing, pulsa Next a Preview, despliega las tablas RFID y UPC y genera un Excel en Descargas/Reportes VINCE. Resumen: NP, PO, ESTILO, COD. COLOR, COLOR y PRECIO (MSRP). Incluye Detalle Preview por talla. El agente no pulsa Submit Order ni Save Order.

## Prioridad de resultados

Si la misma PO tiene Saved y Available For Callout, el agente prioriza un único Available For Callout y continúa con Create Order. Si no hay Available For Callout, utiliza un único Saved. Varios Available For Callout detienen el proceso para revisar la grilla.

El paquete actualizado se prepara en Compilados\Agente.

## Selección de varios Saved (28/10/2026)

Available For Callout conserva la prioridad. Cuando no existe Callout y hay varios Saved de la PO exacta, se compara la columna EDI_LOAD_DATE por día y se abre el Saved de fecha mayor. Si varios Saved tienen la misma fecha, se abre el último de ellos en el orden de la grilla. Cancelled y otras PO no participan. Si no se puede leer alguna fecha entre varios Saved, el agente detiene el proceso antes de hacer clic para evitar abrir un borrador incorrecto. Varios Available For Callout siguen requiriendo revisión.

## Retiro de SKU en cero (28/10/2026)
Después de completar y verificar cantidades, el agente revisa RFID y UPC Stickers. Selecciona únicamente las filas con Quantity To Order igual a cero y pulsa Remove SKU en cada pestaña que lo requiera. Antes de hacerlo limpia selecciones previas de filas. Comprueba que las filas restantes, sus cantidades y MSRP se conserven; si no se confirma la eliminación, detiene el flujo. Sin filas cero continúa sin pulsar Remove SKU. Esta limpieza se ejecuta en el llenado con OC y en po-required, antes de Shipping o del aviso final sin OC. El modo preserve-provider conserva su comportamiento de no modificar el pedido.
