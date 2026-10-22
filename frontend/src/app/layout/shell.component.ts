import { Router } from '@angular/router';
import { Component, inject } from '@angular/core';
import { SessionService } from '../core/auth/session.service';
@Component({ selector: 'app-shell', templateUrl: './shell.component.html', styleUrl: './shell.component.scss' })
export class ShellComponent {
    session = inject(SessionService);
    menuOpen = false;
    logisticsOpen = true;
    private router = inject(Router);
    get isLogin() { return this.router.url.startsWith('/login'); }
    logout() { this.session.logout().subscribe({ next: () => { this.menuOpen = false; void this.router.navigate(['/login']); }, error: () => this.session.error.set('No se pudo cerrar la sesion. Vuelve a intentarlo.') }); }
}
