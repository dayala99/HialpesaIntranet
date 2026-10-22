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
import { OrderReference, LinkedRequirement } from '../../../models/purchase-order.models';
import { SessionService } from '../../../../../core/auth/session.service';
import { ConfirmationDialogComponent, ConfirmationDialogData } from '../../../../../shared/components/dialogs/confirmation-dialog/confirmation-dialog.component';
export interface RequirementLinksDialogData {
    order: OrderReference;
    sequence: string;
    status: string;
}
export interface RequirementLinksDialogResult {
    changed: boolean;
}
@Component({ selector: 'app-requirement-links-dialog', standalone: true, imports: [CommonModule, FormsModule, MatDialogModule, MatIconModule, SharedModule], templateUrl: './requirement-links-dialog.component.html', styleUrl: './requirement-links-dialog.component.scss' })
export class RequirementLinksDialogComponent {
    readonly data = inject<RequirementLinksDialogData>(MAT_DIALOG_DATA);
    private ref = inject(MatDialogRef<RequirementLinksDialogComponent, RequirementLinksDialogResult>);
    private api = inject(PurchaseOrdersApiService);
    private dialog = inject(MatDialog);
    private destroyRef = inject(DestroyRef);
    session = inject(SessionService);
    linkedSequence = this.data.sequence;
    linkedRows: LinkedRequirement[] = [];
    linkedSelected = new Set<string>();
    linkedBusy = true;
    linkedError = '';
    ngOnInit() { this.api.linkedRequirements(this.data.order, this.data.sequence).pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.linkedBusy = false)).subscribe({ next: rows => this.linkedRows = rows, error: e => this.linkedError = apiError(e) }); }
    toggleLinked(key: string, checked: boolean) { if (checked)
        this.linkedSelected.add(key);
    else
        this.linkedSelected.delete(key); }
    toggleAllLinked(checked: boolean) { this.linkedSelected = checked ? new Set(this.linkedRows.map(r => r.key)) : new Set(); }
    async withdrawRequirements() { if (this.linkedBusy || !this.linkedSelected.size)
        return; const confirmed = await firstValueFrom(this.dialog.open<ConfirmationDialogComponent, ConfirmationDialogData, boolean>(ConfirmationDialogComponent, { width: '480px', maxWidth: '95vw', data: { title: 'Retirar requerimientos', message: '¿Deseas retirar los ' + this.linkedSelected.size + ' requerimientos seleccionados de esta OC? Gestión actualizará sus saldos y puede eliminar la secuencia si queda vacía. La OP no se elimina.', confirmLabel: 'Sí, retirar' } }).afterClosed()); if (confirmed !== true)
        return; this.linkedBusy = true; this.linkedError = ''; this.api.removeRequirements(this.data.order, this.linkedSequence, [...this.linkedSelected]).pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.linkedBusy = false)).subscribe({ next: () => this.ref.close({ changed: true }), error: e => this.linkedError = apiError(e) }); }
}
