import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { ArticleRule, MatrixRow, OrderDetail, Preview, Requirement, Session } from './models';
import { RegistrationCatalogs, LookupOption, OrderReference, CreateOrderRequest, PendingRequirement, DetailsSaved, PrintOrder } from './models';
import { environment } from '../../environments/environment';
export interface ColorConfirmation {style:string;colorCode:string;providerColor:string;gestionColor:string;}
export interface ProviderResponse {status:string;message:string;confirmations?:ColorConfirmation[];}
@Injectable({ providedIn: 'root' })
export class AviosApi {
  private http = inject(HttpClient);
  private baseUrl = environment.apiBaseUrl;
  listOrders(page:number,search:string){return this.http.get<{rows: {series:string;number:string;supplier:string;status:string;class:string;createdAt:string}[];hasMore:boolean;page:number}>(`${this.baseUrl}/logistica/avios/ordenes`,{params:{page,search},withCredentials:true});}
  providerPos(reference:OrderReference){return this.http.get<string[]>(`${this.baseUrl}/proveedor/ordenes/${reference.series}/${reference.number}/pos`,{withCredentials:true});}
  providerLogin(reference:OrderReference,po:string,colorConfirmations:ColorConfirmation[]=[]){return this.http.post<ProviderResponse>(`${this.baseUrl}/proveedor/iniciar-sesion`,{series:reference.series,number:reference.number,po,colorConfirmations}, {withCredentials:true,headers:{'X-Intranet-Provider':'1'}});}
  session() { return this.http.get<Session>(`${this.baseUrl}/sesion`, { withCredentials: true }); }
  order(series: string, number: string) { return this.http.get<OrderDetail>(`${this.baseUrl}/logistica/avios/ordenes/${encodeURIComponent(series)}/${encodeURIComponent(number)}`, { withCredentials: true }); }
  requirements(series: string, number: string, sequence: string) { return this.http.get<Requirement[]>(`${this.baseUrl}/logistica/avios/ordenes/${encodeURIComponent(series)}/${encodeURIComponent(number)}/items/${encodeURIComponent(sequence)}/requerimientos`, { withCredentials: true }); }
  registrationCatalogs() { return this.http.get<RegistrationCatalogs>(`${this.baseUrl}/logistica/avios/registro/catalogos`, { withCredentials: true }); }
  lookup(kind: 'proveedores' | 'grupos', search: string) { return this.http.get<LookupOption[]>(`${this.baseUrl}/logistica/avios/registro/catalogos/${kind}`, { params: { search }, withCredentials: true }); }
  createOrder(request: CreateOrderRequest) { return this.http.post<OrderReference>(`${this.baseUrl}/logistica/avios/ordenes`, request, { withCredentials: true, headers: { 'X-Intranet-Write': '1' } }); }
  pending(order: OrderReference, productionOrder: string, itemCode: string) { return this.http.get<PendingRequirement[]>(`${this.baseUrl}/logistica/avios/ordenes/${order.series}/${order.number}/pendientes`, { params: { productionOrder, itemCode }, withCredentials: true }); }
  addDetails(order: OrderReference, productionOrder: string, itemCode: string, requirementKeys: string[], unitPrice: number) { return this.http.post<DetailsSaved>(`${this.baseUrl}/logistica/avios/ordenes/${order.series}/${order.number}/detalles`, { productionOrder, itemCode, requirementKeys, unitPrice }, { withCredentials: true, headers: { 'X-Intranet-Write': '1' } }); }
  printOrder(order: OrderReference) { return this.http.get<PrintOrder>(`${this.baseUrl}/logistica/avios/ordenes/${order.series}/${order.number}/impresion`, { withCredentials: true }); }
  preview(rows: MatrixRow[], articles: ArticleRule[]) { return this.http.post<Preview>(`${this.baseUrl}/logistica/avios/previsualizacion`, { rows, articles }, { withCredentials: true }); }
}
export function apiError(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) return 'No se pudo completar la operación.';
  if (error.status === 0 || error.status === 504 || error.status === 502) return 'No hay conexión con la API. Comprueba que esté iniciada.';
  if (error.status === 401) return 'Inicia sesión con tu cuenta Windows para continuar.';
  const body = error.error;
  if (body?.errors) return Object.values(body.errors).flat().join(' ');
  return body?.title || 'No se pudo completar la operación.';
}
