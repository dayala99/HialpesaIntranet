import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CatalogComboboxComponent } from './components/catalog-combobox/catalog-combobox.component';
import { DatePickerDirective } from './directives/date-picker.directive';
@NgModule({ declarations: [CatalogComboboxComponent, DatePickerDirective], imports: [CommonModule, FormsModule], exports: [CatalogComboboxComponent, DatePickerDirective] })
export class SharedModule {
}
