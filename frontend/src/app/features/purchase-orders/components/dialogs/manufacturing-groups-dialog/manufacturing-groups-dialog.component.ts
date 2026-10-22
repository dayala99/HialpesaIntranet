import { Component, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialog, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, timer, switchMap, catchError, of, firstValueFrom } from 'rxjs';
import { SharedModule } from '../../../../../shared/shared.module';
import { apiError } from '../../../../../core/http/api-error';
import { PurchaseOrdersApiService } from '../../../services/purchase-orders-api.service';
import { OrderReference, LookupOption } from '../../../models/purchase-order.models';
export interface ManufacturingGroupsDialogData {
    order: OrderReference;
}
export interface ManufacturingGroupsDialogResult {
    saved: boolean;
}
@Component({ selector: 'app-manufacturing-groups-dialog', standalone: true, imports: [CommonModule, FormsModule, MatDialogModule, MatIconModule, SharedModule], templateUrl: './manufacturing-groups-dialog.component.html', styleUrl: './manufacturing-groups-dialog.component.scss' })
export class ManufacturingGroupsDialogComponent {
    readonly data = inject<ManufacturingGroupsDialogData>(MAT_DIALOG_DATA);
    private api = inject(PurchaseOrdersApiService);
    private destroyRef = inject(DestroyRef);
    groupTarget = this.data.order;
    groupSaving = false;
    originalGroups: string[] = [];
    groupsLoading = true;
    groupsLoaded = false;
    saved = false;
    factoryOptions: LookupOption[] = [];
    factoryCode = 'HIA';
    groupOp = '';
    opGroups: LookupOption[] = [];
    opGroupBusy = false;
    opGroupError = '';
    opGroupSearched = false;
    selectedManufacturingGroups: LookupOption[] = [];
    get addedGroupCodes() { return this.selectedManufacturingGroups.map(g => g.code).filter(c => !this.originalGroups.includes(c)); }
    get removedGroupCodes() { return this.originalGroups.filter(c => !this.selectedManufacturingGroups.some(g => g.code === c)); }
    get groupChanges() { return this.addedGroupCodes.length + this.removedGroupCodes.length; }
    ngOnInit() { this.api.orderGroups(this.groupTarget).pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.groupsLoading = false)).subscribe({ next: rows => { this.selectedManufacturingGroups = rows; this.originalGroups = rows.map(g => g.code); this.groupsLoaded = true; }, error: e => this.opGroupError = apiError(e) }); this.api.factories().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({ next: rows => { this.factoryOptions = rows; this.factoryCode = rows.find(r => r.code.trim().toUpperCase() === 'HIA')?.code ?? 'HIA'; }, error: e => this.opGroupError = apiError(e) }); }
    resetOpGroups() { this.opGroups = []; this.opGroupSearched = false; this.opGroupError = ''; }
    searchManufacturingGroup() { if (this.opGroupBusy)
        return; this.resetOpGroups(); const factory = this.factoryCode.trim(), op = this.groupOp.trim().toUpperCase(); if (!factory || !/^[A-Z0-9]{5}$/.test(op)) {
        this.opGroupError = 'Selecciona una fábrica e ingresa una OP de 5 caracteres.';
        return;
    } this.opGroupBusy = true; this.api.groupsByOp(factory, op).pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.opGroupBusy = false)).subscribe({ next: rows => { this.opGroups = rows; this.opGroupSearched = true; }, error: e => this.opGroupError = apiError(e) }); }
    chooseManufacturingGroup(group: LookupOption) { if (this.groupSaving || this.groupsLoading || this.hasManufacturingGroup(group.code))
        return; this.selectedManufacturingGroups = [...this.selectedManufacturingGroups, group]; }
    hasManufacturingGroup(code: string) { return this.selectedManufacturingGroups.some(g => g.code === code); }
    removeManufacturingGroup(code: string) { if (this.groupSaving || this.groupsLoading)
        return; this.selectedManufacturingGroups = this.selectedManufacturingGroups.filter(g => g.code !== code); }
    saveGroups() { if (this.groupSaving || !this.groupsLoaded || !this.groupChanges)
        return; this.groupSaving = true; this.opGroupError = ''; this.api.addManufacturingGroups(this.groupTarget, this.addedGroupCodes, this.removedGroupCodes).pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.groupSaving = false)).subscribe({ next: () => { this.originalGroups = this.selectedManufacturingGroups.map(g => g.code); this.saved = true; }, error: e => this.opGroupError = apiError(e) }); }
}
