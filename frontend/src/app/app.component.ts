import { Router } from '@angular/router';
import { Component, HostListener, inject } from '@angular/core';
import { SessionService } from './core/session.service';
@Component({ selector: 'app-root', templateUrl: './app.component.html', styleUrl: './app.component.scss' })
export class AppComponent {
  @HostListener('document:click',['$event'])
  openDateCalendar(event:MouseEvent){
    const input=event.target;
    if(!(input instanceof HTMLInputElement)||input.type!=='date'||input.matches(':disabled')||input.readOnly||typeof input.showPicker!=='function')return;
    try{input.showPicker();event.preventDefault();}catch{/* Preserve native interaction if the browser cannot open the picker. */}
  }
  session = inject(SessionService);
  menuOpen = false;
  logisticsOpen = true;
  private router=inject(Router);
  get isLogin(){return this.router.url.startsWith('/login');}
  logout(){this.session.logout().subscribe({next:()=>{this.menuOpen=false;void this.router.navigate(['/login']);},error:()=>this.session.error.set('No se pudo cerrar la sesion. Vuelve a intentarlo.')});}
}
