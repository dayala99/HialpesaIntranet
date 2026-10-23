import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './features/home/pages/home/home.component';
import { LoginComponent } from './features/auth/pages/login/login.component';
import { sessionGuard } from './core/auth/session.guard';
const routes: Routes = [
    { path: 'login', component: LoginComponent, title: 'Iniciar sesion - Intranet' },
    { path: '', canActivate: [sessionGuard], component: HomeComponent, title: 'Inicio · Intranet' },
    { path: 'logistica/avios', canActivate: [sessionGuard], canActivateChild:[sessionGuard], loadChildren: () => import('./features/purchase-orders/purchase-orders.module').then(m => m.PurchaseOrdersModule) },
    { path: 'logistica/importaciones', canActivate: [sessionGuard], loadChildren: () => import('./features/imports/imports.module').then(m => m.ImportsModule) },
    { path: 'tintoreria/generacion-partidas', canActivate: [sessionGuard], loadComponent: () => import('./features/dyeing/pages/batch-generation/batch-generation.component').then(m => m.BatchGenerationComponent), title: 'Generación Partidas - Tintoreria · Intranet' },
    { path: 'aseguramiento-calidad/eventos-tintoreria', canActivate: [sessionGuard], loadComponent: () => import('./features/quality-assurance/pages/events/dyeing-events.component').then(m => m.DyeingEventsComponent), data: { heading: 'Eventos Tintorería' }, title: 'Eventos Tintorería · Intranet' },
    { path: 'aseguramiento-calidad/eventos-tintoreria-tela-por-acabar', canActivate: [sessionGuard], loadComponent: () => import('./features/quality-assurance/pages/events/dyeing-events.component').then(m => m.DyeingEventsComponent), data: { unfinished: true }, title: 'Eventos Tintorería - Tela Por Acabar · Intranet' },
    { path: 'sin-acceso', canActivate:[sessionGuard], loadComponent:()=>import('./features/auth/pages/access-denied/access-denied.component').then(m=>m.AccessDeniedComponent),title:'Acceso no autorizado · Intranet' },
    { path: '**', redirectTo: '' }
];
@NgModule({ imports: [RouterModule.forRoot(routes)], exports: [RouterModule] })
export class AppRoutingModule {
}
