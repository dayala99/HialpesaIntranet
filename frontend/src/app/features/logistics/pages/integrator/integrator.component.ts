import { SharedModule } from '../../../../shared/shared.module';
import { Component, DestroyRef, inject } from '@angular/core';
import { SelectedProcessesComponent } from './selected-processes.component';
import { AnalysisPdfExportComponent } from './analysis-pdf-export.component';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { HttpClient } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { environment } from '../../../../../environments/environment';
import { SessionService } from '../../../../core/auth/session.service';
import { AnalysisHeaderComponent } from './analysis-header.component';
import { calculateQuotation } from './quotation-calculations';
import { Router, ActivatedRoute } from '@angular/router';
import { AnalysisRow, HistoryRow, PartialSelection, Requotation, SectorAnalysis } from './analysis.models';
import { AnalysisDraftService } from './analysis-draft.service';
import { apiError } from '../../../../core/http/api-error';
@Component({selector:'app-logistics-integrator',standalone:true,imports:[SharedModule,CommonModule,FormsModule,MatIconModule,AnalysisHeaderComponent,SelectedProcessesComponent,AnalysisPdfExportComponent],templateUrl:'./integrator.component.html',styleUrl:'./integrator.component.scss'})
export class IntegratorComponent {
 otherOpened=false;
 searchTab:'np'|'others'='np';analysisTab:'np'|'others'='np';

