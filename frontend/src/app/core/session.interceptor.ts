import { inject } from '@angular/core';
import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
export const sessionInterceptor:HttpInterceptorFn=(request,next)=>{
 const router=inject(Router);
 return next(request).pipe(catchError(error=>{
  if(error instanceof HttpErrorResponse&&error.status===401&&!request.url.endsWith('/sesion/login')&&!request.url.endsWith('/sesion')&&!router.url.startsWith('/login'))void router.navigate(['/login'],{queryParams:{returnUrl:router.url}});
  return throwError(()=>error);
 }));
};
