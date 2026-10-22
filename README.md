# Hialpesa · Intranet

Intranet modular para diferentes áreas, clientes y proveedores. Primer módulo: **Logística / Compras de avíos**.

## Tecnologías

- Angular 18 con TypeScript, SCSS, NgModule y módulos diferidos; interfaz adaptable a móviles.
- API ASP.NET Core 8, Dapper, Microsoft.Data.SqlClient y autenticación Windows.
- SQL Server existente: no hay migraciones ni modificaciones de tablas, funciones o procedimientos.

## Funciones actuales

- Consulta de OC de clase 01, ítems y requerimientos de origen.
- Registro de cabecera con grupo de manufactura mediante procedimientos existentes.
- Agregado de detalle por OP/ítem: consulta de saldos, selección, precio y guardado transaccional.
- Redondeo automático de C. compra al siguiente múltiplo de 5, conservando C. requerida y los múltiplos exactos.
- Impresión de OC y guardado PDF desde el navegador, con datos registrados.
- Importación Excel, matriz editable, previsualización y exportación general de compra/Calidad. Estas exportaciones todavía no representan una integración validada con el portal de un proveedor.

**No se ejecutaron registros reales durante la implementación.** Los guardados se probaron con sesiones falsas y con solicitudes API interceptadas en el navegador. El usuario realizará las pruebas con los datos que recibirá.

Guía de pruebas y alcance: [docs/pruebas-registro-oc.md](docs/pruebas-registro-oc.md).

## Arranque local

Requiere SDK .NET 10.0.402 (global.json), Node.js 22 y npm.

Terminal 1 desde la raíz:

```powershell
dotnet restore Intranet.sln
dotnet run --project backend/src/Intranet.Api --launch-profile Intranet.Api
```

Terminal 2:

```powershell
cd frontend
npm ci
npm start
```

Frontend: http://localhost:4200. API: http://localhost:5080. Swagger de desarrollo: http://localhost:5080/swagger.

En desarrollo Angular utiliza http://localhost:5080/api/v1 con credenciales Windows; CORS admite exclusivamente http://localhost:4200. En producción utiliza /api/v1 bajo el mismo origen, con HTTPS y autenticación Windows correctamente configurada.

## Configuración de Gestión

`backend/src/Intranet.Api/appsettings.Local.json`, excluido de Git, contiene la conexión integrada y las asociaciones de identidad. El archivo `.example.json` sirve como referencia.

- `Mode=SqlServer`: datos reales. `Demo` solo se permite en Development y no admite registros.
- `WindowsUsers`: cuenta Windows → usuario propio de Gestión.
- `ReadAllWindowsUsers`: excepción administrativa de consulta; se valida coincidencia con ORIGINAL_LOGIN y rol sysadmin antes de leer la cabecera sin el filtro de LG_SEGORDCOMP.
- `EnableOrderRegistration` y `WriteWindowsUsers`: habilitan registro únicamente para las cuentas autorizadas. La configuración de ejemplo mantiene esta capacidad desactivada; la configuración local la habilita para programador04 por la solicitud de implementar el flujo para sus pruebas.

La identidad del proceso API abre SQL; no hay impersonación/delegación del navegador a SQL. No se deben asociar automáticamente usuarios con identidades de otras personas. La API verifica acceso a la OC antes de consultar detalle o agregarlo.

Los procedimientos mutadores se invocan solo cuando el usuario pulsa los botones de guardado. Las consultas, las exportaciones y la impresión no crean OCs ni detalles.

## Estructura

```text
backend/src/Intranet.Api/
  Controllers/       Endpoints y autorización
  Contracts/         Solicitudes y respuestas
  Services/          Cálculo y orquestación de registro
  Infrastructure/   Dapper, consultas y transacciones
backend/tests/Intranet.Api.Tests/
frontend/src/app/
  core/              Sesión, contratos y servicios HTTP
  home/              Inicio de la intranet
  features/avios/    Consulta, registro, matrices e impresión
docs/
```

## Verificación

```powershell
dotnet test Intranet.sln
cd frontend
npm run build
npm run test:registration
```

34 pruebas de backend aprobadas. La prueba de registro de navegador simula todas las APIs y genera un PDF ficticio; no escribe en HIALPESA. `test:live-read` verifica únicamente consultas reales. `test:smoke` corresponde al flujo de matriz en modo Demo.

## Límites antes de producción

Angular 18 está fuera de soporte y el árbol de dependencias mantiene hallazgos de npm audit documentados en frontend/npm-audit.json. La conexión al SQL heredado negoció TLS 1.0 durante la prueba de lectura. No se cambió la infraestructura ni se actualizaron versiones mayores contra lo solicitado. La auditoría NuGet de la API se corrigió durante la primera entrega.

Confirmar el guardado real y las validaciones heredadas con los datos de prueba del usuario. No hay autorización/cambio de estado, eliminación ni actualización de cabeceras existentes. La impresión web muestra suma de líneas antes de descuentos/impuestos y no reproduce las macros históricas de Excel. La plantilla específica del proveedor y el formato definitivo de revisión de impresión de Calidad siguen requiriendo su definición.

No hay reintentos automáticos de registro. Si se pierde una respuesta después de guardar, consultar antes de repetir. Este proyecto no incluye despliegue productivo ni cambios en SQL.

La organización del backend en Api, Entity, Service y Data se documenta en [backend/README.md](backend/README.md).