 filterPrice:number|null=null;filterTime:number|null=null;filterType='';filterBusy=false;typesBusy=false;typesLoaded=false;filterSearched=false;filterError='';typesError='';typeOptions:{code:string;description:string}[]=[];filterRows:AnalysisRow[]=[];filterHasMore=false;filterNextCursor:string|null=null;filterCursors:(string|null)[]=[null];filterPage=0;private appliedFilters:{price:number|null;time:number|null;type:string}={price:null,time:null,type:''};
 activateOthers(){this.searchTab='others';if(this.typesLoaded||this.typesBusy)return;this.typesBusy=true;this.typesError='';this.http.get<{code:string;description:string}[]>(environment.apiBaseUrl+'/logistica/analisis-rb/tipos',{withCredentials:true}).pipe(takeUntilDestroyed(this.destroy)).subscribe({next:rows=>{this.typeOptions=rows;this.typesLoaded=true;this.typesBusy=false;},error:error=>{this.typesBusy=false;this.typesError=apiError(error);}});}
 searchOther(){if(this.filterBusy)return;this.filterError='';if(this.filterPrice==null&&this.filterTime==null&&!this.filterType){this.filterError='Indica al menos un filtro de búsqueda.';return;}if(this.filterPrice!=null&&(!Number.isFinite(this.filterPrice)||this.filterPrice<0)||this.filterTime!=null&&(!Number.isFinite(this.filterTime)||this.filterTime<0)){this.filterError='Precio y tiempo deben ser valores mayores o iguales a cero.';return;}this.otherOpened=false;this.appliedFilters={price:this.filterPrice,time:this.filterTime,type:this.filterType};this.filterCursors=[null];this.filterPage=0;this.readOther(null);}
 private readOther(cursor:string|null){this.filterBusy=true;this.filterError='';this.filterRows=[];this.filterHasMore=false;this.filterNextCursor=null;const f=this.appliedFilters;const params:Record<string,string>={};if(f.price!=null)params['price']=String(f.price);if(f.time!=null)params['time']=String(f.time);if(f.type)params['type']=f.type;if(cursor)params['cursor']=cursor;this.http.get<{rows:AnalysisRow[];hasMore:boolean;nextCursor:string|null}>(environment.apiBaseUrl+'/logistica/analisis-rb/otros',{params,withCredentials:true}).pipe(takeUntilDestroyed(this.destroy)).subscribe({next:result=>{this.filterRows=result.rows;this.filterHasMore=result.hasMore;this.filterNextCursor=result.nextCursor;this.filterBusy=false;this.filterSearched=true;},error:error=>{this.filterBusy=false;this.filterError=apiError(error);}});}
 nextOther(){if(this.filterBusy||!this.filterHasMore||!this.filterNextCursor)return;this.filterPage++;this.filterCursors[this.filterPage]=this.filterNextCursor;this.readOther(this.filterNextCursor);}
 previousOther(){if(this.filterBusy||this.filterPage<=0)return;this.filterPage--;this.readOther(this.filterCursors[this.filterPage]);}
 backToOtherResults(){if(!this.busy&&!this.saving)this.otherOpened=false;}
 selectOther(row:AnalysisRow){if(this.busy||this.saving)return;this.otherOpened=true;this.np=row.np;this.search();}
 private router=inject(Router);private route=inject(ActivatedRoute);private drafts=inject(AnalysisDraftService);
 private http=inject(HttpClient);private destroy=inject(DestroyRef);readonly session=inject(SessionService);
 private readonly historyUrl=environment.apiBaseUrl+'/logistica/analisis-rb/historial';
 analysisReady=false;sectorAnalysis:SectorAnalysis|null=null;saving=false;saveError='';saveMessage='';partialSelection:PartialSelection|null=null;isRequotation=false;baseServicePrice:number|null=null;percentage:number|null=null;
 private saveSignature='';private requestId='';private lastSavedSignature='';
 get canSave(){return this.analysisReady&&!this.busy&&!this.saving&&!!this.session.current()?.canWriteDatabase&&!!this.current&&this.current.commercialPrice!=null&&this.current.sewingTime!=null&&this.current.sewingTime>0&&this.usedTime!=null&&this.usedTime>0&&this.servicePrice!=null&&this.servicePrice>=0&&this.otherExpenses!=null&&this.otherExpenses>=0&&(!this.isRequotation||this.validRequotation);}
 get sameAsSaved(){return this.lastSavedSignature!==''&&this.signature()===this.lastSavedSignature;}
 private signature(){return JSON.stringify({source:this.current,usedTime:this.usedTime,servicePrice:this.servicePrice,otherExpenses:this.otherExpenses,partialSelection:this.partialSelection,requotation:this.requotation,sectorAnalysis:this.sectorAnalysis});}
 private newRequestId(){const values=crypto.getRandomValues(new Uint8Array(16));values[6]=(values[6]&15)|64;values[8]=(values[8]&63)|128;const hex=Array.from(values,n=>n.toString(16).padStart(2,'0')).join('');return hex.slice(0,8)+'-'+hex.slice(8,12)+'-'+hex.slice(12,16)+'-'+hex.slice(16,20)+'-'+hex.slice(20);}
 save(){
  if(!this.canSave||this.sameAsSaved)return;
  const signature=this.signature();if(signature!==this.saveSignature){this.saveSignature=signature;this.requestId=this.newRequestId();}
  this.saving=true;this.saveError='';this.saveMessage='';
  this.http.post<HistoryRow>(this.historyUrl,{requestId:this.requestId,source:this.current,usedTime:this.usedTime,servicePrice:this.servicePrice,otherExpenses:this.otherExpenses,partialSelection:this.partialSelection,requotation:this.requotation,sectorAnalysis:this.sectorAnalysis},{withCredentials:true,headers:{'X-Intranet-Write':'1'}}).pipe(takeUntilDestroyed(this.destroy)).subscribe({next:row=>{this.saving=false;this.lastSavedSignature=signature;this.saveMessage='Análisis guardado. Registro '+row.id+'.';},error:error=>{this.saving=false;this.saveError=apiError(error)+' Puedes consultar el historial antes de reintentar.';}});
 }
 openHistory(){this.openPage('historial');}
 openOperations(){this.openPage('costura');}
 private openPage(page:string){
  if(!this.searchedNp||this.busy||this.saving)return;
  this.drafts.draft={searchTab:this.searchTab,analysisTab:this.analysisTab,otherSearch:{opened:this.otherOpened,price:this.filterPrice,time:this.filterTime,type:this.filterType,rows:this.filterRows,searched:this.filterSearched,hasMore:this.filterHasMore,nextCursor:this.filterNextCursor,cursors:this.filterCursors,page:this.filterPage,applied:this.appliedFilters},np:this.searchedNp,rows:this.rows,usedTime:this.usedTime,servicePrice:this.servicePrice,otherExpenses:this.otherExpenses,saveSignature:this.saveSignature,requestId:this.requestId,lastSavedSignature:this.lastSavedSignature,saveMessage:this.saveMessage,partialSelection:this.partialSelection,requotation:this.requotation,sectorAnalysis:this.sectorAnalysis,analysisReady:this.analysisReady};
  void this.router.navigate(['/logistica/analiticas/'+page],{queryParams:{np:this.searchedNp}});
 }
 ngOnInit(){
  const np=(this.route.snapshot.queryParamMap.get('np')??'').trim().toUpperCase();const draft=this.drafts.draft;
  if(draft&&(!np||np===draft.np)){this.searchTab=draft.searchTab??'np';this.analysisTab=draft.analysisTab??'np';const other=draft.otherSearch;if(other){this.otherOpened=other.opened??false;this.filterPrice=other.price;this.filterTime=other.time;this.filterType=other.type;this.filterRows=other.rows;this.filterSearched=other.searched;this.filterHasMore=other.hasMore;this.filterNextCursor=other.nextCursor;this.filterCursors=other.cursors;this.filterPage=other.page;this.appliedFilters=other.applied;}if(this.searchTab==='others')this.activateOthers();this.np=this.searchedNp=draft.np;this.rows=draft.rows;this.searched=true;this.usedTime=draft.usedTime;this.servicePrice=draft.servicePrice;this.otherExpenses=draft.otherExpenses;this.saveSignature=draft.saveSignature;this.requestId=draft.requestId;this.lastSavedSignature=draft.lastSavedSignature;this.saveMessage=draft.saveMessage;this.partialSelection=draft.partialSelection??null;this.sectorAnalysis=draft.sectorAnalysis??null;this.analysisReady=draft.analysisReady??false;this.isRequotation=draft.requotation?.enabled??false;this.baseServicePrice=this.isRequotation?(draft.requotation?.baseServicePrice??null):null;this.percentage=this.isRequotation?(draft.requotation?.percentage??null):null;this.drafts.draft=null;this.applyPartial();}
  else if(np){this.np=np;this.search();}
 }

