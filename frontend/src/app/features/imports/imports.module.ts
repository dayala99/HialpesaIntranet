import { MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ImportsComponent } from './imports.component';
@NgModule({declarations:[ImportsComponent],imports: [MatDialogModule, MatIconModule, CommonModule,FormsModule,RouterModule.forChild([{path:'',component:ImportsComponent,title:'Importaciones · Intranet'}])]})
export class ImportsModule {}
