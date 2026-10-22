import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { PoDetailLine } from '../models/purchase-order.models';
@Injectable({ providedIn: 'root' })
export class PoApiService {
    private http = inject(HttpClient);
    private baseUrl = environment.apiBaseUrl;
    poDetails(po: string, clientCode: string) { return this.http.get<PoDetailLine[]>(`${this.baseUrl}/logistica/avios/pos/${encodeURIComponent(po)}/detalle`, { params: { clientCode }, withCredentials: true }); }
    listPos(page: number, po: string, search: string) { return this.http.get<{
        rows: {
            po: string;
            clientCode: string;
            client: string;
            description: string;
            createdAt: string;
        }[];
        page: number;
        totalPages: number;
        totalRows: number;
        hasMore: boolean;
    }>(`${this.baseUrl}/logistica/avios/pos`, { params: { page, po, search }, withCredentials: true }); }
}
