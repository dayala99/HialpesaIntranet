import {forkJoin,of} from 'rxjs';
import {Component,inject,DestroyRef} from '@angular/core';import {CommonModule} from '@angular/common';import {FormsModule,NgForm} from '@angular/forms';import {MAT_DIALOG_DATA,MatDialogModule,MatDialogRef} from '@angular/material/dialog';import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {SharedModule} from '../../../../../shared/shared.module';import {ControlConcept,ControlOption} from '../../../models/import-control.models';import {ImportControlApiService} from '../../../services/import-control-api.service';import {SessionService} from '../../../../../core/auth/session.service';import {apiError} from '../../../../../core/http/api-error';
@Component({selector:'app-import-concept-editor',standalone:true,imports:[CommonModule,FormsModule,MatDialogModule,SharedModule],templateUrl:'./concept-editor.component.html',styles:[`.fields{display:grid;grid-template-columns:repeat(3,minmax(0,1fr));gap:18px;padding:8px 0 20px}.concept{display:grid;grid-template-columns:64px minmax(0,1fr);gap:10px;align-items:end;grid-column:1/-1}.concept-code input{height:46px}input{width:100%;min-width:0}.rate{grid-column:2}.equivalence{grid-column:3}.document{grid-column:1/-1;max-width:300px}.muted{margin-bottom:6px}.feedback{margin-top:12px}@media(max-width:480px){.fields{grid-template-columns:repeat(2,minmax(0,1fr));gap:16px}.rate,.equivalence{grid-column:auto}}`]})
export class ConceptEditorComponent {
 data=inject<{row:ControlConcept|null;options:ControlOption[];used:string[];date:string;exchangeRate?:number;documentNumber?:string}>(MAT_DIALOG_DATA);row:ControlConcept=this.data.row?{...this.data.row,documentNumber:this.data.row.documentNumber?.trim()?this.data.row.documentNumber:this.data.documentNumber??''}:{code:'',description:'',dollars:0,soles:0,exchangeRate:this.data.exchangeRate??0,other:0,documentNumber:this.data.documentNumber??''};equivalence=0;otherEdited=false;loading=false;error='';private destroy=inject(DestroyRef);private api=inject(ImportControlApiService);private session=inject(SessionService);private ref=inject(MatDialogRef<ConceptEditorComponent>);
 constructor(){if(!this.data.row&&this.data.date){this.loading=true;forkJoin({equivalence:this.api.documentChoices('conceptEquiv','','',this.data.date.slice(0,10)),rate:this.row.exchangeRate>0?of([{exchangeRate:this.row.exchangeRate}]):this.api.documentChoices('exchangeRate','','',this.data.date.slice(0,10))}).pipe(takeUntilDestroyed(this.destroy)).subscribe({next:result=>{this.equivalence=Number(result.equivalence[0]?.['equivalence']??0);this.row.exchangeRate=Number(result.rate[0]?.['exchangeRate']??0);this.loading=false;},error:e=>{this.error=apiError(e);this.loading=false;}});}}
 nextField(event:KeyboardEvent){
  if(event.key!=='Enter'||event.defaultPrevented||event.isComposing||event.ctrlKey||event.altKey||event.metaKey)return;
  const current=event.target;
  if(!(current instanceof HTMLInputElement)||current.readOnly||current.disabled||['button','submit','checkbox','radio','hidden'].includes(current.type))return;
  event.preventDefault();
  const form=event.currentTarget as HTMLFormElement;
  const fields=Array.from(form.querySelectorAll<HTMLInputElement>('input')).filter(input=>!input.disabled&&!input.readOnly&&!['button','submit','checkbox','radio','hidden'].includes(input.type)&&input.tabIndex>=0&&input.getClientRects().length>0);
  const index=fields.indexOf(current);const next=fields[index+(event.shiftKey?-1:1)];
  if(index>=0&&next){next.focus();if(['text','number','search','tel'].includes(next.type))next.select();}
 }
 get isGlobalUser(){return this.session.current()?.gestionUser?.trim().toLowerCase()==='programador04';}
 get options(){const list=this.data.options.filter(o=>!this.data.used.includes(o.code)||o.code===this.row.code);return this.row.code&&!list.some(o=>o.code===this.row.code)?[...list,{kind:'concepts',code:this.row.code,description:this.row.description}]:list;}
 description(){this.row.description=this.options.find(o=>o.code===this.row.code)?.description??'';}
 fromDollars(){if(this.row.exchangeRate>0&&Number.isFinite(this.row.dollars))this.row.soles=Math.round(this.row.dollars*this.row.exchangeRate*100)/100;}
 fromSoles(){if(this.row.exchangeRate>0&&Number.isFinite(this.row.soles))this.row.dollars=Math.round(this.row.soles/this.row.exchangeRate*100)/100;}
 fromOther(){if(!this.otherEdited)return;this.otherEdited=false;if(Number.isFinite(this.row.other)&&this.equivalence>=0){this.row.dollars=Math.round(this.row.other*this.equivalence*100)/100;this.fromDollars();}}
 accept(form:NgForm){if(this.loading){this.error='Espera a que termine la consulta de equivalencia.';return;}form.form.markAllAsTouched();if(form.invalid){this.error='Selecciona un concepto y revisa los importes, tipo de cambio y equivalencia.';return;}this.ref.close(this.row);}
}
