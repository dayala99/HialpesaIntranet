import { HttpErrorResponse } from '@angular/common/http';
export function apiError(error: unknown): string {
    if (!(error instanceof HttpErrorResponse))
        return 'No se pudo completar la operación.';
    if (error.status === 0 || error.status === 504 || error.status === 502)
        return 'No hay conexión con la API. Comprueba que esté iniciada.';
    if (error.status === 429)
        return 'Demasiados intentos. Espera un minuto y vuelve a intentar.';
    if (error.status === 401)
        return error.error?.title || 'Inicia sesión en la intranet para continuar.';
    const body = error.error;
    if (body?.errors)
        return Object.values(body.errors).flat().join(' ');
    return body?.title || 'No se pudo completar la operación.';
}
