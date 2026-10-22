import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { OrderReference } from '../../../shared/models/lookup.models';
import { ColorConfirmation, ProviderResponse } from '../models/provider.models';
@Injectable({ providedIn: 'root' })
export class ProviderApiService {
    private http = inject(HttpClient);
    private baseUrl = environment.apiBaseUrl;
    preparePoLaunch(po: string, clientCode: string) { return this.http.post<{
        token: string;
        expiresAt: string;
    }>(`${this.baseUrl}/proveedor/preparar-apertura-po`, { po, clientCode }, { withCredentials: true, headers: { 'X-Intranet-Provider': '1' } }); }
    providerPos(reference: OrderReference) { return this.http.get<string[]>(`${this.baseUrl}/proveedor/ordenes/${reference.series}/${reference.number}/pos`, { withCredentials: true }); }
    prepareProvider(reference: OrderReference, po: string) { return this.http.post(`${this.baseUrl}/proveedor/preparar-llenado`, { series: reference.series, number: reference.number, po }, { withCredentials: true, headers: { 'X-Intranet-Provider': '1' }, responseType: 'blob' }); }
    prepareProviderLaunch(reference: OrderReference, po: string) { return this.http.post<{
        token: string;
        expiresAt: string;
    }>(`${this.baseUrl}/proveedor/preparar-apertura`, { series: reference.series, number: reference.number, po }, { withCredentials: true, headers: { 'X-Intranet-Provider': '1' } }); }
    providerLaunchStatus(token: string) { return this.http.post<{
        received: boolean;
    }>(`${this.baseUrl}/proveedor/estado-apertura`, { token }, { withCredentials: true, headers: { 'X-Intranet-Provider': '1' } }); }
    providerLogin(reference: OrderReference, po: string, colorConfirmations: ColorConfirmation[] = []) { return this.http.post<ProviderResponse>(`${this.baseUrl}/proveedor/iniciar-sesion`, { series: reference.series, number: reference.number, po, colorConfirmations }, { withCredentials: true, headers: { 'X-Intranet-Provider': '1' } }); }
}
