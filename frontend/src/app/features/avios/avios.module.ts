import { CatalogComboboxComponent } from './catalog-combobox.component';
import { MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AviosComponent } from './avios.component';
import { OrderRegistrationComponent } from './order-registration.component';
import { OrderPrintComponent } from './order-print.component';
@NgModule({ declarations: [CatalogComboboxComponent, AviosComponent, OrderRegistrationComponent, OrderPrintComponent], imports: [MatDialogModule, MatIconModule, CommonModule, FormsModule, RouterModule.forChild([{ path: 'nueva', component: AviosComponent, data: { newOrder: true }, title: 'Nueva orden de compra · Intranet' }, { path: '', component: AviosComponent, title: 'Consulta OC y PO · Intranet' }])] })
export class AviosModule {}
