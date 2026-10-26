import {SharedModule} from '../../../../shared/shared.module';
import {MatDialog,MatDialogModule} from '@angular/material/dialog';
import {ConfirmationDialogComponent,ConfirmationDialogData,ConfirmationDialogResult} from '../../../../shared/components/dialogs/confirmation-dialog/confirmation-dialog.component';
import {CommonModule} from '@angular/common';
import {Component,Input,Output,EventEmitter,OnInit,inject,DestroyRef} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {MatIconModule} from '@angular/material/icon';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {finalize} from 'rxjs';
import {QualityEventsApiService} from '../../services/quality-events-api.service';
import {EventOption,EventRow,EventUpdate} from '../../models/quality-event.models';
import {apiError} from '../../../../core/http/api-error';
@Component({selector:'app-event-register',standalone:true,imports:[CommonModule,FormsModule,MatIconModule,MatDialogModule,SharedModule],templateUrl:'./event-register.component.html',styleUrl:'./event-register.component.scss'})
export class EventRegisterComponent implements OnInit {
 @Input() row:EventRow|null=null;@Input() action='I';@Input() options:EventOption[]=[];@Input() initialEvent='';@Input() initialDate='';@Output() completed=new EventEmitter<boolean>();
 private dialog=inject(MatDialog);private api=inject(QualityEventsApiService);private destroyRef=inject(DestroyRef);busy=false;loading=false;error='';items:EventRow[]=[];loadedBatch='';customer='';
 readonly unfinished=this.api.unfinished;
 model!:EventUpdate;
 get title(){return this.action==='I'?'Adicionar evento':this.action==='U'?'Modificar evento':'Eliminar evento';}
 catalog(kind:string){return this.options.filter(x=>x.kind===kind);}
 ngOnInit(){const r=this.row||{};this.model={action:this.action,batch:r['cod_ordtra']||'',eventCode:r['cod_evento_cc']||this.initialEvent,eventSequence:Number(r['num_secuencia_cc']||0),date:r['fec_evento']?String(r['fec_evento']).slice(0,10):this.initialDate,status:r['flg_status_cc']||'A',auditor:String(r['cod_auditor']||'').replace('-',''),observations:r['observaciones']||'',reason:r['cod_motivo_cc']||'',externalResult:String(r['auditoria_externa']||'').slice(0,1).trim(),location:String(r['cod_ubicacion_reproceso']||'').split('-')[0].trim(),productionObservations:r['observacion_produccion']||'',deviation:r['nro_desviacion']||'',cause:r['nro_causa']||'',forcedError:r['flg_error_forzado_area']==='S',dispatched:r['flg_despachado']==='S',confirmApproved:false,items:[],expectedVersion:r['version']||null};this.customer=[r['abr_cliente'],r['nom_cliente']].filter(Boolean).join(' · ');if(this.action!=='I')this.items=[{sec:Number(r['num_secuencia']),kilos:Number(r['kilos']),selected:true,des_tela:r['tela'],des_color:r['color'],talla:r['talla']}];}
 get allSelected(){return this.items.length>0&&this.items.every(i=>i['selected']);}
 get someSelected(){return !this.allSelected&&this.items.some(i=>i['selected']);}
 selectAll(selected:boolean){if(this.action==='I')this.items.forEach(i=>i['selected']=selected);}
 selectNumber(e:Event){(e.target as HTMLInputElement).select();}
 batchChanged(){this.items=[];this.loadedBatch='';this.customer='';}
 loadItems(){if(this.loading||this.busy)return;const batch=this.model.batch.trim().toUpperCase();if(!/^[A-Z0-9]{1,5}$/.test(batch)){this.error='Ingresa una partida válida.';return;}this.loading=true;this.error='';this.api.items(batch).pipe(finalize(()=>this.loading=false),takeUntilDestroyed(this.destroyRef)).subscribe({next:result=>{const items=result.items;this.customer=result.customer?result.customer.code+' · '+result.customer.name:'';this.model.batch=batch;this.loadedBatch=batch;this.items=items.map(x=>({...x,selected:true}));if(!items.length)this.error='La partida no tiene ítems disponibles.';},error:e=>this.error=apiError(e)});}
 statusChanged(){if(!['R','P'].includes(this.model.status)){this.model.reason='';this.model.deviation='';this.model.cause='';}}
 save(confirmed=false){if(this.busy||this.loading)return;this.error='';const m=this.model;if(this.action==='I'&&this.loadedBatch!==m.batch.trim().toUpperCase()){this.error='Consulta los ítems de la partida antes de guardar.';return;}m.items=this.items.filter(i=>i['selected']).map(i=>({sequence:Number(i['sec']),kilos:Number(i['kilos'])}));if(!m.items.length){this.error='Selecciona al menos un ítem.';return;}if(this.action!=='D'&&(!m.eventCode||!m.status||!m.auditor||!m.location)){this.error='Completa evento, estado, auditor y ubicación de reproceso.';return;}this.busy=true;this.api.save({...m,confirmApproved:confirmed}).pipe(finalize(()=>this.busy=false),takeUntilDestroyed(this.destroyRef)).subscribe({next:()=>this.completed.emit(true),error:e=>{this.error=apiError(e);if(e.status===409&&this.error.includes('El evento está aprobado.')&&!confirmed){this.dialog.open<ConfirmationDialogComponent,ConfirmationDialogData,ConfirmationDialogResult>(ConfirmationDialogComponent,{width:'480px',maxWidth:'95vw',data:{title:'Evento aprobado',message:'La partida tiene un evento aprobado para esta selección. ¿Deseas continuar de todas maneras?',confirmLabel:'Continuar',cancelLabel:'Cancelar'}}).afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(ok=>{if(ok)this.save(true);});}}});}
}
