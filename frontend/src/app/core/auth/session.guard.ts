import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { SessionService } from './session.service';
export function requiredOption(url:string):string|null {
 const path=url.split(/[?#]/)[0].replace(/\/$/,'');
 if(path==='/logistica/avios/nueva')return 'orders.create';
 if(path==='/logistica/avios'||path.startsWith('/logistica/avios/'))return 'orders.query';
 if(path==='/logistica/importaciones'||path.startsWith('/logistica/importaciones/'))return 'imports';
 if(path==='/tintoreria/generacion-partidas'||path.startsWith('/tintoreria/generacion-partidas/'))return 'dyeing.batches';
 if(path==='/aseguramiento-calidad/eventos-tintoreria')return 'quality.events';
 if(path==='/aseguramiento-calidad/eventos-tintoreria-tela-por-acabar')return 'quality.unfinished';
 return null;
}
export const sessionGuard: CanActivateFn = (_route, state) => {
    const session = inject(SessionService), router = inject(Router);
    return session.restore().pipe(map(() => {const required=requiredOption(state.url);return !required||session.can(required)?true:router.createUrlTree(['/sin-acceso']);}), catchError(() => { session.current.set(null); return of(router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } })); }));
};
