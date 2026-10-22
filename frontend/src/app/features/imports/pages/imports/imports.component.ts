import { ImportsApiService } from '../../services/imports-api.service';
import { ImportSource, ImportCatalogOption, ImportRecord, ImportRequest } from '../../models/import.models';
import { ExistingImportDialogComponent, ExistingImportDialogData } from '../../components/dialogs/existing-import-dialog/existing-import-dialog.component';
import { ImportConfirmationDialogComponent, ImportConfirmationDialogData } from '../../components/dialogs/import-confirmation-dialog/import-confirmation-dialog.component';
import { Subject, switchMap, timer, catchError, of } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatDialog } from '@angular/material/dialog';
import { Component, inject, DestroyRef } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { apiError } from '../../../../core/http/api-error';
import { SessionService } from '../../../../core/auth/session.service';
@Component({ selector: 'app-imports', templateUrl: './imports.component.html', styleUrl: './imports.component.scss' })
export class ImportsComponent {
    private dialog = inject(MatDialog);
    confirmCreation() { if (this.busy || this.blocked || !this.source)
        return; this.review = true; this.dialog.open<ImportConfirmationDialogComponent, ImportConfirmationDialogData, boolean>(ImportConfirmationDialogComponent, { width: '620px', maxWidth: '94vw', autoFocus: 'first-tabbable', restoreFocus: true, data: { supplier: this.source.supplier, currency: this.source.currency, registeredAt: this.form.registeredAt, origin: this.optionName('origins', this.form.origin), itemType: this.optionName('itemTypes', this.form.itemType), series: this.loadedSeries, number: this.loadedNumber } }).afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(confirmed => { if (confirmed === true)
        this.save();
    else
        this.review = false; }); }
    private api = inject(ImportsApiService);
    session = inject(SessionService);
    private route = inject(ActivatedRoute);
    series = this.route.snapshot.queryParamMap.get('series') || '002';
    number = this.route.snapshot.queryParamMap.get('number') || '';
    source: ImportSource | null = null;
    options: ImportCatalogOption[] = [];
    automatic = false;
    validAnnex = false;
    busy = false;
    review = false;
    error = '';
    success = '';
    loadedSeries = '';
    loadedNumber = '';
    uncertain = false;
    form: ImportRequest = { itemType: '', origin: '', shipment: '', country: '', area: '05', registeredAt: [new Date().getFullYear(), String(new Date().getMonth() + 1).padStart(2, '0'), String(new Date().getDate()).padStart(2, '0')].join('-'), guideDate: null as string | null, paymentDate: null as string | null, dueDate: null as string | null, guide: '', observations: '' };
    registrationOpen = !!this.route.snapshot.queryParamMap.get('number');
    rows: ImportRecord[] = [];
    listSearch = '';
    listPage = 1;
    totalPages = 1;
    hasMore = false;
    listBusy = false;
    listError = '';
    private destroyRef = inject(DestroyRef);
    private listRequests = new Subject<{
        page: number;
        search: string;
    }>();
    ngOnInit() { this.listRequests.pipe(switchMap(q => timer(300).pipe(switchMap(() => this.api.list(q.page, q.search)), catchError(e => { this.listError = apiError(e); return of(null); }))), takeUntilDestroyed(this.destroyRef)).subscribe(r => { this.listBusy = false; if (r) {
        this.rows = r.rows;
        this.listPage = r.page;
        this.totalPages = r.totalPages ?? 1;
        this.hasMore = r.hasMore;
    } }); if (!this.registrationOpen)
        this.loadList(); }
    loadList(page = 1) { this.listBusy = true; this.listError = ''; this.rows = []; this.listRequests.next({ page, search: this.listSearch.trim() }); }
    openRegistration() { this.source = null; this.number = ''; this.error = ''; this.success = ''; this.review = false; this.uncertain = false; this.registrationOpen = true; }
    backToList() { this.registrationOpen = false; this.source = null; this.error = ''; this.loadList(this.listPage); }
    constructor() { this.api.catalogs().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ next: r => this.options = r, error: e => this.error = apiError(e) }); }
    choices(kind: string) { return this.options.filter(o => o.Kind === kind); }
    optionName(kind: string, code: string) { return this.options.find(o => o.Kind === kind && o.Code === code)?.Name || code; }
    search() { this.source = null; this.review = false; this.error = ''; this.success = ''; this.busy = true; this.api.source(this.series, this.number).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ next: r => { this.source = r.source; this.automatic = r.automaticNumbering; this.validAnnex = r.validAnnex; this.loadedSeries = this.series; this.loadedNumber = this.number; this.uncertain = false; this.busy = false; if (this.source.existingImport && this.source.existingImport !== "0") {
            this.dialog.open<ExistingImportDialogComponent, ExistingImportDialogData, void>(ExistingImportDialogComponent, { width: '480px', maxWidth: '94vw', autoFocus: 'first-tabbable', restoreFocus: false, disableClose: true, data: { series: this.loadedSeries, number: this.loadedNumber, existingImport: this.source.existingImport } }).afterClosed().subscribe(() => this.backToList());
        } }, error: e => { this.error = apiError(e); this.busy = false; } }); }
    get blocked() { return !this.source || !this.automatic || !this.validAnnex || this.source.supplierOrigin !== 'E' || !!(this.source.existingImport && this.source.existingImport !== '0') || !this.session.current()?.canWriteDatabase || this.uncertain; }
    save() { if (this.busy || this.blocked || !this.review)
        return; this.busy = true; this.error = ''; this.api.create(this.loadedSeries, this.loadedNumber, this.form).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ next: r => { this.success = 'Importación ' + r.number + ' creada y asociada a la OC ' + this.loadedSeries + '-' + this.loadedNumber; this.source!.existingImport = r.number; this.review = false; this.busy = false; this.registrationOpen = false; this.loadList(); }, error: e => { this.error = apiError(e); this.uncertain = true; this.review = false; this.busy = false; } }); }
}
