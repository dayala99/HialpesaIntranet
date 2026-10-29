import { Router } from '@angular/router';
import { Component, effect, inject } from '@angular/core';
import { SessionService } from '../core/auth/session.service';
@Component({ selector: 'app-shell', templateUrl: './shell.component.html', styleUrl: './shell.component.scss' })
export class ShellComponent {
    session = inject(SessionService);
    menuOpen = window.matchMedia('(min-width: 1025px)').matches;
    logisticsOpen = true;
    logisticsMaintenanceOpen = true;
    logisticsQueriesOpen = true;
    dyeingOpen = true;
    qualityAssuranceOpen = true;
    private router = inject(Router);
    constructor() {
        let authenticated = false;
        effect(() => {
            const hasSession = this.session.current() !== null;
            if (hasSession && !authenticated) this.menuOpen = true;
            authenticated = hasSession;
        });
    }
    closeMobileNavigation(){if(window.matchMedia('(max-width: 1024px)').matches)this.menuOpen=false;}
    get isLogin() { return this.router.url.startsWith('/login'); }
    logout() { this.session.logout().subscribe({ next: () => { this.menuOpen = false; void this.router.navigate(['/login']); }, error: () => this.session.error.set('No se pudo cerrar la sesion. Vuelve a intentarlo.') }); }
}
