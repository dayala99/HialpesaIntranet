import { MatDialog } from '@angular/material/dialog';
import { Component, EventEmitter, Input, OnInit, Output, inject } from '@angular/core';
import { finalize, timer, Subject, debounceTime, switchMap, catchError, of, from, mergeMap, map, toArray } from 'rxjs';
import { DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PurchaseOrdersApiService } from '../../services/purchase-orders-api.service';
import { apiError } from '../../../../core/http/api-error';
import { SessionService } from '../../../../core/auth/session.service';
import { CreateOrderRequest, LookupOption, OrderReference, PendingRequirement, RegistrationCatalogs } from '../../models/purchase-order.models';
@Component({ selector: 'app-order-registration', templateUrl: './order-registration.component.html', styleUrl: './order-registration.component.scss' })
export class OrderRegistrationComponent implements OnInit {
    @Input()
    editReference: OrderReference | null = null;
    @Output()
    orderUpdated = new EventEmitter<OrderReference>();
    @Input()
    returnLabel = 'Volver al listado';
    @Input()
    orderReference: OrderReference | null = null;
    @Output()
    orderCreated = new EventEmitter<OrderReference>();
    @Output()
    detailsSaved = new EventEmitter<OrderReference>();
    @Output()
    printRequested = new EventEmitter<OrderReference>();
    @Output()
    closed = new EventEmitter<void>();
    api = inject(PurchaseOrdersApiService);
    session = inject(SessionService);
    catalogs: RegistrationCatalogs | null = null;
    supplierSearch = '';
    suppliers: LookupOption[] = [];
    busy = false;
    looking = false;
    error = '';
    message = '';
    saving = false;
    detailGroups:string[]=[];productionOrders:string[]=[];itemCodes:string[]=[];
    detailGroupOptions:LookupOption[]=[];opOptions:LookupOption[]=[];itemOptions:LookupOption[]=[];
    opsLoading=false;itemsLoading=false;
    private selectorVersion=0;private groupsByOp=new Map<string,string[]>();private validPairs=new Set<string>();
    candidates:PendingRequirement[]=[];selected=new Set<string>();prices:Record<string,number|null>={};
    private pair(op:string,item:string){return op+'|'+item;}
    groupsFor(op:string){return (this.groupsByOp.get(op)??[]).join(', ');}
    changeDetailGroups(values:string[]){
        this.detailGroups=values;this.productionOrders=[];this.itemCodes=[];this.opOptions=[];this.itemOptions=[];this.groupsByOp.clear();this.validPairs.clear();this.clearPending();this.error='';this.itemsLoading=false;
        const version=++this.selectorVersion;if(!this.orderReference||!values.length){this.opsLoading=false;return;}const order=this.orderReference;this.opsLoading=true;
        from(values).pipe(mergeMap(group=>this.api.detailSelector(order,group).pipe(map(rows=>({group,rows}))),4),toArray(),takeUntilDestroyed(this.destroyRef)).subscribe({next:results=>{if(version!==this.selectorVersion)return;const options=new Map<string,LookupOption>();for(const result of results)for(const row of result.rows){options.set(row.code,row);const groups=this.groupsByOp.get(row.code)??[];if(!groups.includes(result.group))groups.push(result.group);this.groupsByOp.set(row.code,groups);}this.opOptions=[...options.values()].sort((a,b)=>a.code.localeCompare(b.code));this.opsLoading=false;},error:e=>{if(version===this.selectorVersion){this.opsLoading=false;this.error=apiError(e);}}});
    }
    changeDetailOps(values:string[]){
        this.productionOrders=values;this.itemCodes=[];this.itemOptions=[];this.validPairs.clear();this.clearPending();this.error='';const version=++this.selectorVersion;
        if(!this.orderReference||!values.length){this.itemsLoading=false;return;}const order=this.orderReference;const pairs=values.flatMap(op=>(this.groupsByOp.get(op)??[]).map(group=>({op,group})));this.itemsLoading=true;
        from(pairs).pipe(mergeMap(pair=>this.api.detailSelector(order,pair.group,pair.op).pipe(map(rows=>({op:pair.op,rows}))),4),toArray(),takeUntilDestroyed(this.destroyRef)).subscribe({next:results=>{if(version!==this.selectorVersion)return;const options=new Map<string,LookupOption>();for(const result of results)for(const row of result.rows){options.set(row.code,row);this.validPairs.add(this.pair(result.op,row.code));}this.itemOptions=[...options.values()].sort((a,b)=>a.code.localeCompare(b.code));this.itemsLoading=false;},error:e=>{if(version===this.selectorVersion){this.itemsLoading=false;this.error=apiError(e);}}});
    }
    changeDetailItems(values:string[]){this.itemCodes=values;this.clearPending();}
    private today() { const date = new Date(); return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`; }
    header: CreateOrderRequest = { classCode: '', supplierCode: '', conditionCode: '', discountCode: '', currencyCode: '', deliveryLocationCode: '', costCenterCode: '', manufacturingGroup: '', taxPercent: 18, deliveryStart: this.today(), deliveryEnd: this.today(), observations: '' };
    private destroyRef = inject(DestroyRef);
    private supplierQueries = new Subject<string>();
    supplierDefaultsLoading = false;
    supplierDefaultsError = '';
    private supplierVersion = 0;
    supplierOpen = false;
    supplierLoading = false;
    supplierError = '';
    supplierDone = false;
    activeSupplier = -1;
    filterSuppliers(value: string) { this.supplierVersion++; this.supplierDefaultsLoading = false; this.supplierDefaultsError = ''; this.header.conditionCode = ''; this.header.discountCode = ''; this.header.taxPercent = null; this.supplierSearch = value; this.header.supplierCode = ''; this.suppliers = []; this.supplierError = ''; this.supplierDone = false; this.activeSupplier = -1; this.supplierOpen = true; this.supplierLoading = value.trim().length >= 2; this.supplierQueries.next(value.trim()); }
    selectSupplier(supplier: LookupOption) {
        this.header.supplierCode = supplier.code;
        this.supplierSearch = supplier.code + ' · ' + supplier.description;
        this.supplierOpen = false;
        this.supplierQueries.next('');
        const version = ++this.supplierVersion;
        this.supplierDefaultsLoading = true;
        this.supplierDefaultsError = '';
        this.header.conditionCode = '';
        this.header.discountCode = '';
        this.header.taxPercent = null;
        this.api.supplierDefaults(supplier.code).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ next: values => { if (version !== this.supplierVersion)
                return; this.header.conditionCode = values.conditionCode; this.header.discountCode = values.discountCode; this.header.taxPercent = values.taxPercent; this.supplierDefaultsLoading = false; if (values.taxPercent === null)
                this.supplierDefaultsError = 'El proveedor no tiene origen Nacional o Extranjero configurado. Revisa el IGV antes de guardar.'; }, error: e => { if (version === this.supplierVersion) {
                this.supplierDefaultsLoading = false;
                this.supplierDefaultsError = apiError(e);
                this.header.supplierCode = '';
            } } });
    }
    supplierKey(event: KeyboardEvent) {
        if (event.key === 'Escape') {
            this.supplierOpen = false;
            event.preventDefault();
        }
        else if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
            event.preventDefault();
            this.supplierOpen = true;
            const n = this.suppliers.length;
            if (n)
                this.activeSupplier = (this.activeSupplier + (event.key === 'ArrowDown' ? 1 : -1) + n) % n;
        }
        else if (event.key === 'Enter' && this.supplierOpen) {
            event.preventDefault();
            if (this.activeSupplier >= 0 && this.suppliers[this.activeSupplier])
                this.selectSupplier(this.suppliers[this.activeSupplier]);
        }
    }
    ngOnInit() {
        this.supplierQueries.pipe(switchMap(query => query.length < 2 ? of(null) : timer(300).pipe(switchMap(() => this.api.lookup('proveedores', query)), catchError(e => { this.supplierError = apiError(e); return of([] as LookupOption[]); }))), takeUntilDestroyed(this.destroyRef)).subscribe(rows => { this.supplierLoading = false; if (rows) {
            this.suppliers = rows;
            this.supplierDone = true;
            this.activeSupplier = -1;
        } });
        if (!this.orderReference)
            this.loadCatalogs();
        if (this.orderReference) {
            this.busy = true;
            this.api.detailSelector(this.orderReference).pipe(finalize(() => this.busy = false), takeUntilDestroyed(this.destroyRef)).subscribe({ next: rows => this.detailGroupOptions = rows, error: e => this.error = apiError(e) });
        }
        if (this.editReference) {
            this.busy = true;
            this.api.editableOrder(this.editReference).pipe(finalize(() => this.busy = false), takeUntilDestroyed(this.destroyRef)).subscribe({ next: data => { this.header = { ...this.header, ...data.header, classCode: '01' }; this.header.deliveryStart = data.header.deliveryStart?.slice(0, 10); this.header.deliveryEnd = data.header.deliveryEnd?.slice(0, 10); this.supplierSearch = this.header.supplierCode + ' · ' + (data.supplierName ?? ''); }, error: e => this.error = apiError(e) });
        }
    }
    loadCatalogs() { this.busy = true; this.error = ''; this.api.registrationCatalogs().pipe(finalize(() => this.busy = false)).subscribe({ next: data => this.catalogs = data, error: e => this.error = apiError(e) }); }
    create() {
        if (this.saving || !this.session.current()?.canWriteDatabase)
            return;
        this.saving = true;
        this.error = '';
        this.message = '';
        if (this.editReference) {
            const reference = { ...this.editReference };
            this.api.updateOrder(reference, { ...this.header }).pipe(finalize(() => this.saving = false)).subscribe({ next: () => this.orderUpdated.emit(reference), error: e => this.error = apiError(e) });
            return;
        }
        this.api.createOrder({ ...this.header }).pipe(finalize(() => this.saving = false)).subscribe({ next: order => { this.orderCreated.emit(order); this.message = `OC ${order.series}-${order.number} guardada correctamente.`; }, error: e => this.error = apiError(e) });
    }
    clearPending(){this.candidates=[];this.selected=new Set<string>();this.prices={};this.message='';}
    pending(){
        if(!this.orderReference||this.busy||this.saving||this.opsLoading||this.itemsLoading)return;
        this.error='';this.clearPending();const order=this.orderReference;
        const pairs=this.productionOrders.flatMap(op=>this.itemCodes.filter(item=>this.validPairs.has(this.pair(op,item))).map(item=>({op,item})));
        if(!pairs.length||pairs.length>500){this.error='Selecciona entre 1 y 500 combinaciones válidas de NP e ítem.';return;}
        this.busy=true;
        from(pairs).pipe(mergeMap(pair=>this.api.pending(order,pair.op,pair.item),4),toArray(),finalize(()=>this.busy=false),takeUntilDestroyed(this.destroyRef)).subscribe({next:results=>{
            this.candidates=[...new Map(results.flat().map(row=>[row.key,row])).values()].sort((a,b)=>a.itemCode.localeCompare(b.itemCode)||a.productionOrder.localeCompare(b.productionOrder));
            this.selected=new Set(this.candidates.map(row=>row.key));
            for(const item of new Set(this.candidates.map(row=>row.itemCode))){const prices=[...new Set(this.candidates.filter(row=>row.itemCode===item).map(row=>row.unitPrice))];this.prices[item]=prices.length===1?prices[0]:null;}
            if(!this.candidates.length)this.message='No hay requerimientos pendientes para la selección.';
        },error:e=>this.error=apiError(e)});
    }
    toggle(key:string,checked:boolean){if(checked)this.selected.add(key);else this.selected.delete(key);}
    toggleAll(checked:boolean){this.selected=checked?new Set(this.candidates.map(row=>row.key)):new Set<string>();}
    get chosen(){return this.candidates.filter(row=>this.selected.has(row.key));}
    get requiredTotal(){return this.chosen.reduce((sum,row)=>sum+row.required,0);}
    get proposedTotal(){return this.chosen.reduce((sum,row)=>sum+row.purchase,0);}
    get priceItems(){return this.itemOptions.filter(item=>this.candidates.some(row=>row.itemCode===item.code&&this.selected.has(row.key)));}
    get canSaveDetails(){return !!this.orderReference&&!this.saving&&!this.busy&&!!this.session.current()?.canWriteDatabase&&this.selected.size>0&&this.selected.size<=500&&this.priceItems.every(item=>this.prices[item.code]!=null&&Number.isFinite(this.prices[item.code])&&this.prices[item.code]!>=0&&this.prices[item.code]!<=99999999);}
    saveDetails(){
        if(!this.canSaveDetails||!this.orderReference)return;this.error='';this.message='';this.saving=true;const reference={...this.orderReference};
        const pairs=new Map<string,{productionOrder:string;itemCode:string;requirementKeys:string[];unitPrice:number}>();
        for(const row of this.chosen){const key=this.pair(row.productionOrder,row.itemCode);const line=pairs.get(key)??{productionOrder:row.productionOrder,itemCode:row.itemCode,requirementKeys:[],unitPrice:this.prices[row.itemCode]!};line.requirementKeys.push(row.key);pairs.set(key,line);}
        this.api.addDetailsBatch(reference,[...pairs.values()]).pipe(finalize(()=>this.saving=false),takeUntilDestroyed(this.destroyRef)).subscribe({next:result=>{this.clearPending();this.message=`Lote registrado: ${result.requirementsAdded} requerimientos, ${result.itemsAdded} secuencias nuevas. C. requerida: ${result.requiredTotal}; C. compra registrada: ${result.purchaseTotal}.`;this.detailsSaved.emit(reference);},error:e=>{this.error=apiError(e);if(e.status===409){this.clearPending();this.error=apiError(e);}}});
    }
}
