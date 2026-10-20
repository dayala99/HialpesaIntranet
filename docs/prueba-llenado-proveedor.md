# Prueba local de llenado de cantidades

## Uso

1. Consultar una OC en Compras de avíos.
2. En la barra de **Detalle de artículos**, pulsar **Probar llenado en página nueva**.
3. Se abre otra pestaña con un formulario local inicialmente vacío. Cada secuencia genera un campo y se completa automáticamente con su **C. requerida**.
4. La barra de progreso confirma el número de campos completados. La pestaña original también muestra la confirmación.
5. En el formulario de prueba se puede pulsar **Vaciar campos** y **Repetir llenado automático**.

Se transfieren todas las filas de la tabla consultada, sin agruparlas ni emparejarlas por posición de talla. Cada campo conserva la secuencia original, el artículo, el estilo y la talla. Se copian las cantidades requeridas originales, incluidos decimales y cero; no se usan cantidades compradas ni redondeadas.

## Alcance

Es una simulación con `prueba-proveedor.html`, servida por el mismo frontend. No está conectada al proveedor, no envía pedidos y no ejecuta registros en SQL. No se modificó el backend para esta prueba.

La transferencia es en memoria mediante mensajes entre la pestaña original y su página local, comprobando origen, ventana y un identificador aleatorio. No se guarda la OC en almacenamiento del navegador ni se incluye su información de negocio en la URL. La pestaña recibe una instantánea de la OC que estaba consultada al pulsar el botón.

Esta simulación no demuestra que la intranet pueda escribir directamente en cualquier web de otro dominio. La integración con la página real requiere revisar el formulario del proveedor y elegir un mecanismo compatible con sus restricciones, importación o integración disponible.

## Verificación

```powershell
cd frontend
npm run test:supplier-demo
```

La prueba aislada simula las consultas de la API y verifica 298 filas, cantidades exactas, valores distintos de la compra, decimales, cero, vaciado/repetición y diseño móvil. Ninguna solicitud de escritura sale del navegador de prueba.

`npm run test:supplier-demo:live` comprueba el mismo llenado con la OC existente 002-349873. Permite solo GET/OPTIONS a la API; compara todos los campos con la respuesta original y no registra nada.
