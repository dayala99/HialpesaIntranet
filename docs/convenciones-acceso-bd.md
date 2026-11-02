# Convenciones de acceso a base de datos

Las consultas y operaciones nuevas o modificadas deben usar procedimientos almacenados. No agregar sentencias SELECT/INSERT/UPDATE/DELETE/DDL al backend: usar CommandType.StoredProcedure y parámetros.

Objetos, columnas y parámetros nuevos: nombres en español y prefijos de tres letras según la convención del proyecto (Cod_, Des_, Num_, Imp_, Min_, Fec_, Tip_, Flg_, Txt_). Los alias de los resultados pueden conservar los contratos de API existentes.

Preparar scripts compatibles con SQL Server 2008 R2 y entregarlos en Compilados\SQL. El usuario registra los scripts en HIALPESA; no aplicar cambios de esquema ni datos de negocio sin autorización explícita.
