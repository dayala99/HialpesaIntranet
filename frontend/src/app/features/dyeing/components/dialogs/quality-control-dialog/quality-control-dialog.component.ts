import { MatIconModule } from '@angular/material/icon';
import { CommonModule } from '@angular/common';
import { Component, DestroyRef, EventEmitter, Input, Output, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { QualityControlApiService } from '../../../services/quality-control-api.service';
import { QUALITY_FIELDS, QualityControlScreen, QualityControlDialogData, QualityControlDialogResult, QualityField, QualityNumberField } from '../../../models/quality-control.models';
import { DyeingCatalogOption } from '../../../models/dyeing.models';
import { QualityLookupData, QualityLookupDialogComponent } from '../quality-lookup-dialog/quality-lookup-dialog.component';
import { apiError } from '../../../../../core/http/api-error';
@Component({selector:'app-quality-control-dialog',standalone:true,imports:[CommonModule,FormsModule,MatDialogModule,MatIconModule],templateUrl:'./quality-control-dialog.component.html',styleUrl:'./quality-control-dialog.component.scss'})
export class QualityControlDialogComponent implements OnInit {
 @Input({required:true}) data!:QualityControlDialogData;
 @Output() completed=new EventEmitter<QualityControlDialogResult>();
 
 private api=inject(QualityControlApiService);private dialog=inject(MatDialog);private destroyRef=inject(DestroyRef);
 screen:QualityControlScreen|null=null;loading=false;saving=false;error='';alternativeDescription='';
 copyRamaValues=false;
 readonly numericFields=QUALITY_FIELDS;
 ngOnInit(){this.load();}
 load(){if(this.loading)return;this.loading=true;this.error='';this.api.read(this.data.batch,this.data.sequence).pipe(finalize(()=>this.loading=false),takeUntilDestroyed(this.destroyRef)).subscribe({next:screen=>{this.screen=screen;this.alternativeDescription=screen.detail.programmed.alternativeDescription;if(screen.detail.values.approval==='A')screen.detail.values.rejectedWeight=0;},error:e=>this.error=apiError(e)});}
 fields(section:string){
  if(section==='ramaMeasurements')return this.numericFields.filter(f=>f.section==='rama'&&!['ramaShrinkWidth','ramaShrinkLength'].includes(f.key));
  if(section==='ramaShrinkage')return this.numericFields.filter(f=>['ramaShrinkWidth','ramaShrinkLength'].includes(f.key));
  if(section==='residualShrinkage')return this.numericFields.filter(f=>['residualShrinkWidth','residualShrinkLength'].includes(f.key));
  return this.numericFields.filter(f=>f.section===section);
 }
 selectNumber(event:Event){const input=event.target as HTMLInputElement;input.select();}
 measurement(key:QualityNumberField):QualityField{return this.numericFields.find(f=>f.key===key)!;}
 numberValue(field:QualityField){return this.screen ? this.screen.detail.values[field.key] : null;}
 updateNumber(field:QualityField,value:number){if(this.screen)this.screen.detail.values[field.key]=value;}
 options(kind:string){return this.screen?.catalogs.filter(o=>o.kind===kind)||[];}
 shrinkTypeDescription(){return this.options('shrinkTypes').find(option=>option.code===this.screen?.detail.values.shrinkType)?.description || 'Selecciona el tipo en Real';}
 approvalChanged(){if(this.screen)this.screen.detail.values.rejectedWeight=0;}
 copyRama(){if(!this.screen?.detail.canEdit||this.saving)return;const v=this.screen.detail.values;v.weight=v.ramaWeight;v.width=v.ramaWidth;v.twist=v.ramaTwist;v.washedWeight=v.ramaWashedWeight;v.shrinkWidth=v.ramaShrinkWidth;v.shrinkLength=v.ramaShrinkLength;v.shrinkType=v.ramaType;}
 openLookup(mode:'fabric'|'consumption'){
  if(this.saving)return;
  this.dialog.open<QualityLookupDialogComponent,QualityLookupData,DyeingCatalogOption>(QualityLookupDialogComponent,{width:'780px',maxWidth:'95vw',data:{mode,batch:this.data.batch,sequence:this.data.sequence},restoreFocus:true}).afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(option=>{if(mode==='fabric'&&option&&this.screen?.detail.canEdit){this.screen.detail.values.alternativeFabric=option.code;this.alternativeDescription=option.description;}});
 }
 clearFabric(){if(this.screen){this.screen.detail.values.alternativeFabric='';this.alternativeDescription='';}}
 save(){
  if(!this.screen?.detail.canEdit||this.saving)return;
  const detail=this.screen.detail,v=detail.values;this.error='';
  for(const field of this.numericFields){const value=v[field.key];if(typeof value!=='number'||!Number.isFinite(value)||value<field.min||value>field.max){this.error=field.label+': ingresa un valor válido.';return;}}
  if(!v.widthType||!v.shrinkType||!v.ramaType){this.error='Selecciona el tipo de ancho y los tipos de encogimiento.';return;}
  if(v.approval==='D'&&v.rejectedWeight<=0){this.error='Ingresa los kilos rechazados para una desaprobación.';return;}
  this.saving=true;
  this.api.save(detail.batch,detail.sequence,{values:{...v},expectedModifiedAt:detail.modifiedAt,recommendations:detail.recommendations.filter(r=>r.selected).map(r=>r.code)}).pipe(finalize(()=>{this.saving=false;}),takeUntilDestroyed(this.destroyRef)).subscribe({next:()=>this.completed.emit({saved:true}),error:e=>this.error=apiError(e)});
 }
}
