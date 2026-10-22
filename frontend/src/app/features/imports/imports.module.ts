import { ImportsRoutingModule } from './imports-routing.module';
import { SharedModule } from '../../shared/shared.module';
import { MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ImportsComponent } from './pages/imports/imports.component';
@NgModule({ declarations: [ImportsComponent], imports: [SharedModule, MatDialogModule, MatIconModule, CommonModule, FormsModule, ImportsRoutingModule] })
export class ImportsModule {
}
