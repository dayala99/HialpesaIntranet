import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { ArticleRule, MatrixRow, OrderDetail, Preview, Requirement, Session } from './models';
import { RegistrationCatalogs, LookupOption, OrderReference, CreateOrderRequest, PendingRequirement, DetailsSaved, PrintOrder } from './models';
import { environment } from '../../environments/environment';
export interface LinkedRequirement {key:string;factory:string;productionOrder:string;presentation:string;itemCode:string;size:string;quantity:number;}
export interface EditableOrderItem {purchased:number;unitPrice:number;discountCode:string;taxPercent:number;deliveryStart:string;deliveryEnd:string;observations:string;}
export interface PoDetailLine {lot:string;style:string;description:string;colorCode:string;color:string;size:string;required:number;purchased:number;shipped:number;upc:string;}
export interface ColorConfirmation {style:string;colorCode:string;providerColor:string;gestionColor:string;}
export interface ProviderResponse {status:string;message:string;confirmations?:ColorConfirmation[];}
@Injectable({ providedIn: 'root' })
export class AviosApi {
  private http = inject(HttpClient);
  private baseUrl = environment.apiBaseUrl;
  poDetails(po:string,clientCode:string){return this.http.get<PoDetailLine[]>(`${this.baseUrl}/logistica/avios/pos/${encodeURIComponent(po)}/detalle`,{params:{clientCode},withCredentials:true});}
  preparePoLaunch(po:string,clientCode:string){return this.http.post<{token:string;expiresAt:string}>(`${this.baseUrl}/proveedor/preparar-apertura-po`,{po,clientCode},{withCredentials:true,headers:{'X-Intranet-Provider':'1'}});}
  listPos(page:number,po:string,search:string){return this.http.get<{rows:{po:string;clientCode:string;client:string;description:string;createdAt:string}[];page:number;totalPages:number;totalRows:number;hasMore:boolean}>(`${this.baseUrl}/logistica/avios/pos`,{params:{page,po,search},withCredentials:true});}
  listOrders(page:number,search:string){return this.http.get<{rows: {series:string;number:string;supplier:string;status:string;class:string;createdAt:string}[];hasMore:boolean;page:number;totalPages:number;totalRows:number}>(`${this.baseUrl}/logistica/avios/ordenes`,{params:{page,search},withCredentials:true});}
  providerPos(reference:OrderReference){return this.http.get<string[]>(`${this.baseUrl}/proveedor/ordenes/${reference.series}/${reference.number}/pos`,{withCredentials:true});}
  prepareProvider(reference:OrderReference,po:string){return this.http.post(`${this.baseUrl}/proveedor/preparar-llenado`,{series:reference.series,number:reference.number,po},{withCredentials:true,headers:{'X-Intranet-Provider':'1'},responseType:'blob'});}
  prepareProviderLaunch(reference:OrderReference,po:string){return this.http.post<{token:string;expiresAt:string}>(`${this.baseUrl}/proveedor/preparar-apertura`,{series:reference.series,number:reference.number,po},{withCredentials:true,headers:{'X-Intranet-Provider':'1'}});}
  providerLaunchStatus(token:string){return this.http.post<{received:boolean}>(`${this.baseUrl}/proveedor/estado-apertura`,{token},{withCredentials:true,headers:{'X-Intranet-Provider':'1'}});}
  providerLogin(reference:OrderReference,po:string,colorConfirmations:ColorConfirmation[]=[]){return this.http.post<ProviderResponse>(`${this.baseUrl}/proveedor/iniciar-sesion`,{series:reference.series,number:reference.number,po,colorConfirmations}, {withCredentials:true,headers:{'X-Intranet-Provider':'1'}});}
  login(username:string,password:string){return this.http.post<Session>(`${this.baseUrl}/sesion/login`,{username,password},{withCredentials:true,headers:{'X-Intranet-Session':'1'}});}
  logout(){return this.http.post<void>(`${this.baseUrl}/sesion/logout`,{},{withCredentials:true,headers:{'X-Intranet-Session':'1'}});}
  session() { return this.http.get<Session>(`${this.baseUrl}/sesion`, { withCredentials: true }); }
  order(series: string, number: string) { return this.http.get<OrderDetail>(`${this.baseUrl}/logistica/avios/ordenes/${encodeURIComponent(series)}/${encodeURIComponent(number)}`, { withCredentials: true }); }
  requirements(series: string, number: string, sequence: string) { return this.http.get<Requirement[]>(`${this.baseUrl}/logistica/avios/ordenes/${encodeURIComponent(series)}/${encodeURIComponent(number)}/items/${encodeURIComponent(sequence)}/requerimientos`, { withCredentials: true }); }
  orderGroups(order:OrderReference){return this.http.get<LookupOption[]>(`${this.baseUrl}/logistica/avios/ordenes/${order.series}/${order.number}/grupos`,{withCredentials:true});}
  addManufacturingGroups(order:OrderReference,groups:string[],removedGroups:string[]=[]){return this.http.post<void>(`${this.baseUrl}/logistica/avios/ordenes/${order.series}/${order.number}/grupos`,{groups,removedGroups},{withCredentials:true,headers:{'X-Intranet-Write':'1'}});}
  supplierDefaults(code:string){return this.http.get<{conditionCode:string;discountCode:string;origin:string;taxPercent:number|null}>(`${this.baseUrl}/logistica/avios/registro/proveedores/${encodeURIComponent(code)}/valores`,{withCredentials:true});}
  factories(){return this.http.get<LookupOption[]>(`${this.baseUrl}/logistica/avios/registro/fabricas`,{withCredentials:true});}
  groupsByOp(factory:string,productionOrder:string){return this.http.get<LookupOption[]>(`${this.baseUrl}/logistica/avios/registro/grupos-por-op`,{params:{factory,productionOrder},withCredentials:true});}
  registrationCatalogs() { return this.http.get<RegistrationCatalogs>(`${this.baseUrl}/logistica/avios/registro/catalogos`, { withCredentials: true }); }
  lookup(kind: 'proveedores' | 'grupos', search: string) { return this.http.get<LookupOption[]>(`${this.baseUrl}/logistica/avios/registro/catalogos/${kind}`, { params: { search }, withCredentials: true }); }
  linkedRequirements(order:OrderReference,sequence:string){return this.http.get<LinkedRequirement[]>(`${this.baseUrl}/logistica/avios/ordenes/${order.series}/${order.number}/detalles/${sequence}/requerimientos`,{withCredentials:true});}
  removeRequirements(order:OrderReference,sequence:string,keys:string[]){return this.http.post<void>(`${this.baseUrl}/logistica/avios/ordenes/${order.series}/${order.number}/detalles/${sequence}/retirar-requerimientos`,{keys},{withCredentials:true,headers:{'X-Intranet-Write':'1'}});}
  editableItem(order:OrderReference,sequence:string){return this.http.get<EditableOrderItem>(`${this.baseUrl}/logistica/avios/ordenes/${order.series}/${order.number}/detalles/${sequence}`,{withCredentials:true});}
  updateItem(order:OrderReference,sequence:string,request:EditableOrderItem){return this.http.put<void>(`${this.baseUrl}/logistica/avios/ordenes/${order.series}/${order.number}/detalles/${sequence}`,request,{withCredentials:true,headers:{'X-Intranet-Write':'1'}});}
  deleteItem(order:OrderReference,sequence:string){return this.http.delete<void>(`${this.baseUrl}/logistica/avios/ordenes/${order.series}/${order.number}/detalles/${sequence}`,{withCredentials:true,headers:{'X-Intranet-Write':'1'}});}
  editableOrder(order:OrderReference){return this.http.get<{header:CreateOrderRequest;supplierName:string}>(`${this.baseUrl}/logistica/avios/ordenes/${order.series}/${order.number}/edicion`,{withCredentials:true});}
  updateOrder(order:OrderReference,request:CreateOrderRequest){return this.http.put<void>(`${this.baseUrl}/logistica/avios/ordenes/${order.series}/${order.number}`,request,{withCredentials:true,headers:{'X-Intranet-Write':'1'}});}
  createOrder(request: CreateOrderRequest) { return this.http.post<OrderReference>(`${this.baseUrl}/logistica/avios/ordenes`, request, { withCredentials: true, headers: { 'X-Intranet-Write': '1' } }); }
  detailSelector(order:OrderReference,group='',productionOrder=''){return this.http.get<LookupOption[]>(`${this.baseUrl}/logistica/avios/ordenes/${order.series}/${order.number}/selector-detalle`,{params:{group,productionOrder},withCredentials:true});}
  pending(order: OrderReference, productionOrder: string, itemCode: string) { return this.http.get<PendingRequirement[]>(`${this.baseUrl}/logistica/avios/ordenes/${order.series}/${order.number}/pendientes`, { params: { productionOrder, itemCode }, withCredentials: true }); }
  addDetails(order: OrderReference, productionOrder: string, itemCode: string, requirementKeys: string[], unitPrice: number) { return this.http.post<DetailsSaved>(`${this.baseUrl}/logistica/avios/ordenes/${order.series}/${order.number}/detalles`, { productionOrder, itemCode, requirementKeys, unitPrice }, { withCredentials: true, headers: { 'X-Intranet-Write': '1' } }); }
  printOrder(order: OrderReference) { return this.http.get<PrintOrder>(`${this.baseUrl}/logistica/avios/ordenes/${order.series}/${order.number}/impresion`, { withCredentials: true }); }
  preview(rows: MatrixRow[], articles: ArticleRule[]) { return this.http.post<Preview>(`${this.baseUrl}/logistica/avios/previsualizacion`, { rows, articles }, { withCredentials: true }); }
}
export function apiError(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) return 'No se pudo completar la operación.';
  if (error.status === 0 || error.status === 504 || error.status === 502) return 'No hay conexión con la API. Comprueba que esté iniciada.';
  if(error.status===429)return 'Demasiados intentos. Espera un minuto y vuelve a intentar.';
  if (error.status === 401) return error.error?.title || 'Inicia sesión en la intranet para continuar.';
  const body = error.error;
  if (body?.errors) return Object.values(body.errors).flat().join(' ');
  return body?.title || 'No se pudo completar la operación.';
}




