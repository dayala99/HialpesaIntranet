# Estructura del frontend

La aplicación conserva sus rutas públicas y contratos HTTP.

- `src/app/core`: sesión, autenticación, interceptores y manejo de errores HTTP.
- `src/app/layout`: contenedor, navegación y cabecera de la aplicación.
- `src/app/shared`: componentes, directivas y modelos compartidos sin reglas de negocio de un módulo.
- `src/app/features/purchase-orders`: páginas, registro, impresión, modelos y servicios de órdenes de compra y consultas PO.
- `src/app/features/imports`: páginas, modelos, servicios y diálogos de importaciones.
- `src/app/features/provider`: integración del proveedor, selección de PO, confirmación de colores y apertura del agente.
- `src/app/features/auth` y `features/home`: acceso e inicio.

Cada diálogo está en `components/dialogs/<nombre>` y tiene archivos TypeScript, HTML y SCSS propios. Sus interfaces de entrada y resultado se declaran junto al componente; se comunican mediante `MAT_DIALOG_DATA` y `MatDialogRef`. Los temporizadores y solicitudes de los diálogos se cancelan al destruir el componente. Las confirmaciones genéricas se encuentran en `shared/components/dialogs`.

Las páginas coordinan el flujo y los servicios de cada módulo realizan las llamadas HTTP. Los módulos de OC e importaciones se cargan mediante sus propios archivos de rutas. Las rutas `/login`, `/`, `/logistica/avios`, `/logistica/avios/nueva` y `/logistica/importaciones` se mantienen.

`frontend/tools` y el proyecto `agent` contienen la automatización del portal; esta reorganización no cambia esos scripts. El frontend prepara la operación y abre el protocolo del agente mediante `AgentLauncherService`.

## Verificación

Compilar: `npm --prefix frontend run build -- --configuration production --base-href /FrontEnd/`.

Pruebas de interfaz con APIs simuladas: `order-groups-edit.cjs`, `order-item-actions.cjs`, `order-edit.cjs`, `withdraw-requirements.cjs`, `po-actions.cjs`, `provider-agent-launch.cjs`, `supplier-defaults.cjs` e `imports-isolated.cjs`, ubicadas en `frontend/tests`. No sustituyen una prueba completa contra el portal del proveedor.
