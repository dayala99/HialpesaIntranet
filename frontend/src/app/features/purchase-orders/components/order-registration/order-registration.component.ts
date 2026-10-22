import { MatDialog } from '@angular/material/dialog';
import { Component, EventEmitter, Input, OnInit, Output, inject } from '@angular/core';
import { finalize, timer, Subject, debounceTime, switchMap, catchError, of } from 'rxjs';
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
    detailGroup = '';
    detailGroupOptions: LookupOption[] = [];
    opOptions: LookupOption[] = [];
    itemOptions: LookupOption[] = [];
    opsLoading = false;
    itemsLoading = false;
    changeDetailGroup(value: string) { this.detailGroup = value; this.productionOrder = ''; this.itemCode = ''; this.opOptions = []; this.itemOptions = []; this.clearPending(); this.error = ''; this.itemsLoading = false; if (!this.orderReference || !value) {
        this.opsLoading = false;
        return;
    } this.opsLoading = true; this.api.detailSelector(this.orderReference, value).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ next: rows => { if (this.detailGroup === value) {
            this.opOptions = rows;
            this.opsLoading = false;
        } }, error: e => { if (this.detailGroup === value) {
            this.error = apiError(e);
            this.opsLoading = false;
        } } }); }
    changeDetailOp(value: string) { this.productionOrder = value; this.itemCode = ''; this.itemOptions = []; this.clearPending(); this.error = ''; if (!this.orderReference || !value) {
        this.itemsLoading = false;
        return;
    } const group = this.detailGroup; this.itemsLoading = true; this.api.detailSelector(this.orderReference, group, value).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ next: rows => { if (this.detailGroup === group && this.productionOrder === value) {
            this.itemOptions = rows;
            this.itemsLoading = false;
        } }, error: e => { if (this.detailGroup === group && this.productionOrder === value) {
            this.error = apiError(e);
            this.itemsLoading = false;
        } } }); }
    productionOrder = '';
    itemCode = '';
    unitPrice: number | null = null;
    candidates: PendingRequirement[] = [];
    selected = new Set<string>();
    loadedProductionOrder = '';
    loadedItemCode = '';
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
    clearPending() { this.candidates = []; this.selected.clear(); this.unitPrice = null; this.loadedProductionOrder = ''; this.loadedItemCode = ''; this.message = ''; }
    pending() {
        if (!this.orderReference)
            return;
        this.error = '';
        this.message = '';
        this.clearPending();
        const op = this.productionOrder.trim(), item = this.itemCode.trim();
        if (!/^[A-Za-z0-9]{5}$/.test(op) || !/^[A-Za-z0-9]{8}$/.test(item)) {
            this.error = 'Ingresa una OP de 5 caracteres y un código de ítem de 8 caracteres.';
            return;
        }
        this.busy = true;
        this.api.pending(this.orderReference, op, item).pipe(finalize(() => this.busy = false)).subscribe({ next: rows => { this.candidates = rows; this.loadedProductionOrder = op; this.loadedItemCode = item; this.selected = new Set(rows.map(r => r.key)); this.unitPrice = rows[0]?.unitPrice ?? null; if (!rows.length)
                this.message = 'No hay requerimientos pendientes para esta OP e ítem.'; }, error: e => this.error = apiError(e) });
    }
    toggle(key: string, checked: boolean) { if (checked)
        this.selected.add(key);
    else
        this.selected.delete(key); }
    toggleAll(checked: boolean) { this.selected = checked ? new Set(this.candidates.map(r => r.key)) : new Set(); }
    get chosen() { return this.candidates.filter(r => this.selected.has(r.key)); }
    get requiredTotal() { return this.chosen.reduce((s, r) => s + r.required, 0); }
    get proposedTotal() { return this.chosen.reduce((s, r) => s + r.purchase, 0); }
    saveDetails() {
        if (!this.orderReference || this.unitPrice === null || !this.selected.size || this.saving || !this.session.current()?.canWriteDatabase)
            return;
        this.error = '';
        this.message = '';
        this.saving = true;
        const reference = { ...this.orderReference };
        this.api.addDetails(reference, this.loadedProductionOrder, this.loadedItemCode, Array.from(this.selected), this.unitPrice).pipe(finalize(() => this.saving = false)).subscribe({ next: result => { this.clearPending(); this.message = `Detalle registrado: ${result.requirementsAdded} requerimientos, ${result.itemsAdded} secuencias nuevas. C. requerida: ${result.requiredTotal}; C. compra registrada: ${result.purchaseTotal}.`; this.detailsSaved.emit(reference); }, error: e => { this.error = apiError(e); if (e.status === 409)
                this.clearPending(); } });
    }
}
