# Login de Gestión en IIS

El inicio de sesión usa la conexión Gestion existente, a HIALPESA, y consulta SEGURIDAD.dbo.SEG_Usuarios mediante el nombre completo entre bases. No necesita otra cadena de conexión ni cambios en SQL.

Usuarios normales: Cod_Usuario identifica al usuario y Password valida su contraseña. La consulta es parametrizada, exige coincidencia exacta de contraseña (incluidas mayúsculas y espacios), no acepta contraseñas vacías y rechaza is_active=0. Los registros heredados con is_active NULL se consideran habilitados. Nom_Usuario se muestra en la cabecera. No se envían contraseñas ni hashes al frontend y no se actualizan last_login ni otros campos de SEGURIDAD.

Excepción fija: programador04 entra con contraseña programador04 aunque no exista en SEGURIDAD o SQL no esté disponible. La contraseña de la excepción se comprueba con un hash PBKDF2 incluido en el backend. No se intenta autenticar esa cuenta contra la tabla, ni siquiera cuando la contraseña es incorrecta. Su identidad administrativa sigue siendo HIALPESA\programador04 y mantiene la visibilidad completa ya configurada.

Los usuarios de Gestión llevan su código validado en la sesión cifrada y usan los permisos existentes de LG_SEGORDCOMP. La escritura de las operaciones implementadas está habilitada para todos los usuarios autenticados de Gestión mediante Gestion.EnableOrderRegistration=true y Gestion.AllowAllAuthenticatedWrites=true. Se conservan las validaciones de estado de OC, duplicados, saldos y transacciones. El listado mantiene sus permisos existentes. VINCE está disponible para cualquier usuario autenticado de Gestión sobre sus OC accesibles; no requiere la lista administrativa ReadAllWindowsUsers. Se conservan la validación de la OC, el PO y sus datos.

La sesión utiliza cookie HttpOnly; IIS debe permitir autenticación anónima y tener Windows Authentication deshabilitada tanto para FrontEnd como BackEnd. La autorización real la realiza la aplicación. Mantener la configuración y las claves del backend fuera de FrontEnd. Las peticiones de login mantienen el límite de intentos vigente.

Publicación: copiar Compilados\Back a BackEnd y Compilados\Front a FrontEnd; conservar appsettings.Local.json con su conexión y firma del agente; reciclar el grupo de aplicaciones y recargar la página con Ctrl+F5. No es necesario actualizar el agente VINCE para este cambio de login.

Validación: 82 pruebas backend, flujo de sesión en navegador (credenciales incorrectas, administrador, listado, recarga y logout), rechazo con 401 al consultar SEGURIDAD con contraseña incorrecta, e inicio de sesión 200 de programador04 con SQL deliberadamente inaccesible en una instancia temporal. Los casos válidos de usuarios normales se prueban con un repositorio simulado; la prueba de un usuario real puede hacerse con sus credenciales desde la pantalla, sin compartir su contraseña.


