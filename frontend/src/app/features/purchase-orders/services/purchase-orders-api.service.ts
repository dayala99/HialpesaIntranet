import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { ArticleRule, MatrixRow, OrderDetail, Preview, Requirement, RegistrationCatalogs, LookupOption, OrderReference, CreateOrderRequest, PendingRequirement, DetailsSaved, PrintOrder, LinkedRequirement, EditableOrderItem } from '../models/purchase-order.models';
@Injectable({ providedIn: 'root' })
export class PurchaseOrdersApiService {
    private http = inject(HttpClient);
    private baseUrl = environment.apiBaseUrl;
    listOrders(page: number, search: string) { return this.http.get<{
        rows: {
            series: string;
            number: string;
            supplier: string;
            status: string;
            class: string;
            createdAt: string;
        }[];
        hasMore: boolean;
        page: number;
        totalPages: number;
        totalRows: number;
    }>(`${this.baseUrl}/logistica/avios/ordenes`, { params: { page, search }, withCredentials: true }); }
    order(series: string, number: string) { return this.http.get<OrderDetail>(`${this.baseUrl}/logistica/avios/ordenes/${encodeURIComponent(series)}/${encodeURIComponent(number)}`, { withCredentials: true }); }
    requirements(series: string, number: string, sequence: string) { return this.http.get<Requirement[]>(`${this.baseUrl}/logistica/avios/ordenes/${encodeURIComponent(series)}/${encodeURIComponent(number)}/items/${encodeURIComponent(sequence)}/requerimientos`, { withCredentials: true }); }
    orderGroups(order: OrderReference) { return this.http.get<LookupOption[]>(`${this.baseUrl}/logistica/avios/ordenes/${order.series}/${order.number}/grupos`, { withCredentials: true }); }
    addManufacturingGroups(order: OrderReference, groups: string[], removedGroups: string[] = []) { return this.http.post<void>(`${this.baseUrl}/logistica/avios/ordenes/${order.series}/${order.number}/grupos`, { groups, removedGroups }, { withCredentials: true, headers: { 'X-Intranet-Write': '1' } }); }
    supplierDefaults(code: string) { return this.http.get<{
        conditionCode: string;
        discountCode: string;
        origin: string;
        taxPercent: number | null;
    }>(`${this.baseUrl}/logistica/avios/registro/proveedores/${encodeURIComponent(code)}/valores`, { withCredentials: true }); }
    factories() { return this.http.get<LookupOption[]>(`${this.baseUrl}/logistica/avios/registro/fabricas`, { withCredentials: true }); }
    groupsByOp(factory: string, productionOrder: string) { return this.http.get<LookupOption[]>(`${this.baseUrl}/logistica/avios/registro/grupos-por-op`, { params: { factory, productionOrder }, withCredentials: true }); }
    registrationCatalogs() { return this.http.get<RegistrationCatalogs>(`${this.baseUrl}/logistica/avios/registro/catalogos`, { withCredentials: true }); }
    lookup(kind: 'proveedores' | 'grupos', search: string) { return this.http.get<LookupOption[]>(`${this.baseUrl}/logistica/avios/registro/catalogos/${kind}`, { params: { search }, withCredentials: true }); }
    linkedRequirements(order: OrderReference, sequence: string) { return this.http.get<LinkedRequirement[]>(`${this.baseUrl}/logistica/avios/ordenes/${order.series}/${order.number}/detalles/${sequence}/requerimientos`, { withCredentials: true }); }
    removeRequirements(order: OrderReference, sequence: string, keys: string[]) { return this.http.post<void>(`${this.baseUrl}/logistica/avios/ordenes/${order.series}/${order.number}/detalles/${sequence}/retirar-requerimientos`, { keys }, { withCredentials: true, headers: { 'X-Intranet-Write': '1' } }); }
    editableItem(order: OrderReference, sequence: string) { return this.http.get<EditableOrderItem>(`${this.baseUrl}/logistica/avios/ordenes/${order.series}/${order.number}/detalles/${sequence}`, { withCredentials: true }); }
    updateItem(order: OrderReference, sequence: string, request: EditableOrderItem) { return this.http.put<void>(`${this.baseUrl}/logistica/avios/ordenes/${order.series}/${order.number}/detalles/${sequence}`, request, { withCredentials: true, headers: { 'X-Intranet-Write': '1' } }); }
    deleteItem(order: OrderReference, sequence: string) { return this.http.delete<void>(`${this.baseUrl}/logistica/avios/ordenes/${order.series}/${order.number}/detalles/${sequence}`, { withCredentials: true, headers: { 'X-Intranet-Write': '1' } }); }
    editableOrder(order: OrderReference) { return this.http.get<{
        header: CreateOrderRequest;
        supplierName: string;
    }>(`${this.baseUrl}/logistica/avios/ordenes/${order.series}/${order.number}/edicion`, { withCredentials: true }); }
    updateOrder(order: OrderReference, request: CreateOrderRequest) { return this.http.put<void>(`${this.baseUrl}/logistica/avios/ordenes/${order.series}/${order.number}`, request, { withCredentials: true, headers: { 'X-Intranet-Write': '1' } }); }
    createOrder(request: CreateOrderRequest) { return this.http.post<OrderReference>(`${this.baseUrl}/logistica/avios/ordenes`, request, { withCredentials: true, headers: { 'X-Intranet-Write': '1' } }); }
    detailSelector(order: OrderReference, group = '', productionOrder = '') { return this.http.get<LookupOption[]>(`${this.baseUrl}/logistica/avios/ordenes/${order.series}/${order.number}/selector-detalle`, { params: { group, productionOrder }, withCredentials: true }); }
    pending(order: OrderReference, productionOrder: string, itemCode: string) { return this.http.get<PendingRequirement[]>(`${this.baseUrl}/logistica/avios/ordenes/${order.series}/${order.number}/pendientes`, { params: { productionOrder, itemCode }, withCredentials: true }); }
    addDetails(order: OrderReference, productionOrder: string, itemCode: string, requirementKeys: string[], unitPrice: number) { return this.http.post<DetailsSaved>(`${this.baseUrl}/logistica/avios/ordenes/${order.series}/${order.number}/detalles`, { productionOrder, itemCode, requirementKeys, unitPrice }, { withCredentials: true, headers: { 'X-Intranet-Write': '1' } }); }
    printOrder(order: OrderReference) { return this.http.get<PrintOrder>(`${this.baseUrl}/logistica/avios/ordenes/${order.series}/${order.number}/impresion`, { withCredentials: true }); }
    preview(rows: MatrixRow[], articles: ArticleRule[]) { return this.http.post<Preview>(`${this.baseUrl}/logistica/avios/previsualizacion`, { rows, articles }, { withCredentials: true }); }
}
