import { Injectable, inject, signal } from '@angular/core';
import { tap } from 'rxjs';
import { AviosApi, apiError } from './avios-api.service';
import { Session } from './models';
@Injectable({ providedIn: 'root' })
export class SessionService {
 private api=inject(AviosApi);current=signal<Session|null>(null);error=signal('');
 restore(){return this.api.session().pipe(tap(s=>{this.current.set(s);this.error.set('');}));}
 login(username:string,password:string){return this.api.login(username,password).pipe(tap(s=>{this.current.set(s);this.error.set('');}));}
 logout(){return this.api.logout().pipe(tap(()=>{this.current.set(null);this.error.set('');}));}
 load(){this.restore().subscribe({error:e=>this.error.set(apiError(e))});}
}
