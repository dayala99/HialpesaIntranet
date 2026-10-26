import {Component,inject,DestroyRef} from '@angular/core';
import {MatDialog} from '@angular/material/dialog';import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {ConfirmationDialogComponent,ConfirmationDialogData} from '../../../../shared/components/dialogs/confirmation-dialog/confirmation-dialog.component';
import {ImportControlApiService} from '../../services/import-control-api.service';
import {ControlCatalogs,ControlDetail,ControlFilter,ControlRow,newControlFilter} from '../../models/import-control.models';
import {SessionService} from '../../../../core/auth/session.service';import {apiError} from '../../../../core/http/api-error';
@Component({selector:'app-imports',templateUrl:'./imports.component.html',styleUrl:'./imports.component.scss'})
export class ImportsComponent {
 private api=inject(ImportControlApiService);private dialog=inject(MatDialog);private destroy=inject(DestroyRef);session=inject(SessionService);
 catalogs:ControlCatalogs|null=null;filter=newControlFilter();private applied:ControlFilter|null=null;
 rows:ControlRow[]=[];selected:ControlRow|null=null;busy=false;loading=true;error='';message='';searched=false;editor=false;detail:ControlDetail|null=null;page=1;totalPages=1;totalRows=0;hasMore=false;
 ngOnInit(){this.api.catalogs().pipe(takeUntilDestroyed(this.destroy)).subscribe({next:r=>{this.catalogs=r;this.loading=false;this.search(this.filter);},error:e=>{this.error=apiError(e);this.loading=false;}});}
 search(filter:ControlFilter){if(this.busy)return;this.applied={...filter};this.load(filter.page);}
 load(page=1){if(!this.applied)return;this.busy=true;this.error='';this.rows=[];this.selected=null;this.api.search({...this.applied,page}).pipe(takeUntilDestroyed(this.destroy)).subscribe({next:r=>{this.rows=r.rows;this.selected=r.rows[0]??null;this.page=r.page;this.totalPages=r.totalPages;this.totalRows=r.totalRows;this.hasMore=r.hasMore;this.busy=false;this.searched=true;},error:e=>{this.error=apiError(e);this.busy=false;}});}
 add(){if(this.busy||!this.catalogs||!this.session.current()?.canWriteDatabase)return;this.detail=null;this.editor=true;this.error='';this.message='';}
 edit(){if(!this.selected||this.busy)return;this.busy=true;this.error='';this.api.detail(this.selected.number).pipe(takeUntilDestroyed(this.destroy)).subscribe({next:r=>{this.detail=r;this.editor=true;this.busy=false;},error:e=>{this.error=apiError(e);this.busy=false;}});}
 back(){this.editor=false;this.detail=null;}
 saved(number:string){this.back();this.message='Importación '+number+' guardada correctamente.';this.filter={...newControlFilter(),number};this.applied={...this.filter};this.load();}
 provision(){if(!this.selected||this.busy||!this.session.current()?.canWriteDatabase)return;const number=this.selected.number;this.busy=true;this.error='';this.message='';this.api.detail(number).pipe(takeUntilDestroyed(this.destroy)).subscribe({next:detail=>{
 const action=detail.status==='R'?'provision':detail.status==='P'?'reverse':null;if(!action){this.error='El estado de la importación no permite provisionar ni revertir la provisión.';this.busy=false;return;}
 const title=action==='provision'?'Provisionar importación '+number:'Revertir provisión de importación '+number;
 const message=action==='provision'?'¿Deseas provisionar la importación '+number+' de '+detail.annexName+'? Se generará el asiento contable de provisión.':'¿Deseas revertir la provisión de la importación '+number+' de '+detail.annexName+'? Se revertirá su asiento contable de provisión.';
 this.dialog.open<ConfirmationDialogComponent,ConfirmationDialogData,boolean>(ConfirmationDialogComponent,{width:'520px',maxWidth:'95vw',disableClose:true,data:{title,message,confirmLabel:action==='provision'?'Provisionar':'Revertir'}}).afterClosed().pipe(takeUntilDestroyed(this.destroy)).subscribe(confirmed=>{if(confirmed!==true){this.busy=false;return;}this.api.provision(number,detail.version,action).pipe(takeUntilDestroyed(this.destroy)).subscribe({next:()=>{this.busy=false;this.message=action==='provision'?'Importación '+number+' provisionada correctamente.':'Provisión de la importación '+number+' revertida correctamente.';this.load(1);},error:e=>{this.error=apiError(e);this.busy=false;}});});
 },error:e=>{this.error=apiError(e);this.busy=false;}});}
 remove(){if(!this.selected||this.busy||!this.session.current()?.canWriteDatabase)return;this.busy=true;this.error='';this.message='';this.api.detail(this.selected.number).pipe(takeUntilDestroyed(this.destroy)).subscribe({next:detail=>{
  if(!detail.canDelete){this.error='No se puede eliminar esta importación. Revisa su estado, documentos, pagos o liquidaciones asociadas en Gestión.';this.busy=false;return;}
  this.dialog.open<ConfirmationDialogComponent,ConfirmationDialogData,boolean>(ConfirmationDialogComponent,{width:'500px',maxWidth:'95vw',disableClose:true,data:{title:'Eliminar importación '+detail.number,message:'¿Deseas eliminar la importación '+detail.number+' de '+detail.annexName+'? Se eliminará el registro y se retirará su vínculo con las OC asociadas. Esta acción no se puede deshacer desde esta pantalla.',confirmLabel:'Eliminar'}}).afterClosed().pipe(takeUntilDestroyed(this.destroy)).subscribe(confirmed=>{
   if(confirmed!==true){this.busy=false;return;}this.api.remove(detail.number,detail.version).pipe(takeUntilDestroyed(this.destroy)).subscribe({next:()=>{this.busy=false;this.message='Importación '+detail.number+' eliminada correctamente.';this.load(1);},error:e=>{this.error=apiError(e);this.busy=false;}});
  });
 },error:e=>{this.error=apiError(e);this.busy=false;}});}
}
