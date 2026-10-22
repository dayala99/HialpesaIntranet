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
import { EditableOrderItem, OrderReference, LookupOption } from '../../../models/purchase-order.models';
export interface ItemEditDialogData {
    order: OrderReference;
    sequence: string;
    item: EditableOrderItem;
    discounts: LookupOption[];
}
export interface ItemEditDialogResult {
    saved: boolean;
}
@Component({ selector: 'app-item-edit-dialog', standalone: true, imports: [CommonModule, FormsModule, MatDialogModule, MatIconModule, SharedModule], templateUrl: './item-edit-dialog.component.html', styleUrl: './item-edit-dialog.component.scss' })
export class ItemEditDialogComponent {
    readonly data = inject<ItemEditDialogData>(MAT_DIALOG_DATA);
    private ref = inject(MatDialogRef<ItemEditDialogComponent, ItemEditDialogResult>);
    private api = inject(PurchaseOrdersApiService);
    private destroyRef = inject(DestroyRef);
    itemSequence = this.data.sequence;
    itemEdit = { ...this.data.item };
    itemDiscounts = this.data.discounts;
    itemSaving = false;
    itemError = '';
    saveItem() { if (this.itemSaving)
        return; this.itemSaving = true; this.itemError = ''; this.api.updateItem(this.data.order, this.itemSequence, this.itemEdit).pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.itemSaving = false)).subscribe({ next: () => this.ref.close({ saved: true }), error: e => this.itemError = apiError(e) }); }
}
