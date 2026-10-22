import { Subject, switchMap, timer, catchError, of } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatDialog } from '@angular/material/dialog';
import { Component,inject,TemplateRef,DestroyRef,ViewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router';
import { environment } from '../../../environments/environment';
import { apiError } from '../../core/avios-api.service';
import { SessionService } from '../../core/session.service';
interface Source {supplierCode:string;supplier:string;annexType:string;annex:string;currency:string;existingImport:string;supplierOrigin:string;}
interface Option {Kind:string;Code:string;Name:string;}
@Component({selector:'app-imports',templateUrl:'./imports.component.html',styleUrl:'./imports.component.scss'})
export class ImportsComponent {
 @ViewChild('existingImportDialog') existingImportDialog!:TemplateRef<unknown>;
 private dialog=inject(MatDialog);
 confirmCreation(template:TemplateRef<unknown>){if(this.busy||this.blocked)return;this.review=true;this.dialog.open(template,{width:"620px",maxWidth:"94vw",autoFocus:"first-tabbable",restoreFocus:true}).afterClosed().subscribe(confirmed=>{if(confirmed===true)this.save();else this.review=false;});}
 private http=inject(HttpClient);session=inject(SessionService);private route=inject(ActivatedRoute);
 private base=environment.apiBaseUrl+'/logistica/importaciones';
 series=this.route.snapshot.queryParamMap.get('series')||'002';number=this.route.snapshot.queryParamMap.get('number')||'';
 source:Source|null=null;options:Option[]=[];automatic=false;validAnnex=false;busy=false;review=false;error='';success='';loadedSeries='';loadedNumber='';uncertain=false;
 form={itemType:'',origin:'',shipment:'',country:'',area:'05',registeredAt:[new Date().getFullYear(),String(new Date().getMonth()+1).padStart(2,'0'),String(new Date().getDate()).padStart(2,'0')].join('-'),guideDate:null as string|null,paymentDate:null as string|null,dueDate:null as string|null,guide:'',observations:''};
 registrationOpen=!!this.route.snapshot.queryParamMap.get('number');
 rows:{number:string;registeredAt:string;status:string;supplier:string;currency:string;origin:string;series:string;orderNumber:string}[]=[];
 listSearch='';listPage=1;totalPages=1;hasMore=false;listBusy=false;listError='';
 private destroyRef=inject(DestroyRef);private listRequests=new Subject<{page:number;search:string}>();
 ngOnInit(){this.listRequests.pipe(switchMap(q=>timer(300).pipe(switchMap(()=>this.http.get<{rows:ImportsComponent['rows'];page:number;hasMore:boolean;totalPages:number;totalRows:number}>(this.base,{params:q,withCredentials:true})),catchError(e=>{this.listError=apiError(e);return of(null);}))),takeUntilDestroyed(this.destroyRef)).subscribe(r=>{this.listBusy=false;if(r){this.rows=r.rows;this.listPage=r.page;this.totalPages=r.totalPages??1;this.hasMore=r.hasMore;}});if(!this.registrationOpen)this.loadList();}
 loadList(page=1){this.listBusy=true;this.listError='';this.rows=[];this.listRequests.next({page,search:this.listSearch.trim()});}
 openRegistration(){this.source=null;this.number='';this.error='';this.success='';this.review=false;this.uncertain=false;this.registrationOpen=true;}
 backToList(){this.registrationOpen=false;this.source=null;this.error='';this.loadList(this.listPage);}
 constructor(){this.http.get<Option[]>(this.base+'/catalogos',{withCredentials:true}).subscribe({next:r=>this.options=r,error:e=>this.error=apiError(e)});}
 choices(kind:string){return this.options.filter(o=>o.Kind===kind);}
 optionName(kind:string,code:string){return this.options.find(o=>o.Kind===kind && o.Code===code)?.Name || code;}
 search(){this.source=null;this.review=false;this.error='';this.success='';this.busy=true;this.http.get<{source:Source;automaticNumbering:boolean;validAnnex:boolean}>(`${this.base}/ordenes/${this.series}/${this.number}`,{withCredentials:true}).subscribe({next:r=>{this.source=r.source;this.automatic=r.automaticNumbering;this.validAnnex=r.validAnnex;this.loadedSeries=this.series;this.loadedNumber=this.number;this.uncertain=false;this.busy=false;if(this.source.existingImport && this.source.existingImport!=="0"){this.dialog.open(this.existingImportDialog,{width:"480px",maxWidth:"94vw",autoFocus:"first-tabbable",restoreFocus:false,disableClose:true}).afterClosed().subscribe(()=>this.backToList());}},error:e=>{this.error=apiError(e);this.busy=false;}});}
 get blocked(){return !this.source||!this.automatic||!this.validAnnex||this.source.supplierOrigin!=='E'||!!(this.source.existingImport&&this.source.existingImport!=='0')||!this.session.current()?.canWriteDatabase||this.uncertain;}
 save(){if(this.busy||this.blocked||!this.review)return;this.busy=true;this.error='';this.http.post<{number:string}>(`${this.base}/ordenes/${this.loadedSeries}/${this.loadedNumber}`,this.form,{withCredentials:true,headers:{'X-Intranet-Write':'1'}}).subscribe({next:r=>{this.success='Importación '+r.number+' creada y asociada a la OC '+this.loadedSeries+'-'+this.loadedNumber;this.source!.existingImport=r.number;this.review=false;this.busy=false;this.registrationOpen=false;this.loadList();},error:e=>{this.error=apiError(e);this.uncertain=true;this.review=false;this.busy=false;}});}
}

