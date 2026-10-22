import { Component, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialog, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, timer, switchMap, catchError, of, firstValueFrom } from 'rxjs';
import { SharedModule } from '../../../../../shared/shared.module';
import { apiError } from '../../../../../core/http/api-error';
import { ColorConfirmation } from '../../../models/provider.models';
export interface ColorConfirmationDialogData {
    po: string;
    confirmations: ColorConfirmation[];
}
export type ColorConfirmationDialogResult = boolean;
@Component({ selector: 'app-color-confirmation-dialog', standalone: true, imports: [CommonModule, FormsModule, MatDialogModule, MatIconModule, SharedModule], templateUrl: './color-confirmation-dialog.component.html', styleUrl: './color-confirmation-dialog.component.scss' })
export class ColorConfirmationDialogComponent {
    readonly data = inject<ColorConfirmationDialogData>(MAT_DIALOG_DATA);
}
