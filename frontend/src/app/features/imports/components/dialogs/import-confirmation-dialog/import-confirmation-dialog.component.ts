import { Component, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialog, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, timer, switchMap, catchError, of, firstValueFrom } from 'rxjs';
import { SharedModule } from '../../../../../shared/shared.module';
import { apiError } from '../../../../../core/http/api-error';
export interface ImportConfirmationDialogData {
    supplier: string;
    currency: string;
    registeredAt: string;
    origin: string;
    itemType: string;
    series: string;
    number: string;
}
export type ImportConfirmationDialogResult = boolean;
@Component({ selector: 'app-import-confirmation-dialog', standalone: true, imports: [CommonModule, FormsModule, MatDialogModule, MatIconModule, SharedModule], templateUrl: './import-confirmation-dialog.component.html', styleUrl: './import-confirmation-dialog.component.scss' })
export class ImportConfirmationDialogComponent {
    readonly data = inject<ImportConfirmationDialogData>(MAT_DIALOG_DATA);
}
