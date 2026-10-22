# Backend por capas

La solución contiene cuatro proyectos .NET 10. Las rutas y los JSON de la API se conservan.

| Proyecto | Responsabilidad |
| --- | --- |
| Intranet.Api | Controllers, validación HTTP, cookies, autorización, middleware y composición de dependencias. |
| Intranet.Entity | Entidades y DTO, opciones, contratos de repositorios y transacciones, excepciones y reglas puras compartidas. |
| Intranet.Service | Casos de uso de OC e importaciones, permisos del usuario, cálculo de cantidades, firma y coordinación del agente VINCE. Expone interfaces de servicios. |
| Intranet.Data | Repositorios, Dapper, conexiones SQL, procedimientos almacenados y transacciones con validaciones de consistencia. Implementa las interfaces de Entity. |

## Dependencias

- Api llama a interfaces de Service. Solo la composición en Program registra implementaciones de Data.
- Service consume interfaces de repositorios declaradas en Entity. No referencia Data, Api ni ASP.NET Core.
- Data referencia Entity. No referencia Service ni Api.
- Entity no referencia ninguna otra capa.

En ejecución: Controller → Service → interfaz de repositorio → Data → SQL Server.
Los modelos compartidos se encuentran en Entity.

## Organización

- Api/Controllers: AviosController, OrderRegistrationController, ImportsController, ProviderController y SessionController.
- Api/ServiceResultExtensions.cs: convierte los resultados de casos de uso a respuestas HTTP, sin trasladar IActionResult a Service.
- Entity/Contracts, Interfaces, Options y Rules.
- Service/Services, Interfaces, Security, Provider y Common.
- Data/Repositories, Queries y Connections.

RequestActor se construye por solicitud en Api a partir de la identidad autenticada y de las cabeceras de protección existentes. Service recibe ese contexto sin HttpContext. Las cabeceras no sustituyen las validaciones de identidad y acceso existentes.

La regla compartida QuantityRules mantiene el mismo redondeo para consultas, registro y generación de datos del agente. QuantityCalculator conserva su API pública para los casos de uso.

## Configuración y ejecución

appsettings.json y appsettings.Local.json permanecen en Intranet.Api. No deben copiarse a Entity, Service ni Data. La configuración local de HIALPESA no se modifica con esta reorganización.

Desde la raíz del repositorio:

~~~powershell
dotnet build Intranet.sln -c Release
dotnet test backend/tests/Intranet.Api.Tests/Intranet.Api.Tests.csproj -c Release
dotnet run --project backend/src/Intranet.Api --configuration Release
~~~

Las pruebas LayerDependenciesTests comprueban los límites entre ensamblados. La prueba HTTP backend/tests/http-layering.cjs requiere una instancia aislada en Development, Gestion__Mode=Demo, y valida rutas, autenticación, errores, cálculo y tickets sin acceder a HIALPESA. Su URL se configura con INTRANET_LAYER_TEST_URL (por defecto http://localhost:5089).

## Publicación

Se publica únicamente el proyecto de entrada, que incluye automáticamente las bibliotecas de sus dependencias:

~~~powershell
dotnet publish backend/src/Intranet.Api/Intranet.Api.csproj -c Release -o D:/Programador04/Archivos/Compilados/Back
~~~

El despliegue requiere las DLL Intranet.Api, Intranet.Service, Intranet.Data e Intranet.Entity junto con las demás dependencias. No sustituir solo Intranet.Api.dll.

Los procesos y la firma del agente permanecen en Service/Provider. Los scripts del navegador continúan en frontend/tools y en el paquete distribuible del agente; sus rutas no cambian.

## SQL directo inventariado

[sql/PA_Consultas_Backend.sql](sql/PA_Consultas_Backend.sql) contiene 46 procedimientos nuevos propuestos para las consultas directas de Data, con nombres PA_TABLA_S0001 y secuencia independiente por tabla. Incluye parámetros, índice, origen y notas de transacciones. Se preservan los SP reutilizados. El SQL directo de Data fue reemplazado por llamadas a los PA_, incluyendo los parámetros LockRow y ReadAll. Los seis catálogos de registro se consultan por separado y mantienen el mismo DTO. Los 46 procedimientos fueron desplegados por el usuario y verificados en HIALPESA; Data ya los invoca mediante CommandType.StoredProcedure.
