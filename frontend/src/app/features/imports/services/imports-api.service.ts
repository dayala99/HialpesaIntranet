import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { ImportCatalogOption, ImportCreatedResponse, ImportListResponse, ImportRequest, ImportSourceResponse } from '../models/import.models';
@Injectable({ providedIn: 'root' })
export class ImportsApiService {
    private http = inject(HttpClient);
    private base = environment.apiBaseUrl + '/logistica/importaciones';
    list(page: number, search: string) { return this.http.get<ImportListResponse>(this.base, { params: { page, search }, withCredentials: true }); }
    catalogs() { return this.http.get<ImportCatalogOption[]>(this.base + '/catalogos', { withCredentials: true }); }
    source(series: string, number: string) { return this.http.get<ImportSourceResponse>(this.base + '/ordenes/' + encodeURIComponent(series) + '/' + encodeURIComponent(number), { withCredentials: true }); }
    create(series: string, number: string, request: ImportRequest) { return this.http.post<ImportCreatedResponse>(this.base + '/ordenes/' + encodeURIComponent(series) + '/' + encodeURIComponent(number), request, { withCredentials: true, headers: { 'X-Intranet-Write': '1' } }); }
}
