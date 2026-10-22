import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './features/home/pages/home/home.component';
import { LoginComponent } from './features/auth/pages/login/login.component';
import { sessionGuard } from './core/auth/session.guard';
const routes: Routes = [
    { path: 'login', component: LoginComponent, title: 'Iniciar sesion - Intranet' },
    { path: '', canActivate: [sessionGuard], component: HomeComponent, title: 'Inicio · Intranet' },
    { path: 'logistica/avios', canActivate: [sessionGuard], loadChildren: () => import('./features/purchase-orders/purchase-orders.module').then(m => m.PurchaseOrdersModule) },
    { path: 'logistica/importaciones', canActivate: [sessionGuard], loadChildren: () => import('./features/imports/imports.module').then(m => m.ImportsModule) },
    { path: '**', redirectTo: '' }
];
@NgModule({ imports: [RouterModule.forRoot(routes)], exports: [RouterModule] })
export class AppRoutingModule {
}
