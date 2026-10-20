import { Component, inject } from '@angular/core';
import { SessionService } from './core/session.service';
@Component({ selector: 'app-root', templateUrl: './app.component.html', styleUrl: './app.component.scss' })
export class AppComponent {
  session = inject(SessionService);
  menuOpen = false;
  logisticsOpen = true;
  constructor() { this.session.load(); }
}
