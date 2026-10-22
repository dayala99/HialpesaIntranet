import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { SessionService } from './session.service';
export const sessionGuard:CanActivateFn=(_route,state)=>{
 const session=inject(SessionService),router=inject(Router);
 return session.restore().pipe(map(()=>true),catchError(()=>{session.current.set(null);return of(router.createUrlTree(['/login'],{queryParams:{returnUrl:state.url}}));}));
};
