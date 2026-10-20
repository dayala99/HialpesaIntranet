# Verificación de la primera entrega

Fecha: 16/10/2026.

## Resultados

- Compilación .NET 8 correcta.
- Pruebas de cálculo: redondeo de fracciones, múltiplos exactos, cero, reglas independientes, NPs distintas y rechazo de duplicados/múltiplos inválidos.
- Compilación Angular de producción correcta, con ExcelJS en una carga diferida.
- API `/health`, sesión y cálculo verificados en modo Demo.
- Repositorio Dapper verificado directamente contra HIALPESA103/HIALPESA con usuario de Gestión erondon y OC 002-349873: 298 ítems y un requerimiento en la primera secuencia. Se llamaron únicamente UP_SEL_ORDCOMP, UP_SEL_ORDCOMPITEM y UP_SEL_ORDCOMPITEMREQ. No se invocaron procedimientos de mantenimiento.
- Prueba de navegador con Chrome: consulta OC Demo, requerimientos, Excel de dos hojas, detección de hoja sin encabezados, selección de hoja válida, importación, previsualización por API y validación del contenido de la descarga para Calidad.
- Anchos 320, 390, 768, 1024 y 1440 sin desbordamiento horizontal de página. Las tablas densas tienen desplazamiento dentro de su contenedor. Menú móvil cerrado inicialmente y navegación probada.
- No se observaron errores JavaScript de página durante esa prueba.

## Límites y pendientes concretos

- El modo visual queda en Demo. La cuenta Windows programador04 no tiene una fila de clase 01 en LG_SEGORDCOMP, según la consulta realizada. No se crearon permisos ni se asoció esa cuenta automáticamente con erondon. Configurar la asociación autorizada en appsettings.Local.json para habilitar el acceso web a SQL.
- El ensayo Dapper es una verificación local del repositorio; no sustituye una prueba del despliegue IIS/Negotiate con la cuenta final.
- El controlador SQL 7.1.1 logró conectar, pero informó negociación TLS 1.0 con el servidor heredado. No se cambió la configuración de SQL ni del equipo; resolver este punto de infraestructura antes de producción.
- Angular 18 está fuera de soporte. npm audit reportó 65 hallazgos (7 bajos, 17 moderados, 39 altos, 2 críticos) en el árbol completo de dependencias. npm audit fix no los eliminó conservando las versiones mayores. El detalle está en frontend/npm-audit.json. No se aplicó fix --force ni se cambió Angular por otra versión. Requiere revisión/actualización antes de producción; el conteo no demuestra que todos los vectores sean explotables en esta implementación.
- Los archivos de salida son formatos generales. No se probó una importación en el portal del proveedor ni se definieron códigos de barras/arte impreso.
- No hay creación/modificación de OC, aprobación, guardado de reglas ni persistencia de matrices. Solo consultas SQL, cálculo y archivos de salida.

## Repetir la prueba de navegador

Con frontend y API iniciados:

```powershell
cd frontend
npm run test:smoke
```

La prueba utiliza Chrome o Edge instalado en las rutas habituales de Windows. También admite PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH. Si no hay navegador, ejecutar npx playwright install chromium. Las capturas y archivos de ejemplo se guardan en frontend/test-results y se excluyen de Git.

## Repetir la prueba de repositorio

Configurar ConnectionStrings__Gestion en el entorno y ejecutar:

```powershell
dotnet run --project backend/tools/Intranet.ConnectionCheck -- USUARIO_GESTION SERIE NUMERO_OC
```

La herramienta es local para desarrollo y no expone un endpoint que permita elegir identidades de Gestión desde el navegador.

Resultado final de unit tests: 10 aprobadas. La auditoría NuGet final de la API no reportó paquetes vulnerables tras actualizar System.Security.Cryptography.Pkcs a 8.0.2.


## Actualización: conexión real de programador04

Se verificó que HIALPESA\programador04 es sysadmin y dbo. Se activó el modo SqlServer con su propia identidad y una excepción administrativa de consulta validada también contra ORIGINAL_LOGIN y el rol SQL; se retiró la asociación temporal con erondon. La restricción LG_SEGORDCOMP de la cabecera se resuelve mediante SELECT parametrizado y no mediante cambios a SQL. Angular usa la API directamente en desarrollo, con CORS limitado al origen localhost:4200; esto evita el fallo de reutilización de conexión de Negotiate en el proxy de desarrollo. Prueba en Chrome con autenticación integrada automática: sesión real programador04, OC 002-349873 con 298 ítems y requerimientos de primera secuencia. Ninguna escritura en tablas de negocio. La observación anterior de modo Demo corresponde a la primera entrega y queda reemplazada por esta actualización.


Regla corregida tras aclaración del flujo OP/ítem: redondeo ascendente al siguiente múltiplo de 5, exactos conservados. API rechaza un múltiplo diferente de 5. Interfaz sin selector de múltiplo. 19 pruebas unitarias aprobadas; incluye 11, 16, 28, 10, 20, 15 y valores fraccionarios.


## Registro de OC implementado sin ejecutar escrituras reales

Guía vigente: docs/pruebas-registro-oc.md. El 16/10/2026 se implementó cabecera + grupo, detalle OP/ítem, ajuste a múltiplos de 5 con precio explícito e impresión. 34 pruebas unitarias aprobadas; registro de navegador con todas las APIs simuladas, cuatro anchos (320, 390, 768, 1440), PDF ficticio de una página verificado visualmente. Ningún procedimiento mutador se ejecutó contra HIALPESA. Las observaciones anteriores sobre ausencia de registro o estado de solo consulta corresponden a versiones previas y quedan reemplazadas por esta implementación; el guardado real lo probará el usuario.

