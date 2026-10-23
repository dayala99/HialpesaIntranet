import {ActivatedRoute} from '@angular/router';
import { Injectable,inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { EventFilter,EventOption,EventRow,EventUpdate } from '../models/quality-event.models';
@Injectable({providedIn:'root'}) export class QualityEventsApiService {
 readonly unfinished=inject(ActivatedRoute).snapshot.data['unfinished']===true;
 private http=inject(HttpClient);private base=environment.apiBaseUrl+'/aseguramiento-calidad/'+(this.unfinished?'eventos-tintoreria-tela-por-acabar':'eventos-tintoreria');
 search(f:EventFilter){return this.http.get<{rows:EventRow[];canEdit:boolean}>(this.base,{params:{...f},withCredentials:true});}
 catalogs(){return this.http.get<EventOption[]>(this.base+'/catalogos',{withCredentials:true});}
 items(batch:string){return this.http.get<{customer:{code:string;abbreviation:string;name:string}|null;items:EventRow[]}>(this.base+'/partidas/'+encodeURIComponent(batch)+'/items',{withCredentials:true});}
 save(r:EventUpdate){return this.http.post<{saved:boolean}>(this.base,r,{withCredentials:true,headers:{'X-Intranet-Write':'1'}});}
}
