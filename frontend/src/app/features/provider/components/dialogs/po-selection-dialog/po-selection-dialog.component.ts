import { Component, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialog, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, timer, switchMap, catchError, of, firstValueFrom } from 'rxjs';
import { SharedModule } from '../../../../../shared/shared.module';
import { apiError } from '../../../../../core/http/api-error';
export interface PoSelectionDialogData {
    pos: string[];
}
export type PoSelectionDialogResult = string;
@Component({ selector: 'app-po-selection-dialog', standalone: true, imports: [CommonModule, FormsModule, MatDialogModule, MatIconModule, SharedModule], templateUrl: './po-selection-dialog.component.html', styleUrl: './po-selection-dialog.component.scss' })
export class PoSelectionDialogComponent {
    readonly data = inject<PoSelectionDialogData>(MAT_DIALOG_DATA);
}
