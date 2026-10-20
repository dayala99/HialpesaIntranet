# Registro de OC de avíos: guía de pruebas del usuario

## Alcance implementado

Nueva OC de clase 01, asociación de un grupo de manufactura, incorporación de requerimientos por OP e ítem, cálculo automático de cantidad de compra e impresión desde los datos guardados. Se reutilizan procedimientos existentes; no se crearon ni modificaron objetos SQL.

El agente NO ejecutó registros de prueba en HIALPESA. Las 34 pruebas del backend usan sesiones falsas para las operaciones de registro. La prueba de navegador intercepta todas las solicitudes API, devuelve datos ficticios y no deja salir escrituras hacia SQL. La impresión ficticia 002-999001 de test-results es un archivo local de prueba, no una OC registrada.

Las operaciones de guardado están implementadas, pero su ejecución efectiva contra los procedimientos heredados queda para la prueba del usuario solicitada. No se verificó un registro real ni se promete que las validaciones de los procedimientos permitan cualquier combinación de datos.

## Recorrido

1. Abrir http://localhost:4200/logistica/avios con la sesión Windows autorizada.
2. Seleccionar **Nueva OC**.
3. Buscar/seleccionar proveedor o ingresar su código. Completar condición de pago, descuento, moneda, IGV, lugar de entrega, centro de costo y fechas.
4. Buscar/ingresar el grupo de manufactura que corresponda a las OPs de la prueba.
5. Pulsar **Guardar OC y grupo**. Este botón sí ejecutará el registro cuando lo pruebe el usuario. La numeración la devuelve Gestión, sin calcularla en Angular. La cabecera y asociación del grupo se confirman juntas.
6. En **Agregar detalle**, ingresar OP (5 caracteres) e ítem (8 caracteres) y pulsar **Consultar requerimientos**. La consulta no registra detalles.
7. Revisar OP, estilo, presentación, talla, cantidad requerida y compra estimada. Seleccionar los requerimientos que se incorporarán.
8. Revisar el precio unitario. Se propone el precio devuelto por la consulta, y el valor ingresado se aplicará a las nuevas secuencias.
9. Pulsar **Guardar detalle en OC**. El backend vuelve a consultar los saldos, rechaza selecciones que cambiaron e incorpora los requerimientos mediante los procedimientos existentes.
10. Cerrar registro para ver el detalle actualizado; usar **Imprimir OC** y después **Imprimir / Guardar PDF**.

También puede consultarse una OC Planeada existente y utilizar **Agregar detalle**. Las órdenes Lista, Autorizada o Cerrada no admiten este botón ni la operación en el backend. No se implementaron cambios de estado ni autorización.

## Regla de cantidad

C. requerida se conserva. C. compra se calcula como `Ceiling(C. requerida / 5) * 5`.

| C. requerida | C. compra |
|---:|---:|
| 11 | 15 |
| 16 | 20 |
| 28 | 30 |
| 10 | 10 |
| 20 | 20 |
| 15 | 15 |

La estimación se muestra por requerimiento. El redondeo guardado se aplica a C. requerida de cada nueva secuencia generada por Gestión. Si el procedimiento agrupa requerimientos, la suma estimada por fila puede variar frente al total de compra final; el resultado de guardado devuelve los totales finales y se refresca el detalle.

## Procedimientos y transacciones

- `UP_MAN_ORDCOMP`: crea cabecera, clase 01 y estado Planeada, y devuelve serie/número.
- `LG_MAN_ORDCOMP_GRUPOS`: asocia el grupo en la misma transacción de cabecera.
- `UP_SEL_REQUEXCOMPRARLOG`: consulta saldos por OP/ítem dentro de los grupos asociados.
- `UP_ACTUALIZA_REQ_OC`: incorpora la selección usando acción C (nuevas secuencias). Recibe cantidades obtenidas por el servidor, no una cantidad arbitraria enviada por el navegador.
- `UP_MAN_ORDCOMPITEM_CANTIDAD_COMPRADA`: ajusta la compra redondeada enviando siempre `pre_unitario` explícito, para no utilizar su valor predeterminado cero.

El detalle completo se confirma en una transacción. Se bloquea la cabecera, el rango de sus ítems y la demanda OP/ítem mientras se valida y guarda, evitando consumir el mismo saldo durante la operación. Si falla una incorporación o el ajuste, la sesión revierte la operación. Estas garantías se comprobaron en la orquestación con dobles de prueba, sin ejecutar procedimientos mutadores en la base real.

## Acceso

La sesión actual de `HIALPESA\programador04` está habilitada en la configuración local para que el usuario pruebe el registro. La configuración de ejemplo mantiene el registro desactivado. No se cambió ningún permiso SQL.

Los endpoints de escritura exigen cuenta Windows autenticada, asociación a Gestión, habilitación explícita en `WriteWindowsUsers`, permiso administrativo existente o sysadmin con identidad coincidente, cuerpo JSON y cabecera `X-Intranet-Write: 1`. No existe un endpoint de ejecución SQL libre.

El procedimiento de cabecera no tiene un parámetro de usuario creador; no se añadió uno ni se actualizó directamente ese campo. La API registra identidad Windows, usuario de Gestión, referencia de OC y traceId en sus logs. La auditoría interna conserva el comportamiento de los procedimientos y triggers existentes.

## Impresión

Vista de OC con proveedor, condiciones, moneda, fechas, grupo, observaciones, líneas, cantidades y precios obtenidos de la base. **Imprimir / Guardar PDF** utiliza la impresión del navegador y no cambia el estado de la OC.

La suma mostrada es el importe de líneas antes de descuentos/impuestos, identificado así en el documento. No sustituye la liquidación fiscal ni reproduce las macros del formato Excel histórico del escritorio.

## Pruebas automáticas aisladas

```powershell
dotnet test Intranet.sln
cd frontend
npm run test:registration
```

El segundo comando necesita el frontend de desarrollo en localhost:4200 y Chrome/Edge instalado. Simula todas las respuestas API: nunca debe sustituirse la interceptación por llamadas reales para probar guardados sin autorización. Capturas y PDF ficticio en frontend/test-results.

Ante un error de red después de guardar, consultar primero la OC o los logs antes de repetir la creación; no hay reintentos automáticos ni persistencia de claves de idempotencia entre reinicios.
