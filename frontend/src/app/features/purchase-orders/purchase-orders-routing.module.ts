import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AviosComponent } from './pages/purchase-orders/avios.component';
const routes: Routes = [{ path: 'nueva', component: AviosComponent, data: { newOrder: true }, title: 'Nueva orden de compra · Intranet' }, { path: '', component: AviosComponent, title: 'Consulta OC y PO · Intranet' }];
@NgModule({ imports: [RouterModule.forChild(routes)], exports: [RouterModule] })
export class PurchaseOrdersRoutingModule {
}
