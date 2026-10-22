import { Component, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialog, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, timer, switchMap, catchError, of, firstValueFrom } from 'rxjs';
import { SharedModule } from '../../../../../shared/shared.module';
import { apiError } from '../../../../../core/http/api-error';
import { ProviderApiService } from '../../../services/provider-api.service';
import { AgentTicket } from '../../../models/provider.models';
export interface AgentLaunchDialogData {
    ticket: AgentTicket;
}
export type AgentLaunchDialogResult = 'received' | 'expired';
@Component({ selector: 'app-agent-launch-dialog', standalone: true, imports: [CommonModule, FormsModule, MatDialogModule, MatIconModule, SharedModule], templateUrl: './agent-launch-dialog.component.html', styleUrl: './agent-launch-dialog.component.scss' })
export class AgentLaunchDialogComponent {
    readonly data = inject<AgentLaunchDialogData>(MAT_DIALOG_DATA);
    private ref = inject(MatDialogRef<AgentLaunchDialogComponent, AgentLaunchDialogResult>);
    private api = inject(ProviderApiService);
    private destroyRef = inject(DestroyRef);
    agentLaunchRemaining = '02:00';
    ngOnInit() { const expiresAt = Date.parse(this.data.ticket.expiresAt); timer(0, 750).pipe(switchMap(() => this.api.providerLaunchStatus(this.data.ticket.token).pipe(catchError(() => of(null)))), takeUntilDestroyed(this.destroyRef)).subscribe(status => { if (status?.received)
        this.ref.close('received'); }); timer(0, 250).pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => { const remaining = Math.max(0, Math.ceil((expiresAt - Date.now()) / 1000)); this.agentLaunchRemaining = String(Math.floor(remaining / 60)).padStart(2, '0') + ':' + String(remaining % 60).padStart(2, '0'); if (!remaining)
        this.ref.close('expired'); }); }
}
