# Logs de errores del backend

Destino configurado: `\\PC011426\Compilados\LOGS`.

Se genera un archivo JSON por evento Error o Critical, con fecha UTC, servidor, usuario de la solicitud, método, ruta, traceId y excepción técnica. No se registran cuerpos, cookies, cabeceras ni parámetros de consulta. Las contraseñas y tokens identificados en mensajes se ocultan.

La escritura se hace en segundo plano. Si la carpeta compartida falla o tarda más de dos segundos, el evento se conserva en `logs` dentro del backend. La falta de permisos en ambos destinos se informa por stderr.

El destino puede cambiarse mediante `ErrorLogging:Directory` en appsettings.Local.json o la variable de entorno `ErrorLogging__Directory`.

Para publicar, copiar Compilados\Back al backend de IIS y reciclar el grupo de aplicaciones. La cuenta Windows del proceso de IIS necesita permiso de modificación tanto en el recurso compartido como en la carpeta NTFS de destino, y permiso para escribir en la carpeta local de respaldo. El acceso desde una cuenta de desarrollo no confirma el acceso de IIS.

Ante un fallo de guardado, consultar la importación antes de repetir. El log permite investigar el error sin ejecutar guardados de prueba en producción. Esta actualización registra errores futuros; no recupera el detalle de errores pasados.
