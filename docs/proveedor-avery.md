# Acceso al proveedor Avery Dennison

## Uso publicado en IIS con agente Windows

El frontend publicado prepara la apertura con `POST /api/v1/proveedor/preparar-apertura`, autenticado y con el permiso administrativo de lectura. El backend valida la OC/PO y firma los datos del reporte. Devuelve un token aleatorio de 256 bits, de un solo uso, con vigencia de dos minutos. Los tickets pendientes se conservan en memoria con límite de 100 y desaparecen al reiniciar el proceso.

La confirmación de la intranet ofrece un enlace `hialpesa-vince://run` que Windows abre con el agente instalado. El agente admite solamente servidores exactos de agent-config.json y recupera por POST `/api/v1/proveedor/recuperar-llenado`, con token y cabecera X-Intranet-Agent. Esta recuperación permite acceso anónimo porque usa el token como autorización temporal; nunca devuelve datos sin token válido. No hay redirecciones ni credenciales SQL/Avery en el enlace. El token se consume atómicamente; reutilización, caducidad o reciclado del proceso devuelven 410.

El agente verifica la firma con public.pem y la vigencia de los datos antes de pedir confirmación de la OC. Abre Chrome bajo el usuario Windows interactivo con su acceso Avery cifrado por DPAPI CurrentUser. Conserva las validaciones, confirmaciones de color y cantidades del worker de desarrollo. El mensaje final es solamente: Cierra el navegador cuando termines. Los resultados se muestran localmente.

Instalación/actualización: agent/LEEME.md. Para habilitar el protocolo hay que ejecutar nuevamente Instalar agente.cmd en las PC que tenían la versión de archivos descargados. El paquete incluye Node/Playwright y requiere Chrome. La configuración fija la URL API autorizada; si cambia el dominio, puerto o HTTPS, actualizar agent-config.json. La clave privada permanece exclusivamente en ProviderAgent:SigningPrivateKey del backend; public.pem viaja con el agente. Mantener ambas entre publicaciones.

Pruebas: node agent/test.cjs; node agent/test-protocol.cjs; node agent/test-install.cjs; node agent/test-orchestration.cjs; node agent/test-api.cjs (contraseña de prueba por variable de entorno). En frontend, node tests/provider-agent-launch.cjs tras el build de producción. Se verifica enlace y confirmación sin descargas, firma, destinos autorizados, rechazo de PO ajeno, expiración y consumo único. Los tests de instalación y orquestación son simulados; no modifican el registro ni ingresan al proveedor. La prueba API usa datos reales sin abrir Avery. La ruta anterior preparar-llenado y la asociación de archivos se mantienen por compatibilidad.

La ruta `/iniciar-sesion` descrita a continuación conserva su modo de desarrollo y loopback; no se utiliza desde el frontend publicado.




El botón obtiene los PO únicos de la OC y permite elegir uno. Inicia sesión y navega a PRODUCT ORDERING > VINCE; completa PO Number, pulsa Search y abre el primer enlace Saved cuya fila coincide exactamente con el PO. No crea, guarda ni envía pedidos.



El enlace, usuario y contraseña se guardan cifrados con Windows DPAPI CurrentUser en %LOCALAPPDATA%\Hialpesa\Intranet\avery.protected. Solo son descifrables bajo la cuenta Windows que los configuró. No están en Angular, appsettings ni en el repositorio. Cambiar la cuenta del servicio requiere configurar su propio acceso cifrado. No copiar el archivo a otro equipo esperando que funcione.



El proceso de automatización es local: requiere Chrome y Node instalados, API Development, petición loopback y usuario Windows autorizado. No es todavía una solución multiusuario remota. Sesiones de navegador y claves de acceso son distintas: la sesión del portal puede caducar aunque la contraseña esté guardada. Al detectarlo se repite el acceso. Si el proceso anterior rechaza el llenado, se inicia de nuevo el navegador.



HIALPESA permanece con EnableOrderRegistration=false. El acceso a Avery no habilita escrituras SQL. No se modifica la regla de redondeo.



El flujo completa Quantity To Order en RFID (VINCEH501RFID) y UPC Stickers (VINH300), sobrescribiendo el valor anterior con Purchased de ReportRows (la fuente del Excel). No recalcula ni redondea. Cruza PO, estilo, talla, color y NP; vincula el color de RFID con la descripción del sticker por barcode, estilo y código de color. Los stickers sin PO explícito solo se usan cuando la relación de NP/estilo/color/talla identifica un único PO. Duplicados o coincidencias incompletas detienen el proceso antes del llenado. MSRP no se modifica. No pulsa Save Order, Apply ni Next.


El proceso no pulsa Save Order ni envia el pedido. Los eventos onchange del portal pueden persistir campos automaticamente; no debe asumirse que cerrar la ventana revierte el llenado. Los valores MSRP existentes se conservan.

Recuperacion de acceso: provider-auth.cjs espera la navegacion tras Sign In, PRODUCT ORDERING y VINCE. Reintenta hasta tres veces cuando el portal reinicia el formulario o devuelve al login; un formulario rechazado con usuario presente detiene los intentos. La API espera hasta 180 segundos y conserva el proceso ante un error recuperable para permitir un nuevo intento desde el boton. Pruebas: node tests/provider-auth.cjs. Verificacion real: dos accesos hasta Order Setup, sin abrir ni modificar pedidos.

Equivalencias confirmadas: tools/provider-color-aliases.json contiene BURN HENNA -> BURNT HENNA, confirmado por el usuario el 20/10/2026. No se usa similitud aproximada. Los errores de cruce se transmiten como detalle controlado (PO, tipo, estilo, color, codigo, talla, valores de Gestion y accion sugerida) y aparecen en el aviso rojo de la grilla. La prevalidacion completa se mantiene antes de escribir cantidades.

Regla decimal de calculo: si la parte entera termina en 0 o 5 y la fraccion es menor que 0.50, se conserva esa parte entera. Desde 0.50 sube al siguiente multiplo de 5. Las demas terminaciones suben al siguiente multiplo de 5. Ejemplos: 130.49 -> 130; 130.50 -> 135; 155.40 -> 155; 155.50 -> 160. Se aplica en la previsualizacion y en nuevos detalles registrados desde la intranet. El proveedor y el Excel de OC siguen usando Purchased registrado, sin recalcular OC existentes.

Confirmacion de similitudes: si el nombre completo del proveedor aparece como frase dentro de un unico color de Gestion para el mismo estilo, se propone una equivalencia. No se usa coincidencia aproximada de letras. Toda la prevalidacion (incluidas talla, NP, articulo y relacion PO) se ejecuta antes de devolver confirmation-required; no se llena ningun campo. El usuario ve ambas descripciones, estilo y codigo de color y puede elegir Si o No. Si reenvia las equivalencias unicamente para esa operacion, el worker las compara de nuevo con candidatos obtenidos de los datos actuales. No o Escape no hacen una segunda solicitud. No se persisten estas aprobaciones ni se modifica la base de datos. Pruebas UI aisladas: node tests/provider-color-confirmation.cjs.


