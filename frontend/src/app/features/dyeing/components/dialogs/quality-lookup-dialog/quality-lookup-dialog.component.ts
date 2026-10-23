import { CommonModule } from '@angular/common';
import { Component, DestroyRef, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, Observable } from 'rxjs';
import { QualityControlApiService } from '../../../services/quality-control-api.service';
import { DyeingCatalogOption, DyeingRow } from '../../../models/dyeing.models';
import { apiError } from '../../../../../core/http/api-error';
export interface QualityLookupData { mode:'fabric'|'consumption'; batch:string; sequence:number; }
@Component({selector:'app-quality-lookup-dialog',standalone:true,imports:[CommonModule,FormsModule,MatDialogModule],templateUrl:'./quality-lookup-dialog.component.html',styleUrl:'./quality-lookup-dialog.component.scss'})
export class QualityLookupDialogComponent {
 readonly data=inject<QualityLookupData>(MAT_DIALOG_DATA);
 private ref=inject(MatDialogRef<QualityLookupDialogComponent,DyeingCatalogOption>);
 private api=inject(QualityControlApiService);private destroyRef=inject(DestroyRef);
 search='';busy=false;error='';searched=false;fabrics:DyeingCatalogOption[]=[];rows:DyeingRow[]=[];
 readonly columns=[['oc','O/C corte'],['num_prendas_prog','Prendas programadas'],['num_prendas_reales','Prendas reales'],['caida','Caídas'],['cod_compest','Componente']];
 constructor(){if(this.data.mode==='consumption')this.load();}
 load(){if(this.busy)return;this.busy=true;this.error='';this.searched=false;this.fabrics=[];this.rows=[];
  const action: Observable<DyeingCatalogOption[] | DyeingRow[]> = this.data.mode==='fabric'?this.api.fabrics(this.search.trim()):this.api.consumption(this.data.batch,this.data.sequence);
  action.pipe(finalize(()=>this.busy=false),takeUntilDestroyed(this.destroyRef)).subscribe({next:rows=>{this.searched=true;if(this.data.mode==='fabric')this.fabrics=rows as DyeingCatalogOption[];else this.rows=rows as DyeingRow[];},error:e=>this.error=apiError(e)});
 }
 choose(option:DyeingCatalogOption){this.ref.close(option);}
}
