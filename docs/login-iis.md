# Login de la intranet en IIS

La API .NET 10 usa autenticacion por cookie. Ya no registra Negotiate ni solicita credenciales Windows al navegador.

## Publicacion

- Copiar Compilados\Back a D:\Apps\BackEnd.
- Copiar Compilados\Front a D:\Apps\FrontEnd.
- BackEnd debe ser aplicacion IIS. Mantener Hosting Bundle .NET 10.
- En BackEnd: autenticacion anonima habilitada; autenticacion Windows deshabilitada. La API rechaza peticiones sin cookie mediante 401; anonimo en IIS no otorga acceso a las operaciones de la API.
- En FrontEnd: autenticacion anonima habilitada, Windows deshabilitada.
- El web.config del frontend incluye fallback de rutas Angular y requiere el modulo IIS URL Rewrite.
- Abrir http://192.168.70.109:8109/FrontEnd/ y recargar con Ctrl+F5. En produccion con acceso externo configurar HTTPS.

## Cuenta

ApplicationLogin en appsettings.Local.json contiene Username, PermissionIdentity y PasswordHash. La contrasena es obligatoria y se verifica mediante PBKDF2-SHA256 (210000 iteraciones y salt aleatorio). No se devuelve al frontend ni se almacena en texto claro. El usuario configurado es programador04; su identidad de permisos se asigna exclusivamente en el servidor.

La cookie es HttpOnly, SameSite=Lax y Secure cuando la peticion es HTTPS; dura hasta ocho horas y se elimina al cerrar sesion. La sesion no se guarda en localStorage. El login limita a cinco intentos por minuto y direccion IP.

## SQL Server y proveedor

El login no cambia la identidad del proceso de IIS que conecta con SQL Server. La cuenta del grupo de aplicaciones sigue necesitando los permisos SQL correspondientes. No se modificaron tablas, procedimientos ni permisos de la BD. EnableOrderRegistration permanece false en HIALPESA.

La automatizacion local del proveedor mantiene sus requisitos y restricciones previos; no se adapto a ejecucion remota desde IIS.

## Verificacion

64 pruebas .NET correctas. Prueba real de login y consultas de solo lectura: node tests/application-login.cjs con INTRANET_TEST_PASSWORD configurada solo en el proceso de prueba. Comprueba 401 sin WWW-Authenticate, rechazo de contrasenas invalidas, cookie HttpOnly, acceso a OC, logout y guardas de rutas. Capturas de escritorio y movil en frontend/test-results/login-*.png.