import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ImportsComponent } from './pages/imports/imports.component';
const routes: Routes = [{ path: '', component: ImportsComponent, title: 'Control de Importaciones · Intranet' }];
@NgModule({ imports: [RouterModule.forChild(routes)], exports: [RouterModule] })
export class ImportsRoutingModule {
}
