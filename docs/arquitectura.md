# Arquitectura inicial

## Separación de responsabilidades

Angular se comunica por HTTP con `/api/v1`. La API controla autenticación, permisos de acceso, validación y cálculo; el repositorio Dapper conoce únicamente tres procedimientos de consulta existentes. La base existente conserva el modelo y reglas de Gestión. No existe dependencia del navegador con SQL Server.

El módulo Avíos utiliza rutas diferidas y NgModule. Se pueden añadir otros módulos bajo `features/` sin acoplarlos a un cliente. Los códigos de cliente, proveedor y artículo no están fijados a Vince. Las clases de OC diferentes de 01 se incorporarán con sus propios contratos y procedimientos validados.

## Seguridad y operación

- SqlServer exige autenticación Windows. Cada cuenta debe asociarse explícitamente a su usuario de Gestión en configuración del servidor.
- UP_SEL_ORDCOMP filtra por clases permitidas al usuario; se ejecuta antes de acceder al detalle.
- No se recibe identidad de Gestión desde un parámetro del cliente.
- La conexión integrada usa la identidad del proceso API. Configurar la cuenta de servicio e IIS sin cambiar el esquema de SQL.
- Demo anónimo únicamente en Development y con datos ficticios. La aplicación se niega a iniciar en Production con Demo.
- Sin procedimientos mutadores, SQL dinámico recibido del navegador ni endpoints CRUD genéricos.
- Los errores HTTP no devuelven la cadena SQL ni detalles internos; incluyen traceId para el diagnóstico del servidor.
- El Excel se procesa en el navegador; la matriz se valida nuevamente en la API. Los archivos no se guardan en disco del servidor.
- Las exportaciones contienen valores, sin fórmulas derivadas del texto de entrada.

## Cantidades

Cada fila conserva fábrica, NP, estilo, presentación, color, talla y destino. Las filas duplicadas de esa clave se rechazan en el cálculo. El redondeo usa decimal.Ceiling(cantidad / múltiplo) * múltiplo. Nunca disminuye la necesidad. Los artículos tienen reglas independientes y la suma base no se duplica al mostrar el total original; las unidades de compra sí suman todos los artículos.

Convertir requerimientos a matriz agrupa solo las filas del ítem seleccionado que comparten toda la clave. La presentación de la NP tiene prioridad para el color visible cuando Cod_Color del avío está vacío. Todavía no se reconstruye una OC completa emparejando automáticamente artículos de distintas secuencias.

## Próximas ampliaciones

Confirmar granularidad de redondeo y formatos de proveedor/Calidad; completar selección masiva de requerimientos entre secuencias; definir permisos por acción y empresa/sede; confirmar identidad y despliegue; habilitar operaciones de negocio solo después de revisar sus procedimientos, transacciones y auditoría. No se requieren cambios SQL para probar la entrega actual.
