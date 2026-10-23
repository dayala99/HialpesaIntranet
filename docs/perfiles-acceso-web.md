# Perfiles y accesos en la intranet

El usuario autenticado procede exclusivamente de la cookie de sesión del servidor (gestion_user) o del mapeo Windows explícito existente. El navegador no envía el usuario ni el perfil para decidir autorizaciones.

## Lectura de SEGURIDAD
Se reutilizan sin modificaciones:
- SEGURIDAD.dbo.Sg_Muestra_PerfilesUsuarios: identifica perfiles del usuario, filtrando empresa 01 (HIALPESA).
- SEGURIDAD.dbo.SM_MUESTRA_OPCIONES_PERFIL_EMPRESA_multiperfil: reúne accesos de todos los perfiles asignados al usuario y empresa. Se envía Cod_Usuario explícitamente y Cod_Perfil=NULL; no se usa la identidad SQL compartida.
El SP multiperfil contiene el filtro ISNULL(flg_activo,0)=0 vigente de Gestión. Se conserva su semántica y no se cambian flags ni perfiles en BD.
No se usa el SP de opciones web porque aplica flags diferentes y no replica el menú Windows. Tampoco se usa el antiguo generador de árbol: en la comprobación de un perfil existente omitió opciones asignadas.
No se creó ni ejecutó DDL. Las lecturas solo crean temporales internas de los SP originales.

## Correspondencia de opciones
| Web | Aplicación/opción Gestión |
|---|---|
| Nueva orden de compra | 07 / mnuOrdComp |
| Consulta OC y PO | 07 / mnuOrdComp o 29 / frmrptgest |
| Importaciones | 07 / mOImportac |
| Generación Partidas - Tintoreria y Datos Control Calidad | 15 / mnuPartOT |
| Eventos Tintorería | 25 / mCCTinto |
| Eventos Tintorería - Tela Por Acabar | 25 / mEvTintoTe |
Solo se exponen formularios actualmente implementados en la web. Otras opciones de Gestión no generan páginas automáticamente. No se modifican reglas de negocio, permisos de botones ya existentes ni limitaciones de escritura de los SP.

## Protección
SessionController entrega isGlobalUser, allowedOptions y profiles en login y restauración de sesión. No se confía en nombres de presentación.
Angular filtra barra lateral, grupos y tarjetas de inicio; el guard consulta la sesión al entrar en una ruta y en rutas hijas de OC. Sin permiso muestra /sin-acceso.
PortalAccessFilter valida cada solicitud autenticada de los controladores funcionales antes de ejecutar su acción. Crear/modificar OC requiere orders.create; lectura común acepta orders.create u orders.query. Los endpoints anónimos de canje del agente mantienen la validación de ticket existente y no se interceptan con este filtro.
Los permisos se obtienen por solicitud y se reutilizan únicamente dentro de ella; no quedan almacenados en la cookie ni en una caché persistente. Si SEGURIDAD falla, no se concede acceso como alternativa.

## Excepción explícita
programador04 autenticado conserva acceso global a todas las opciones implementadas sin consultar perfiles. No se activa por displayName ni por un nombre que el navegador suministre. Se mantienen el interruptor general de escritura y la autenticación actuales.

## Comprobaciones
Pruebas backend: identidad global, usuario sin perfiles, varias opciones, empresa/aplicación exactas, restricciones de API y controladores cubiertos.
frontend/tests/profile-access.cjs: menú, grupos, inicio, URL directa, usuario sin permisos y global con APIs simuladas.
Prueba real de repositorio en modo lectura: usuario inexistente obtiene 0 perfiles/0 accesos; usuario con perfil asignado devuelve sus accesos y Eventos Tintorería. No se muestran datos personales ni se cambian asignaciones.