 usedTime:number|null=null;servicePrice:number|null=null;otherExpenses:number|null=0.03;
 editingBasePrice=false;editingPercentage=false;
 decimalDisplay(value:number|null){return value==null||!Number.isFinite(value)?null:value.toFixed(2);}
 editBasePrice(value:number|null){this.editingBasePrice=true;this.baseServicePrice=value;this.recalculateServicePrice();}
 editPercentage(value:number|null){this.editingPercentage=true;this.percentage=value;this.recalculateServicePrice();}
 get requotation():Requotation{return {enabled:this.isRequotation,baseServicePrice:this.isRequotation?this.baseServicePrice:null,percentage:this.isRequotation?this.percentage:null};}
 get validRequotation(){return this.baseServicePrice!=null&&Number.isFinite(this.baseServicePrice)&&this.baseServicePrice>=0&&this.percentage!=null&&Number.isFinite(this.percentage)&&this.percentage>=0;}
 toggleRequotation(enabled:boolean){this.isRequotation=enabled;if(!enabled){this.baseServicePrice=null;this.percentage=null;this.editingBasePrice=false;this.editingPercentage=false;}else if(this.validRequotation)this.recalculateServicePrice();}
 recalculateServicePrice(){if(!this.isRequotation)return;this.servicePrice=this.validRequotation?Math.round(this.baseServicePrice!*this.percentage!*1e6)/1e6:null;}
 onServicePriceChanged(value:number|null){this.servicePrice=value;if(this.isRequotation)this.percentage=this.baseServicePrice!=null&&this.baseServicePrice>0&&value!=null&&Number.isFinite(value)?Number((value/this.baseServicePrice).toFixed(16)):null;}
 private applyPartial(){const pending=this.drafts.pendingAnalysis;if(pending?.np!==this.searchedNp||!this.current)return;this.partialSelection=pending.selection;this.sectorAnalysis=pending.sectorAnalysis;this.analysisReady=true;this.usedTime=pending.sectorAnalysis.isComplete?pending.sectorAnalysis.totalTime:Math.round(pending.selection!.operations.reduce((total,row)=>total+row.standardTime,0)*1e6)/1e6;this.drafts.pendingAnalysis=null;this.saveMessage='';this.saveError='';}
 get analysisTotalTime(){return this.sectorAnalysis?.totalTime??this.current?.sewingTime??null;}
 get selectionTime(){return this.partialSelection?.operations.reduce((total,row)=>total+row.standardTime,0)??0;}
 get current(){return this.rows[0]??null;}
 get today(){const d=new Date();return new Intl.DateTimeFormat('es-PE',{timeZone:'America/Bogota',day:'2-digit',month:'2-digit',year:'numeric'}).format(d);}
 get quotation(){return calculateQuotation({commercialPrice:this.current?.commercialPrice??null,totalTime:this.analysisTotalTime,usedTime:this.usedTime,servicePrice:this.servicePrice,otherExpenses:this.otherExpenses,isComplete:this.sectorAnalysis?.isComplete,sectorName:this.sectorAnalysis?.sectorName,sectorCode:this.sectorAnalysis?.sector,forcePartial:!!this.partialSelection,partialSectorName:this.partialSelection?.sectorName});}
 np='';searchedNp='';busy=false;searched=false;error='';rows:AnalysisRow[]=[];
 search(){
  if(this.busy||this.saving)return;this.saveError='';this.saveMessage='';this.lastSavedSignature='';this.saveSignature='';this.requestId='';
  const np=this.np.trim().toUpperCase();this.np=np;this.error='';this.rows=[];this.searched=false;this.usedTime=null;this.servicePrice=null;this.otherExpenses=0.03;this.partialSelection=null;this.sectorAnalysis=null;this.analysisReady=false;this.isRequotation=false;this.baseServicePrice=null;this.percentage=null;
  if(!/^[A-Z0-9]{5}$/.test(np)){this.error='Ingresa una NP de 5 caracteres, por ejemplo J1907.';return;}
  this.busy=true;this.searchedNp=np;this.analysisTab=this.searchTab;
  this.http.get<AnalysisRow[]>(environment.apiBaseUrl+'/logistica/analisis-rb',{params:{np},withCredentials:true}).pipe(takeUntilDestroyed(this.destroy)).subscribe({next:rows=>{this.rows=rows;this.usedTime=rows[0]?.sewingTime??null;this.searched=true;this.busy=false;this.applyPartial();},error:error=>{this.error=apiError(error);this.busy=false;}});
 }
}





