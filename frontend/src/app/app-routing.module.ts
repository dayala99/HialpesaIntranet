import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './home/home.component';
const routes: Routes = [
  { path: '', component: HomeComponent, title: 'Inicio · Intranet' },
  { path: 'logistica/avios', loadChildren: () => import('./features/avios/avios.module').then(m => m.AviosModule) },
  { path: 'logistica/importaciones', loadChildren: () => import('./features/imports/imports.module').then(m => m.ImportsModule) },
  { path: '**', redirectTo: '' }
];
@NgModule({ imports: [RouterModule.forRoot(routes)], exports: [RouterModule] })
export class AppRoutingModule {}
