import {Injectable,inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {environment} from '../../../../environments/environment';
import {ControlCatalogs,ControlDetail,ControlFilter,ControlPage,ControlValues,ControlConcept,ControlCondition,ControlDocument,ControlOrder,ControlPolicy,ControlShipment} from '../models/import-control.models';
@Injectable({providedIn:'root'})
export class ImportControlApiService {
 private http=inject(HttpClient);private base=environment.apiBaseUrl+'/logistica/importaciones/control';
 search(filter:ControlFilter){const params:Record<string,string>={};for(const [k,v] of Object.entries(filter))if(v!==''&&v!==null&&v!==undefined)params[k]=String(v);return this.http.get<ControlPage>(this.base,{params,withCredentials:true});}
 catalogs(){return this.http.get<ControlCatalogs>(this.base+'/catalogos',{withCredentials:true});}
 detail(number:string){return this.http.get<ControlDetail>(this.base+'/'+encodeURIComponent(number),{withCredentials:true});}
 annexes(type:string,search:string){return this.http.get<{code:string;description:string}[]>(this.base+'/anexos',{params:{type,search},withCredentials:true});}
 invoices(number:string){return this.http.get<Record<string,unknown>[]>(this.base+'/'+encodeURIComponent(number)+'/facturas',{withCredentials:true});}
 save(number:string|null,values:ControlValues,version:string,concepts?:ControlConcept[],conditions?:ControlCondition[],documentEntries?:ControlDocument[],order?:ControlOrder,policy?:ControlPolicy){const body={values,version,concepts,conditions,documentEntries,order,policy},options={withCredentials:true,headers:{'X-Intranet-Write':'1'}};return number?this.http.put<{number:string}>(this.base+'/'+encodeURIComponent(number),body,options):this.http.post<{number:string}>(this.base,body,options);}
 documentChoices(kind:string,annexType='',annex='',search=''){return this.http.get<Record<string,any>[]>(this.base+'/documentos/opciones',{params:{kind,annexType,annex,search},withCredentials:true});}
 shipments(){return this.http.get<ControlShipment[]>(this.base+'/embarques',{withCredentials:true});}
 saveShipment(action:string,row:ControlShipment){return this.http.post(this.base+'/embarques',{...row,action},{withCredentials:true,headers:{'X-Intranet-Write':'1'}});}
 provision(number:string,version:string,action:'provision'|'reverse'){return this.http.post<{number:string;action:string;status:string}>(this.base+'/'+encodeURIComponent(number)+'/provision',{version,action},{withCredentials:true,headers:{'X-Intranet-Write':'1'}});}
 remove(number:string,version:string){return this.http.delete(this.base+'/'+encodeURIComponent(number),{withCredentials:true,headers:{'X-Intranet-Write':'1'},body:{version}});}
}
