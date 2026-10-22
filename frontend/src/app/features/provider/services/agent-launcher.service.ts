import { Injectable, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { environment } from '../../../../environments/environment';
import { AgentTicket } from '../models/provider.models';
import { AgentLaunchDialogComponent, AgentLaunchDialogData, AgentLaunchDialogResult } from '../components/dialogs/agent-launch-dialog/agent-launch-dialog.component';
@Injectable({ providedIn: 'root' })
export class AgentLauncherService {
    private dialog = inject(MatDialog);
    launch(ticket: AgentTicket) { if (!/^[A-F0-9]{64}$/.test(ticket.token))
        throw new Error('Respuesta de apertura no válida.'); const expiresAt = Date.parse(ticket.expiresAt); if (!Number.isFinite(expiresAt) || expiresAt <= Date.now())
        throw new Error('La autorización venció. Selecciona nuevamente el PO.'); const server = new URL(environment.apiBaseUrl, window.location.origin).href.replace(/\/$/, ''); const link = 'hialpesa-vince://run?server=' + encodeURIComponent(server) + '&token=' + encodeURIComponent(ticket.token); const popup = this.dialog.open<AgentLaunchDialogComponent, AgentLaunchDialogData, AgentLaunchDialogResult>(AgentLaunchDialogComponent, { width: '300px', maxWidth: '90vw', ariaLabel: 'Tiempo restante', autoFocus: false, restoreFocus: true, data: { ticket } }); window.open(link, '_self'); return popup.afterClosed(); }
}
