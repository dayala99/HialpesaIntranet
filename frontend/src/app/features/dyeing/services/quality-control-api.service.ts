import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { QualityControlScreen, QualityControlUpdate } from '../models/quality-control.models';
import { DyeingCatalogOption, DyeingRow } from '../models/dyeing.models';
@Injectable({providedIn:'root'})
export class QualityControlApiService {
 private http=inject(HttpClient);
 private base=environment.apiBaseUrl+'/tintoreria/partidas';
 private url(batch:string,sequence:number){return this.base+'/'+encodeURIComponent(batch)+'/items/'+sequence+'/control-calidad';}
 read(batch:string,sequence:number){return this.http.get<QualityControlScreen>(this.url(batch,sequence),{withCredentials:true});}
 save(batch:string,sequence:number,request:QualityControlUpdate){return this.http.put<{saved:boolean}>(this.url(batch,sequence),request,{withCredentials:true,headers:{'X-Intranet-Write':'1'}});}
 fabrics(search:string){return this.http.get<DyeingCatalogOption[]>(this.base+'/control-calidad/telas',{params:{search},withCredentials:true});}
 consumption(batch:string,sequence:number){return this.http.get<DyeingRow[]>(this.url(batch,sequence)+'/consumo',{withCredentials:true});}
}
