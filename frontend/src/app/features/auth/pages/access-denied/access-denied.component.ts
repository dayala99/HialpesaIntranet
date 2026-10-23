import {Component} from '@angular/core';
import {RouterLink} from '@angular/router';
@Component({selector:'app-access-denied',standalone:true,imports:[RouterLink],template:'<section class="panel" style="padding:24px"><h1>Acceso no autorizado</h1><p>Tu perfil de Gestión no tiene acceso a esta opción.</p><a class="button" routerLink="/">Volver al inicio</a></section>'})
export class AccessDeniedComponent {}
