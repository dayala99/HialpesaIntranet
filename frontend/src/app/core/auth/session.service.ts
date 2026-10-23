import { Injectable, inject, signal } from '@angular/core';
import { tap } from 'rxjs';
import { SessionApiService } from './session-api.service';
import { apiError } from '../http/api-error';
import { Session } from './session.models';
@Injectable({ providedIn: 'root' })
export class SessionService {
    private api = inject(SessionApiService);
    current = signal<Session | null>(null);
    error = signal('');
    can(option:string){const session=this.current();return !!session&&(session.isGlobalUser===true||session.allowedOptions?.includes(option)===true);}
    canAny(...options:string[]){return options.some(option=>this.can(option));}
    restore() { return this.api.session().pipe(tap(s => { this.current.set(s); this.error.set(''); })); }
    login(username: string, password: string) { return this.api.login(username, password).pipe(tap(s => { this.current.set(s); this.error.set(''); })); }
    logout() { return this.api.logout().pipe(tap(() => { this.current.set(null); this.error.set(''); })); }
    load() { this.restore().subscribe({ error: e => this.error.set(apiError(e)) }); }
}
