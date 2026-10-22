import { Component, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialog, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, timer, switchMap, catchError, of, firstValueFrom } from 'rxjs';
import { SharedModule } from '../../../../../shared/shared.module';
import { apiError } from '../../../../../core/http/api-error';
import { PoApiService } from '../../../services/po-api.service';
import { PoDetailLine } from '../../../models/purchase-order.models';
export interface PoDetailDialogData {
    po: string;
    clientCode: string;
}
export interface PoDetailDialogResult {
    error?: string;
}
@Component({ selector: 'app-po-detail-dialog', standalone: true, imports: [CommonModule, FormsModule, MatDialogModule, MatIconModule, SharedModule], templateUrl: './po-detail-dialog.component.html', styleUrl: './po-detail-dialog.component.scss' })
export class PoDetailDialogComponent {
    readonly data = inject<PoDetailDialogData>(MAT_DIALOG_DATA);
    private ref = inject(MatDialogRef<PoDetailDialogComponent, PoDetailDialogResult>);
    private api = inject(PoApiService);
    private destroyRef = inject(DestroyRef);
    selectedPo = this.data.po;
    poDetail: PoDetailLine[] = [];
    poDetailBusy = true;
    get poRequiredTotal() { return this.poDetail.reduce((n, r) => n + r.required, 0); }
    get poPurchasedTotal() { return this.poDetail.reduce((n, r) => n + r.purchased, 0); }
    ngOnInit() { this.api.poDetails(this.data.po, this.data.clientCode).pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.poDetailBusy = false)).subscribe({ next: rows => this.poDetail = rows, error: e => this.ref.close({ error: apiError(e) }) }); }
}
