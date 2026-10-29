import { ConfirmationDialogComponent, ConfirmationDialogData } from '../../../../shared/components/dialogs/confirmation-dialog/confirmation-dialog.component';
import { RequirementLinksDialogComponent, RequirementLinksDialogData, RequirementLinksDialogResult } from '../../components/dialogs/requirement-links-dialog/requirement-links-dialog.component';
import { ItemEditDialogComponent, ItemEditDialogData, ItemEditDialogResult } from '../../components/dialogs/item-edit-dialog/item-edit-dialog.component';
import { PoDetailDialogComponent, PoDetailDialogData, PoDetailDialogResult } from '../../components/dialogs/po-detail-dialog/po-detail-dialog.component';
import { ManufacturingGroupsDialogComponent, ManufacturingGroupsDialogData, ManufacturingGroupsDialogResult } from '../../components/dialogs/manufacturing-groups-dialog/manufacturing-groups-dialog.component';
import { PoSelectionDialogComponent, PoSelectionDialogData, PoSelectionDialogResult } from '../../../provider/components/dialogs/po-selection-dialog/po-selection-dialog.component';
import { AgentLauncherService } from '../../../provider/services/agent-launcher.service';
import { AgentTicket } from '../../../provider/models/provider.models';
import { PoApiService } from '../../services/po-api.service';
import { ProviderApiService } from '../../../provider/services/provider-api.service';
import { environment } from '../../../../../environments/environment';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';
import { Component, DestroyRef, inject } from '@angular/core';
import { finalize, Subject, switchMap, timer, catchError, of, takeUntil } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PurchaseOrdersApiService } from '../../services/purchase-orders-api.service';
import { apiError } from '../../../../core/http/api-error';
import { PoDetailLine, EditableOrderItem, LinkedRequirement } from '../../models/purchase-order.models';
import { SessionService } from '../../../../core/auth/session.service';
import { ArticleRule, MatrixRow, OrderDetail, OrderItem, Preview, Requirement, OrderReference } from '../../models/purchase-order.models';
import { ExcelService } from '../../services/excel.service';
import { SupplierDemoService } from '../../../provider/services/supplier-demo.service';
@Component({ selector: 'app-avios', templateUrl: './avios.component.html', styleUrl: './avios.component.scss' })
export class AviosComponent {
    private agentLauncher = inject(AgentLauncherService);
    openManufacturingGroups(order: OrderReference) { this.dialog.open<ManufacturingGroupsDialogComponent, ManufacturingGroupsDialogData, ManufacturingGroupsDialogResult>(ManufacturingGroupsDialogComponent, { width: '600px', maxWidth: '95vw', restoreFocus: true, autoFocus: '#manufacturing-group-op', data: { order } }).afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(result => { if (result?.saved)
        this.gridMessage = 'Grupos de manufactura guardados correctamente.'; }); }
    private poApi = inject(PoApiService);
    private providerApi = inject(ProviderApiService);
    poQuery = '';
    poFilter = '';
    poPage = 1;
    poTotalPages = 1;
    poTotalRows = 0;
    poBusy = false;
    poError = '';
    poHasMore = false;
    poRows: {
        po: string;
        clientCode: string;
        client: string;
        description: string;
        createdAt: string;
    }[] = [];
    private poRequests = new Subject<{
        page: number;
        po: string;
        search: string;
    }>();
    openPo() { this.tab = 'pos'; this.loadPos(); }
    loadPos(page = 1) { this.poBusy = true; this.poError = ''; this.poRequests.next({ page, po: this.poQuery.trim(), search: this.poFilter.trim() }); }
    private route = inject(ActivatedRoute);
    private router = inject(Router);
    private destroyRef = inject(DestroyRef);
    private gridRequests = new Subject<{
        page: number;
        search: string;
    }>();
    ngOnInit() {
        this.poRequests.pipe(switchMap(q => timer(300).pipe(switchMap(() => this.poApi.listPos(q.page, q.po, q.search)), catchError(e => { this.poError = apiError(e); return of(null); }))), takeUntilDestroyed(this.destroyRef)).subscribe(r => { this.poBusy = false; if (r) {
            this.poRows = r.rows;
            this.poPage = r.page;
            this.poTotalPages = r.totalPages;
            this.poTotalRows = r.totalRows;
            this.poHasMore = r.hasMore;
        }
        else {
            this.poRows = [];
        } });
        this.gridRequests.pipe(switchMap(q => timer(300).pipe(switchMap(() => this.api.listOrders(q.page, q.search)), catchError(e => { this.gridError = apiError(e); return of(null); }))), takeUntilDestroyed(this.destroyRef)).subscribe(r => {
            this.gridBusy = false;
            if (r) {
                this.gridRows = r.rows;
                this.gridHasMore = r.hasMore;
                this.gridPage = r.page;
                this.gridTotalPages = r.totalPages ?? 1;
            }
        });
        if (this.registrationPage())
            this.loadOrderGrid();
    }
    viewLinkedRequirements(item: OrderItem) { if (!this.order)
        return; this.dialog.open<RequirementLinksDialogComponent, RequirementLinksDialogData, RequirementLinksDialogResult>(RequirementLinksDialogComponent, { width: '900px', maxWidth: '96vw', disableClose: true, data: { order: this.order.order, sequence: item.sequence, status: this.order.order.status } }).afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(result => { if (result?.changed) {
        this.detailMessage = 'Requerimientos retirados correctamente. Se actualizó el detalle de la OC.';
        this.loadRegistrationOrder();
    } }); }
    async modifyItem(item: OrderItem) { if (!this.order)
        return; this.error = ''; this.busy = true; try {
        const { firstValueFrom } = await import('rxjs');
        const order = this.order.order;
        const [data, catalogs] = await Promise.all([firstValueFrom(this.api.editableItem(order, item.sequence)), firstValueFrom(this.api.registrationCatalogs())]);
        this.dialog.open<ItemEditDialogComponent, ItemEditDialogData, ItemEditDialogResult>(ItemEditDialogComponent, { width: '650px', maxWidth: '95vw', disableClose: true, data: { order, sequence: item.sequence, item: { ...data, deliveryStart: data.deliveryStart?.slice(0, 10), deliveryEnd: data.deliveryEnd?.slice(0, 10) }, discounts: catalogs.discounts } }).afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(result => { if (result?.saved) {
            this.detailMessage = 'Secuencia actualizada correctamente.';
            this.loadRegistrationOrder();
        } });
    }
    catch (e) {
        this.error = apiError(e);
    }
    finally {
        this.busy = false;
    } }
    async removeItem(item: OrderItem) { if (!this.order)
        return; const { firstValueFrom } = await import('rxjs'); const confirmed = await firstValueFrom(this.dialog.open<ConfirmationDialogComponent, ConfirmationDialogData, boolean>(ConfirmationDialogComponent, { width: '460px', maxWidth: '95vw', data: { title: 'Eliminar secuencia ' + item.sequence, message: '¿Deseas eliminar este detalle de la orden de compra?', confirmLabel: 'Eliminar' } }).afterClosed()); if (confirmed !== true)
        return; this.busy = true; this.error = ''; this.api.deleteItem(this.order.order, item.sequence).pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.busy = false)).subscribe({ next: () => { this.detailMessage = 'Secuencia eliminada correctamente.'; this.loadRegistrationOrder(); }, error: e => this.error = apiError(e) }); }
    detailListOpen = false;
    detailMessage = '';
    backToOrders() { this.detailListOpen = false; this.registrationOpen = false; this.registrationReference = null; this.error = ''; this.loadOrderGrid(this.gridPage); }
    closeRegistration() { this.editReference = null; const wasDetail = !!this.registrationReference; this.registrationOpen = false; this.error = ''; if (wasDetail) {
        this.detailListOpen = true;
    }
    else
        this.backToOrders(); }
    registerDetail() { if (this.order) {
        this.registrationReference = { series: this.order.order.series, number: this.order.order.number };
        this.detailListOpen = false;
        this.registrationOpen = true;
    } }
    loadRegistrationOrder() {
        if (!/^\d{3}$/.test(this.series) || !/^\d{6}$/.test(this.number)) {
            this.error = 'Ingresa serie de 3 dígitos y número de 6 dígitos.';
            return;
        }
        this.busy = true;
        this.error = '';
        this.api.order(this.series, this.number).pipe(finalize(() => this.busy = false)).subscribe({ next: detail => {
                this.order = detail;
                this.registrationReference = { series: detail.order.series, number: detail.order.number };
                this.registrationOpen = false;
                this.detailListOpen = true;
            }, error: e => this.error = apiError(e) });
    }
    gridRows: {
        series: string;
        number: string;
        supplier: string;
        status: string;
        class: string;
        createdAt: string;
    }[] = [];
    gridTotalPages = 1;
    gridPage = 1;
    gridSearch = '';
    gridHasMore = false;
    gridBusy = false;
    gridError = '';
    gridMessage = '';
    filterOrderGrid(value: string) { this.gridSearch = value; this.loadOrderGrid(); }
    loadOrderGrid(page = 1) { this.gridBusy = true; this.gridError = ''; this.gridRows = []; this.gridHasMore = false; this.gridRequests.next({ page, search: this.gridSearch.trim() }); }
    selectGridOrder(row: OrderReference) { this.detailMessage = ''; this.series = row.series; this.number = row.number; this.loadRegistrationOrder(); }
    registrationPage() { return !!this.route.snapshot.data['newOrder']; }
    registrationOpen = false;
    editReference: OrderReference | null = null;
    editOrder(row: OrderReference) { this.editReference = { ...row }; this.registrationReference = null; this.registrationOpen = true; this.detailListOpen = false; }
    updatedOrder(row: OrderReference) { this.editReference = null; this.registrationOpen = false; this.gridMessage = 'OC ' + row.series + '-' + row.number + ' actualizada correctamente.'; this.loadOrderGrid(this.gridPage); }
    registrationReference: OrderReference | null = null;
    printReference: OrderReference | null = null;
    openRegistration(reference: OrderReference | null = null) { this.editReference = null; this.detailListOpen = false; this.detailMessage = ''; this.registrationReference = reference; this.registrationOpen = true; this.tab = 'orders'; }
    registered(reference: OrderReference) { this.series = reference.series; this.number = reference.number; this.gridMessage = `Orden de compra ${reference.series}-${reference.number} registrada correctamente.`; this.registrationOpen = false; this.registrationReference = null; this.error = ""; this.loadOrderGrid(); }
    detailsRegistered(reference: OrderReference) { this.series = reference.series; this.number = reference.number; this.detailMessage = `Detalle de la OC ${reference.series}-${reference.number} guardado correctamente.`; this.registrationOpen = false; this.detailListOpen = true; this.loadRegistrationOrder(); }
    printCurrent() { if (this.order)
        void this.exportOrder({ series: this.order.order.series, number: this.order.order.number }); }
    async exportOrder(reference: OrderReference) {
        if (this.exporting)
            return;
        this.exporting = true;
        this.error = '';
        try {
            const { firstValueFrom } = await import('rxjs');
            const data = await firstValueFrom(this.api.printOrder(reference));
            await this.excel.purchaseOrder(data);
        }
        catch (e) {
            this.error = apiError(e);
        }
        finally {
            this.exporting = false;
        }
    }
    private api = inject(PurchaseOrdersApiService);
    private excel = inject(ExcelService);
    private supplierDemo = inject(SupplierDemoService);
    openingSupplierDemo = false;
    private dialog = inject(MatDialog);
    poActionMessage = '';
    viewPo(row: {
        po: string;
        clientCode: string;
    }) { this.poError = ''; this.dialog.open<PoDetailDialogComponent, PoDetailDialogData, PoDetailDialogResult>(PoDetailDialogComponent, { width: '1100px', maxWidth: '96vw', autoFocus: false, restoreFocus: true, data: { ...row } }).afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(result => { if (result?.error)
        this.poError = result.error; }); }
    async executePo(row: {
        po: string;
        clientCode: string;
    }) {
        if (this.openingSupplierDemo)
            return;
        this.openingSupplierDemo = true;
        this.poError = '';
        this.poActionMessage = '';
        try {
            const { firstValueFrom } = await import('rxjs');
            const ticket = await firstValueFrom(this.providerApi.preparePoLaunch(row.po, row.clientCode));
            this.launchAgent(ticket);
            this.poActionMessage = 'Se solicitó abrir el agente. Se usarán las cantidades redondeadas de Gestión.';
        }
        catch (e) {
            this.poError = apiError(e);
        }
        finally {
            this.openingSupplierDemo = false;
        }
    }
    canExecutePo(po: string) { return /^PO-[0-9]{1,20}$/.test(po); }
    private launchAgent(ticket: AgentTicket) { this.gridMessage = 'Se solicitó abrir el agente VINCE. Si vence la autorización, selecciona nuevamente el PO.'; this.agentLauncher.launch(ticket).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(result => { if (result === 'received')
        this.gridMessage = 'El agente VINCE recibió la operación. Continúa en el navegador del proveedor.'; if (result === 'expired')
        this.gridMessage = 'La autorización venció. Selecciona nuevamente el PO para abrir el agente VINCE.'; }); }
    providerPos: string[] = [];
    async openSupplierFromGrid(reference: OrderReference) {
        if (this.openingSupplierDemo)
            return;
        this.openingSupplierDemo = true;
        this.gridError = '';
        this.gridMessage = '';
        try {
            const { firstValueFrom } = await import('rxjs');
            this.providerPos = await firstValueFrom(this.providerApi.providerPos(reference));
            if (!this.providerPos.length) {
                this.gridError = 'No se encontraron códigos PO- en las combinaciones de esta OC.';
                return;
            }
            const po = await firstValueFrom(this.dialog.open<PoSelectionDialogComponent, PoSelectionDialogData, PoSelectionDialogResult>(PoSelectionDialogComponent, { width: '380px', maxWidth: '94vw', autoFocus: 'first-tabbable', restoreFocus: true, data: { pos: this.providerPos } }).afterClosed());
            if (!po)
                return;
            if (environment.providerAgent) {
                const ticket = await firstValueFrom(this.providerApi.prepareProviderLaunch(reference, po));
                this.launchAgent(ticket);
                return;
            }
            this.gridMessage = 'Continúa en la ventana automatizada de Avery/VINCE. Las confirmaciones de colores aparecerán allí.';
            const result = await firstValueFrom(this.providerApi.providerLogin(reference, po));
            if (result.status === 'confirmation-required') {
                this.gridMessage = '';
                this.gridError = 'Actualiza el backend local y vuelve a abrir VINCE para confirmar los colores en la ventana automatizada.';
                return;
            }
            this.gridMessage = result.message;

        }
        catch (e) {
            this.gridMessage = '';
            this.gridError = apiError(e);
        }
        finally {
            this.openingSupplierDemo = false;
        }
    }
    async openSupplierDemo() {
        if (!this.order?.items.length || this.openingSupplierDemo)
            return;
        const detail = this.order;
        this.clearFeedback();
        this.openingSupplierDemo = true;
        try {
            const count = await this.supplierDemo.open(detail);
            this.message = `Prueba de OC ${detail.order.series}-${detail.order.number}: ${count} cantidades requeridas completadas en la nueva pestaña. No se envió ningún pedido.`;
        }
        catch (error) {
            this.error = error instanceof Error ? error.message : 'No se pudo abrir la prueba de llenado.';
        }
        finally {
            this.openingSupplierDemo = false;
        }
    }
    session = inject(SessionService);
    tab: 'orders' | 'matrix' | 'outputs' | 'pos' = 'orders';
    series = '002';
    number = '';
    order: OrderDetail | null = null;
    requirements: Requirement[] = [];
    selectedItem: OrderItem | null = null;
    rows: MatrixRow[] = [];
    articles: ArticleRule[] = [{ itemCode: '', description: '', multiple: 5 }, { itemCode: '', description: '', multiple: 5 }];
    preview: Preview | null = null;
    busy = false;
    importing = false;
    exporting = false;
    message = '';
    error = '';
    importError = '';
    importOpen = false;
    workbook: import('exceljs').Workbook | null = null;
    sheetNames: string[] = [];
    sheetName = '';
    filename = '';
    pending: MatrixRow[] = [];
    importVersion = 0;
    get uniqueArticles() { return Array.from(new Map(this.order?.items.map(i => [i.itemCode, i]) ?? []).values()); }
    get pendingTotal() { return this.pending.reduce((sum, row) => sum + row.quantity, 0); }
    clearFeedback() { this.error = ''; this.message = ''; }
    invalidate() { this.preview = null; this.message = ''; }
    search() {
        this.clearFeedback();
        this.order = null;
        this.requirements = [];
        this.selectedItem = null;
        if (!/^\d{3}$/.test(this.series) || !/^\d{6}$/.test(this.number)) {
            this.error = 'Ingresa una serie de 3 dígitos y una OC de 6 dígitos.';
            return;
        }
        this.busy = true;
        this.api.order(this.series, this.number).pipe(finalize(() => this.busy = false)).subscribe({ next: order => this.order = order, error: e => this.error = apiError(e) });
    }
    showRequirements(item: OrderItem) {
        this.clearFeedback();
        this.selectedItem = item;
        this.requirements = [];
        this.busy = true;
        this.api.requirements(this.order!.order.series, this.order!.order.number, item.sequence).pipe(finalize(() => this.busy = false)).subscribe({ next: rows => { this.requirements = rows; if (!rows.length)
                this.message = 'El ítem no tiene requerimientos vinculados.'; }, error: e => this.error = apiError(e) });
    }
    useRequirements() {
        const grouped = new Map<string, MatrixRow>();
        for (const r of this.requirements) {
            const row: MatrixRow = { factory: r.factory, productionOrder: r.productionOrder, style: r.style, presentationCode: r.presentationCode, color: r.presentation || r.color, size: r.size, destination: r.destination, quantity: r.quantity };
            const key = JSON.stringify([row.factory, row.productionOrder, row.style, row.presentationCode, row.color, row.size, row.destination]);
            const previous = grouped.get(key);
            if (previous)
                previous.quantity += row.quantity;
            else
                grouped.set(key, row);
        }
        this.rows = Array.from(grouped.values());
        if (this.selectedItem)
            this.articles[0] = { itemCode: this.selectedItem.itemCode, description: this.selectedItem.description, multiple: 5 };
        this.invalidate();
        this.tab = 'matrix';
        this.message = 'Se cargó la base del ítem seleccionado. Revisa los artículos. La cantidad de compra se redondea automáticamente a terminación 5 o 0.';
    }
    chooseArticle(index: number, itemCode: string) {
        const selected = this.uniqueArticles.find(a => a.itemCode === itemCode);
        if (selected)
            this.articles[index] = { ...this.articles[index], itemCode, description: selected.description };
        this.invalidate();
    }
    addArticle() { if (this.articles.length < 10)
        this.articles.push({ itemCode: '', description: '', multiple: 5 }); this.invalidate(); }
    removeArticle(index: number) { this.articles.splice(index, 1); this.invalidate(); }
    addRow() { if (this.rows.length < 500)
        this.rows.push({ factory: '', productionOrder: '', style: '', presentationCode: '', color: '', size: '', destination: '', quantity: 0 }); this.invalidate(); }
    removeRow(index: number) { this.rows.splice(index, 1); this.invalidate(); }
    example() {
        this.rows = ['S', 'M', 'L'].map((size, i) => ({ factory: '001', productionOrder: 'NP-EJEMPLO', style: 'EST-001', presentationCode: '1', color: 'Negro', size, destination: '', quantity: [21, 31, 18][i] }));
        this.articles = [{ itemCode: 'AV-EJEMPLO-01', description: 'Etiqueta de identificación', multiple: 5 }, { itemCode: 'AV-EJEMPLO-02', description: 'Sticker de empaque', multiple: 5 }];
        this.invalidate();
        this.message = 'Datos de ejemplo cargados. No corresponden a una orden real.';
    }
    async openFile(event: Event) {
        const file = (event.target as HTMLInputElement).files?.[0];
        const version = ++this.importVersion;
        this.pending = [];
        this.workbook = null;
        this.sheetNames = [];
        this.importError = '';
        if (!file)
            return;
        this.filename = file.name;
        this.importing = true;
        try {
            const book = await this.excel.open(file);
            if (version !== this.importVersion)
                return;
            this.workbook = book;
            this.sheetNames = book.worksheets.map(s => s.name);
            this.sheetName = this.sheetNames[0];
            this.readSheet();
        }
        catch (e) {
            if (version === this.importVersion)
                this.importError = e instanceof Error ? e.message : 'No se pudo leer el Excel.';
        }
        finally {
            if (version === this.importVersion)
                this.importing = false;
        }
    }
    readSheet() { this.pending = []; this.importError = ''; if (!this.workbook)
        return; try {
        this.pending = this.excel.read(this.workbook, this.sheetName);
    }
    catch (e) {
        this.importError = e instanceof Error ? e.message : 'No se pudo leer la hoja.';
    } }
    importRows() { if (!this.pending.length)
        return; this.rows = this.pending.map(r => ({ ...r })); this.importOpen = false; this.invalidate(); this.message = `Se importaron ${this.rows.length} filas. Configura los artículos y previsualiza la compra.`; }
    async template() { try {
        await this.excel.template();
    }
    catch {
        this.error = 'No se pudo descargar la plantilla.';
    } }
    calculate() {
        this.clearFeedback();
        this.preview = null;
        if (!this.rows.length) {
            this.error = 'Carga una matriz antes de calcular.';
            return;
        }
        this.busy = true;
        this.api.preview(this.rows, this.articles).pipe(finalize(() => this.busy = false)).subscribe({ next: result => { this.preview = result; this.tab = 'outputs'; this.message = 'Previsualización calculada. No se han actualizado cantidades en Gestión.'; }, error: e => this.error = apiError(e) });
    }
    async export(quality: boolean) { if (!this.preview)
        return; this.exporting = true; try {
        await this.excel.export(this.preview, quality);
        this.message = 'Archivo Excel generado.';
    }
    catch {
        this.error = 'No se pudo generar el archivo.';
    }
    finally {
        this.exporting = false;
    } }
    trackIndex(index: number) { return index; }
}
