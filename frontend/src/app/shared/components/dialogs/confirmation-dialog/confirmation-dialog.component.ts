import { Component, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialog, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, timer, switchMap, catchError, of, firstValueFrom } from 'rxjs';
import { SharedModule } from '../../../shared.module';
import { apiError } from '../../../../core/http/api-error';
export interface ConfirmationDialogData {
    title: string;
    message: string;
    confirmLabel?: string;
    cancelLabel?: string;
}
export type ConfirmationDialogResult = boolean;
@Component({ selector: 'app-confirmation-dialog', standalone: true, imports: [CommonModule, FormsModule, MatDialogModule, MatIconModule, SharedModule], templateUrl: './confirmation-dialog.component.html', styleUrl: './confirmation-dialog.component.scss' })
export class ConfirmationDialogComponent {
    readonly data = inject<ConfirmationDialogData>(MAT_DIALOG_DATA);
}
