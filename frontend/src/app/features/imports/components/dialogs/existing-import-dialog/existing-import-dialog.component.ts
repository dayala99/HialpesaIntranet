import { Component, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialog, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, timer, switchMap, catchError, of, firstValueFrom } from 'rxjs';
import { SharedModule } from '../../../../../shared/shared.module';
import { apiError } from '../../../../../core/http/api-error';
export interface ExistingImportDialogData {
    series: string;
    number: string;
    existingImport: string;
}
export type ExistingImportDialogResult = void;
@Component({ selector: 'app-existing-import-dialog', standalone: true, imports: [CommonModule, FormsModule, MatDialogModule, MatIconModule, SharedModule], templateUrl: './existing-import-dialog.component.html', styleUrl: './existing-import-dialog.component.scss' })
export class ExistingImportDialogComponent {
    readonly data = inject<ExistingImportDialogData>(MAT_DIALOG_DATA);
}
