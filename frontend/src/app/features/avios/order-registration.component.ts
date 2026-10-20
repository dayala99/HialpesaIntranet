import { Component, EventEmitter, Input, OnInit, Output, inject } from '@angular/core';
import { finalize, Subject, debounceTime, switchMap, catchError, of } from 'rxjs';
import { DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AviosApi, apiError } from '../../core/avios-api.service';
import { SessionService } from '../../core/session.service';
import { CreateOrderRequest, LookupOption, OrderReference, PendingRequirement, RegistrationCatalogs } from '../../core/models';

@Component({ selector:'app-order-registration',templateUrl:'./order-registration.component.html',styleUrl:'./order-registration.component.scss' })
export class OrderRegistrationComponent implements OnInit {
  @Input() returnLabel='Volver al listado';
  @Input() orderReference: OrderReference | null=null;
  @Output() orderCreated=new EventEmitter<OrderReference>();
  @Output() detailsSaved=new EventEmitter<OrderReference>();
  @Output() printRequested=new EventEmitter<OrderReference>();
  @Output() closed=new EventEmitter<void>();
  api=inject(AviosApi); session=inject(SessionService);
  catalogs: RegistrationCatalogs | null=null;
  supplierSearch='';groupSearch='';suppliers:LookupOption[]=[];groups:LookupOption[]=[];
  groupLookupDone=false;
  busy=false;looking=false;error='';message='';saving=false;
  productionOrder='';itemCode='';unitPrice:number|null=null;
  candidates:PendingRequirement[]=[];selected=new Set<string>();
  loadedProductionOrder='';loadedItemCode='';
  private today(){const date=new Date();return `${date.getFullYear()}-${String(date.getMonth()+1).padStart(2,'0')}-${String(date.getDate()).padStart(2,'0')}`;}
  header:CreateOrderRequest={supplierCode:'',conditionCode:'',discountCode:'',currencyCode:'',deliveryLocationCode:'',costCenterCode:'',manufacturingGroup:'',taxPercent:18,deliveryStart:this.today(),deliveryEnd:this.today(),observations:''};
  private destroyRef=inject(DestroyRef);
  private groupQueries=new Subject<string>();
  groupOpen=false;groupLoading=false;groupError='';activeGroup=-1;
  ngOnInit(){
    this.groupQueries.pipe(debounceTime(250),switchMap(query=>{
      this.groupLoading=true;this.groupError='';
      return this.api.lookup('grupos',query).pipe(catchError(e=>{this.groupError=apiError(e);return of([] as LookupOption[]);}));
    }),takeUntilDestroyed(this.destroyRef)).subscribe(rows=>{this.groups=rows;this.groupLookupDone=true;this.groupLoading=false;this.activeGroup=-1;});
    if(!this.orderReference)this.loadCatalogs();
  }
  filterGroups(value:string){this.groupSearch=value;this.header.manufacturingGroup='';this.groups=[];this.groupLookupDone=false;this.groupLoading=true;this.groupOpen=true;this.groupQueries.next(value.trim());}
  openGroups(){this.groupOpen=true;this.groupQueries.next(this.header.manufacturingGroup||this.groupSearch.trim());}
  selectGroup(group:LookupOption){this.header.manufacturingGroup=group.code;this.groupSearch=group.code+' · '+group.description;this.groupOpen=false;this.activeGroup=-1;}
  groupKey(event:KeyboardEvent){
    if(event.key==='Escape'){this.groupOpen=false;event.preventDefault();}
    else if(event.key==='ArrowDown'||event.key==='ArrowUp'){event.preventDefault();if(!this.groupOpen){this.openGroups();return;}const n=this.groups.length;if(n)this.activeGroup=(this.activeGroup+(event.key==='ArrowDown'?1:-1)+n)%n;}
    else if(event.key==='Enter'&&this.groupOpen){event.preventDefault();if(this.activeGroup>=0&&this.groups[this.activeGroup])this.selectGroup(this.groups[this.activeGroup]);}
  }
  loadCatalogs(){this.busy=true;this.error='';this.api.registrationCatalogs().pipe(finalize(()=>this.busy=false)).subscribe({next:data=>this.catalogs=data,error:e=>this.error=apiError(e)});}
  lookup(kind:'proveedores'|'grupos'){
    const query=kind==='proveedores'?this.supplierSearch:this.groupSearch;
    if(kind==='proveedores' && query.trim().length<2){this.error='Ingresa al menos dos caracteres para buscar.';return;}
    this.looking=true;this.error='';this.message='';this.api.lookup(kind,query).pipe(finalize(()=>this.looking=false)).subscribe({next:rows=>{if(kind==='proveedores')this.suppliers=rows;else {this.groups=rows;this.groupLookupDone=true;}if(!rows.length)this.message='No se encontraron coincidencias.';},error:e=>this.error=apiError(e)});
  }
  create(){
    if(this.saving||!this.session.current()?.canWriteDatabase)return;
    this.saving=true;this.error='';this.message='';
    this.api.createOrder({...this.header}).pipe(finalize(()=>this.saving=false)).subscribe({next:order=>{this.orderReference=order;this.orderCreated.emit(order);this.message=`OC ${order.series}-${order.number} registrada con su grupo de manufactura. Ahora puedes agregar el detalle.`;},error:e=>this.error=apiError(e)});
  }
  clearPending(){this.candidates=[];this.selected.clear();this.unitPrice=null;this.loadedProductionOrder='';this.loadedItemCode='';this.message='';}
  pending(){
    if(!this.orderReference)return;
    this.error='';this.message='';this.clearPending();
    const op=this.productionOrder.trim(),item=this.itemCode.trim();
    if(!/^[A-Za-z0-9]{5}$/.test(op)||!/^[A-Za-z0-9]{8}$/.test(item)){this.error='Ingresa una OP de 5 caracteres y un código de ítem de 8 caracteres.';return;}
    this.busy=true;
    this.api.pending(this.orderReference,op,item).pipe(finalize(()=>this.busy=false)).subscribe({next:rows=>{this.candidates=rows;this.loadedProductionOrder=op;this.loadedItemCode=item;this.selected=new Set(rows.map(r=>r.key));this.unitPrice=rows[0]?.unitPrice??null;if(!rows.length)this.message='No hay requerimientos pendientes para esta OP e ítem.';},error:e=>this.error=apiError(e)});
  }
  toggle(key:string,checked:boolean){if(checked)this.selected.add(key);else this.selected.delete(key);}
  toggleAll(checked:boolean){this.selected=checked?new Set(this.candidates.map(r=>r.key)):new Set();}
  get chosen(){return this.candidates.filter(r=>this.selected.has(r.key));}
  get requiredTotal(){return this.chosen.reduce((s,r)=>s+r.required,0);}
  get proposedTotal(){return this.chosen.reduce((s,r)=>s+r.purchase,0);}
  saveDetails(){
    if(!this.orderReference||this.unitPrice===null||!this.selected.size||this.saving||!this.session.current()?.canWriteDatabase)return;
    this.error='';this.message='';this.saving=true;
    const reference={...this.orderReference};
    this.api.addDetails(reference,this.loadedProductionOrder,this.loadedItemCode,Array.from(this.selected),this.unitPrice).pipe(finalize(()=>this.saving=false)).subscribe({next:result=>{this.clearPending();this.message=`Detalle registrado: ${result.requirementsAdded} requerimientos, ${result.itemsAdded} secuencias nuevas. C. requerida: ${result.requiredTotal}; C. compra registrada: ${result.purchaseTotal}.`;this.detailsSaved.emit(reference);},error:e=>{this.error=apiError(e);if(e.status===409)this.clearPending();}});
  }
}
