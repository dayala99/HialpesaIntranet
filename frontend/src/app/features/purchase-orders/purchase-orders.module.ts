import { PurchaseOrdersRoutingModule } from './purchase-orders-routing.module';
import { SharedModule } from '../../shared/shared.module';
import { MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AviosComponent } from './pages/purchase-orders/avios.component';
import { OrderRegistrationComponent } from './components/order-registration/order-registration.component';
import { OrderPrintComponent } from './components/order-print/order-print.component';
@NgModule({ declarations: [AviosComponent, OrderRegistrationComponent, OrderPrintComponent], imports: [SharedModule, MatDialogModule, MatIconModule, CommonModule, FormsModule, PurchaseOrdersRoutingModule] })
export class PurchaseOrdersModule {
}
