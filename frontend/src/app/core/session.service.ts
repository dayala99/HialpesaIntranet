import { Injectable, inject, signal } from '@angular/core';
import { AviosApi, apiError } from './avios-api.service';
import { Session } from './models';
@Injectable({ providedIn: 'root' })
export class SessionService {
  private api = inject(AviosApi);
  current = signal<Session | null>(null);
  error = signal('');
  load() { this.api.session().subscribe({ next: s => { this.current.set(s); this.error.set(''); }, error: e => this.error.set(apiError(e)) }); }
}
