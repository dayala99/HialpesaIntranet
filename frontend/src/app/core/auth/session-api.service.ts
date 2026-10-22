import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Session } from './session.models';
@Injectable({ providedIn: 'root' })
export class SessionApiService {
    private http = inject(HttpClient);
    private baseUrl = environment.apiBaseUrl;
    login(username: string, password: string) { return this.http.post<Session>(`${this.baseUrl}/sesion/login`, { username, password }, { withCredentials: true, headers: { 'X-Intranet-Session': '1' } }); }
    logout() { return this.http.post<void>(`${this.baseUrl}/sesion/logout`, {}, { withCredentials: true, headers: { 'X-Intranet-Session': '1' } }); }
    session() { return this.http.get<Session>(`${this.baseUrl}/sesion`, { withCredentials: true }); }
}
