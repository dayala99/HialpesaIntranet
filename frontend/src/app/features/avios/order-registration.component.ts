import { MatDialog } from '@angular/material/dialog';
import { Component, TemplateRef, ViewChild, EventEmitter, Input, OnInit, Output, inject } from '@angular/core';
import { finalize, timer, Subject, debounceTime, switchMap, catchError, of } from 'rxjs';
import { DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AviosApi, apiError } from '../../core/avios-api.service';
import { SessionService } from '../../core/session.service';
import { CreateOrderRequest, LookupOption, OrderReference, PendingRequirement, RegistrationCatalogs } from '../../core/models';

@Component({ selector:'app-order-registration',templateUrl:'./order-registration.component.html',styleUrl:'./order-registration.component.scss' })
export class OrderRegistrationComponent implements OnInit {
  @Input() editReference:OrderReference|null=null;
  @Output() orderUpdated=new EventEmitter<OrderReference>();
  @Input() groupOnly=false;
  @Output() groupsSaved=new EventEmitter<void>();
  groupTarget:OrderReference|null=null;groupSaving=false;
  originalGroups:string[]=[];groupsLoading=false;groupsLoaded=false;
  get addedGroupCodes(){return this.selectedManufacturingGroups.map(g=>g.code).filter(c=>!this.originalGroups.includes(c));}
  get removedGroupCodes(){return this.originalGroups.filter(c=>!this.selectedManufacturingGroups.some(g=>g.code===c));}
  get groupChanges(){return this.addedGroupCodes.length+this.removedGroupCodes.length;}
  editGroups(order:OrderReference){
    this.groupTarget=order;this.selectedManufacturingGroups=[];this.originalGroups=[];this.groupsLoaded=false;this.groupsLoading=true;this.openManufacturingGroup();
    this.api.orderGroups(order).pipe(finalize(()=>this.groupsLoading=false),takeUntilDestroyed(this.destroyRef)).subscribe({next:rows=>{this.selectedManufacturingGroups=rows;this.originalGroups=rows.map(g=>g.code);this.groupsLoaded=true;},error:e=>this.opGroupError=apiError(e)});
  }
  saveGroups(){if(!this.groupTarget||this.groupSaving||!this.groupsLoaded||!this.groupChanges)return;this.groupSaving=true;this.opGroupError='';this.api.addManufacturingGroups(this.groupTarget,this.addedGroupCodes,this.removedGroupCodes).pipe(finalize(()=>this.groupSaving=false)).subscribe({next:()=>{this.originalGroups=this.selectedManufacturingGroups.map(g=>g.code);this.groupsSaved.emit();},error:e=>this.opGroupError=apiError(e)});}
  @Input() returnLabel='Volver al listado';
  @Input() orderReference: OrderReference | null=null;
  @Output() orderCreated=new EventEmitter<OrderReference>();
  @Output() detailsSaved=new EventEmitter<OrderReference>();
  @Output() printRequested=new EventEmitter<OrderReference>();
  @Output() closed=new EventEmitter<void>();
  api=inject(AviosApi); session=inject(SessionService);
  catalogs: RegistrationCatalogs | null=null;
  supplierSearch='';groupSearch='';suppliers:LookupOption[]=[];groups:LookupOption[]=[];
  private dialog=inject(MatDialog);
  @ViewChild('manufacturingGroupDialog') manufacturingGroupDialog!:TemplateRef<unknown>;
  factoryOptions:LookupOption[]=[];factoryCode='HIA';groupOp='';opGroups:LookupOption[]=[];opGroupBusy=false;opGroupError='';opGroupSearched=false;
  openManufacturingGroup(){
    this.groupOp='';this.factoryCode='HIA';this.opGroups=[];this.opGroupError='';this.opGroupSearched=false;this.groupOpen=false;
    this.dialog.open(this.manufacturingGroupDialog,{width:'600px',maxWidth:'95vw',restoreFocus:true,autoFocus:'#manufacturing-group-op'});
    this.api.factories().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({next:rows=>{this.factoryOptions=rows;this.factoryCode=rows.find(r=>r.code.trim().toUpperCase()==='HIA')?.code??'HIA';},error:e=>this.opGroupError=apiError(e)});
  }
  resetOpGroups(){this.opGroups=[];this.opGroupSearched=false;this.opGroupError='';}
  searchManufacturingGroup(){
    if(this.opGroupBusy)return;
    this.resetOpGroups();const factory=this.factoryCode.trim(),op=this.groupOp.trim().toUpperCase();
    if(!factory||!/^[A-Z0-9]{5}$/.test(op)){this.opGroupError='Selecciona una fábrica e ingresa una OP de 5 caracteres.';return;}
    this.opGroupBusy=true;
    this.api.groupsByOp(factory,op).pipe(takeUntilDestroyed(this.destroyRef),finalize(()=>this.opGroupBusy=false)).subscribe({next:rows=>{this.opGroups=rows;this.opGroupSearched=true;},error:e=>this.opGroupError=apiError(e)});
  }
  chooseManufacturingGroup(group:LookupOption){if(!this.groupSaving&&!this.groupsLoading)this.selectGroup(group);}
  selectedManufacturingGroups:LookupOption[]=[];
  groupLookupDone=false;
  busy=false;looking=false;error='';message='';saving=false;
  detailGroup='';detailGroupOptions:LookupOption[]=[];opOptions:LookupOption[]=[];itemOptions:LookupOption[]=[];opsLoading=false;itemsLoading=false;
  changeDetailGroup(value:string){this.detailGroup=value;this.productionOrder='';this.itemCode='';this.opOptions=[];this.itemOptions=[];this.clearPending();this.error='';this.itemsLoading=false;if(!this.orderReference||!value){this.opsLoading=false;return;}this.opsLoading=true;this.api.detailSelector(this.orderReference,value).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({next:rows=>{if(this.detailGroup===value){this.opOptions=rows;this.opsLoading=false;}},error:e=>{if(this.detailGroup===value){this.error=apiError(e);this.opsLoading=false;}}});}
  changeDetailOp(value:string){this.productionOrder=value;this.itemCode='';this.itemOptions=[];this.clearPending();this.error='';if(!this.orderReference||!value){this.itemsLoading=false;return;}const group=this.detailGroup;this.itemsLoading=true;this.api.detailSelector(this.orderReference,group,value).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({next:rows=>{if(this.detailGroup===group&&this.productionOrder===value){this.itemOptions=rows;this.itemsLoading=false;}},error:e=>{if(this.detailGroup===group&&this.productionOrder===value){this.error=apiError(e);this.itemsLoading=false;}}});}
  productionOrder='';itemCode='';unitPrice:number|null=null;
  candidates:PendingRequirement[]=[];selected=new Set<string>();
  loadedProductionOrder='';loadedItemCode='';
  private today(){const date=new Date();return `${date.getFullYear()}-${String(date.getMonth()+1).padStart(2,'0')}-${String(date.getDate()).padStart(2,'0')}`;}
  header:CreateOrderRequest={classCode:'',supplierCode:'',conditionCode:'',discountCode:'',currencyCode:'',deliveryLocationCode:'',costCenterCode:'',manufacturingGroup:'',taxPercent:18,deliveryStart:this.today(),deliveryEnd:this.today(),observations:''};
  private destroyRef=inject(DestroyRef);
  private supplierQueries=new Subject<string>();
  supplierDefaultsLoading=false;supplierDefaultsError='';private supplierVersion=0;
  supplierOpen=false;supplierLoading=false;supplierError='';supplierDone=false;activeSupplier=-1;
  filterSuppliers(value:string){this.supplierVersion++;this.supplierDefaultsLoading=false;this.supplierDefaultsError='';this.header.conditionCode='';this.header.discountCode='';this.header.taxPercent=null;this.supplierSearch=value;this.header.supplierCode='';this.suppliers=[];this.supplierError='';this.supplierDone=false;this.activeSupplier=-1;this.supplierOpen=true;this.supplierLoading=value.trim().length>=2;this.supplierQueries.next(value.trim());}
  selectSupplier(supplier:LookupOption){
    this.header.supplierCode=supplier.code;this.supplierSearch=supplier.code+' · '+supplier.description;this.supplierOpen=false;this.supplierQueries.next('');
    const version=++this.supplierVersion;this.supplierDefaultsLoading=true;this.supplierDefaultsError='';this.header.conditionCode='';this.header.discountCode='';this.header.taxPercent=null;
    this.api.supplierDefaults(supplier.code).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({next:values=>{if(version!==this.supplierVersion)return;this.header.conditionCode=values.conditionCode;this.header.discountCode=values.discountCode;this.header.taxPercent=values.taxPercent;this.supplierDefaultsLoading=false;if(values.taxPercent===null)this.supplierDefaultsError='El proveedor no tiene origen Nacional o Extranjero configurado. Revisa el IGV antes de guardar.';},error:e=>{if(version===this.supplierVersion){this.supplierDefaultsLoading=false;this.supplierDefaultsError=apiError(e);this.header.supplierCode='';}}});
  }
  supplierKey(event:KeyboardEvent){
    if(event.key==='Escape'){this.supplierOpen=false;event.preventDefault();}
    else if(event.key==='ArrowDown'||event.key==='ArrowUp'){event.preventDefault();this.supplierOpen=true;const n=this.suppliers.length;if(n)this.activeSupplier=(this.activeSupplier+(event.key==='ArrowDown'?1:-1)+n)%n;}
    else if(event.key==='Enter'&&this.supplierOpen){event.preventDefault();if(this.activeSupplier>=0&&this.suppliers[this.activeSupplier])this.selectSupplier(this.suppliers[this.activeSupplier]);}
  }
  private groupQueries=new Subject<string>();
  groupOpen=false;groupLoading=false;groupError='';activeGroup=-1;
  ngOnInit(){
    this.supplierQueries.pipe(switchMap(query=>query.length<2?of(null):timer(300).pipe(switchMap(()=>this.api.lookup('proveedores',query)),catchError(e=>{this.supplierError=apiError(e);return of([] as LookupOption[]);}))),takeUntilDestroyed(this.destroyRef)).subscribe(rows=>{this.supplierLoading=false;if(rows){this.suppliers=rows;this.supplierDone=true;this.activeSupplier=-1;}});

    this.groupQueries.pipe(debounceTime(250),switchMap(query=>{
      this.groupLoading=true;this.groupError='';
      return this.api.lookup('grupos',query).pipe(catchError(e=>{this.groupError=apiError(e);return of([] as LookupOption[]);}));
    }),takeUntilDestroyed(this.destroyRef)).subscribe(rows=>{this.groups=rows;this.groupLookupDone=true;this.groupLoading=false;this.activeGroup=-1;});
    if(!this.orderReference&&!this.groupOnly)this.loadCatalogs();
    if(this.orderReference&&!this.groupOnly){this.busy=true;this.api.detailSelector(this.orderReference).pipe(finalize(()=>this.busy=false),takeUntilDestroyed(this.destroyRef)).subscribe({next:rows=>this.detailGroupOptions=rows,error:e=>this.error=apiError(e)});}
    if(this.editReference){this.busy=true;this.api.editableOrder(this.editReference).pipe(finalize(()=>this.busy=false),takeUntilDestroyed(this.destroyRef)).subscribe({next:data=>{this.header={...this.header,...data.header,classCode:'01'};this.header.deliveryStart=data.header.deliveryStart?.slice(0,10);this.header.deliveryEnd=data.header.deliveryEnd?.slice(0,10);this.supplierSearch=this.header.supplierCode+' · '+(data.supplierName??'');},error:e=>this.error=apiError(e)});}

  }
  filterGroups(value:string){this.groupSearch=value;this.header.manufacturingGroup=this.selectedManufacturingGroups[0]?.code??'';this.groups=[];this.groupLookupDone=false;this.groupLoading=true;this.groupOpen=true;this.groupQueries.next(value.trim());}
  openGroups(){this.groupOpen=true;this.groupQueries.next(this.header.manufacturingGroup||this.groupSearch.trim());}
  selectGroup(group:LookupOption){
    if(!this.selectedManufacturingGroups.some(g=>g.code===group.code))this.selectedManufacturingGroups.push(group);
    this.syncManufacturingGroups();this.groupOpen=false;this.activeGroup=-1;
  }
  hasManufacturingGroup(code:string){return this.selectedManufacturingGroups.some(g=>g.code===code);}
  removeManufacturingGroup(code:string){if(this.groupSaving||this.groupsLoading)return;this.selectedManufacturingGroups=this.selectedManufacturingGroups.filter(g=>g.code!==code);this.syncManufacturingGroups();}
  private syncManufacturingGroups(){this.header.manufacturingGroup=this.selectedManufacturingGroups[0]?.code??'';this.header.additionalManufacturingGroups=this.selectedManufacturingGroups.slice(1).map(g=>g.code);this.groupSearch=this.header.manufacturingGroup;}
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
    if(this.editReference){const reference={...this.editReference};this.api.updateOrder(reference,{...this.header}).pipe(finalize(()=>this.saving=false)).subscribe({next:()=>this.orderUpdated.emit(reference),error:e=>this.error=apiError(e)});return;}
    this.api.createOrder({...this.header}).pipe(finalize(()=>this.saving=false)).subscribe({next:order=>{this.orderCreated.emit(order);this.message=`OC ${order.series}-${order.number} guardada correctamente.`;},error:e=>this.error=apiError(e)});
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
