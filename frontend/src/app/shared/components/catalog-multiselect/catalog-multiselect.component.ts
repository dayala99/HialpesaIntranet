import {Component,Input,Output,EventEmitter} from '@angular/core';
import {CommonModule} from '@angular/common';
import {OverlayModule} from '@angular/cdk/overlay';
interface Option{code:string;description:string;}
let nextId=0;
@Component({selector:'app-catalog-multiselect',standalone:true,imports:[CommonModule,OverlayModule],template:`
<label [for]="id">{{label}}</label>
<div class="control" cdkOverlayOrigin #origin="cdkOverlayOrigin"><input [id]="id" [attr.aria-label]="'Filtrar '+label" role="combobox" aria-autocomplete="list" aria-haspopup="dialog" [attr.aria-expanded]="open" [attr.aria-controls]="id+'-options'" [value]="open?query:summary" (focus)="show(origin.elementRef.nativeElement)" (input)="query=$any($event.target).value" (keydown.enter)="$event.preventDefault()" (keydown.escape)="open=false" [disabled]="disabled" placeholder="Escribe código o descripción" autocomplete="off"><button type="button" [disabled]="disabled" [attr.aria-label]="'Mostrar '+label" (click)="open?open=false:show(origin.elementRef.nativeElement)">▾</button></div>
<small>{{value.length}} seleccionados</small>
<ng-template cdkConnectedOverlay [cdkConnectedOverlayOrigin]="origin" [cdkConnectedOverlayOpen]="open&&!disabled" [cdkConnectedOverlayWidth]="width" [cdkConnectedOverlayHasBackdrop]="false" (overlayOutsideClick)="outside($event,origin.elementRef.nativeElement)" (overlayKeydown)="$event.key==='Escape'&&(open=false)">
<div class="options" role="dialog" [id]="id+'-options'" [attr.aria-label]="label+' selección múltiple'"><div class="toolbar"><label><input type="checkbox" [checked]="all" [indeterminate]="some&&!all" (change)="toggleAll(!all)" [attr.aria-label]="'Seleccionar todos '+label"> Seleccionar todos {{query?'los filtrados':''}}</label><button type="button" (click)="emit([])">Limpiar</button><button type="button" (click)="open=false">Cerrar</button></div>
<div class="list"><label *ngFor="let option of filtered"><input type="checkbox" [checked]="value.includes(option.code)" (change)="toggle(option.code,$any($event.target).checked)" [attr.aria-label]="label+' '+option.code"><strong>{{option.code}}</strong><span>{{option.description}}</span></label><p *ngIf="!filtered.length">No hay coincidencias.</p></div></div></ng-template>
`,styles:[`:host{display:block;min-width:0}label{display:block;font-size:11px;color:var(--muted);margin-bottom:8px}.control{display:flex}.control input{min-width:0;width:100%;border-radius:6px 0 0 6px}.control>button{border-radius:0 6px 6px 0;min-width:38px}small{display:block;font-size:11px;color:var(--muted);margin-top:5px}.options{background:#fff;border:1px solid #cfdbd7;border-radius:6px;box-shadow:0 8px 20px #102d3220;width:100%}.toolbar{display:flex;align-items:center;gap:8px;padding:10px;border-bottom:1px solid #cfdbd7;flex-wrap:wrap}.toolbar label{margin:0;flex:1;min-width:120px;display:flex;align-items:center;gap:8px}.toolbar button{font-size:11px;padding:6px 9px}.list{max-height:280px;overflow:auto}.list label{display:flex;align-items:center;gap:10px;margin:0;padding:12px;color:#1b3135;cursor:pointer;font-size:12px;overflow-wrap:anywhere}.list label:hover{background:#edf5f1}.list strong{flex:none}.list p{padding:12px;font-size:12px}input[type=checkbox]{width:18px;height:18px;min-height:18px;padding:0;flex:none;accent-color:var(--accent)}`]})
export class CatalogMultiselectComponent{
 @Input()label='';@Input()options:Option[]=[];@Input()value:string[]=[];@Input()disabled=false;@Output()valueChange=new EventEmitter<string[]>();
 id='catalog-multi-'+nextId++;open=false;query='';width=300;
 private normalize(value:string){return value.normalize('NFD').replace(/[\u0300-\u036f]/g,'').toLowerCase().trim();}
 get filtered(){const q=this.normalize(this.query);return this.options.filter(x=>!q||this.normalize(x.code+' '+x.description).includes(q));}
 get summary(){if(!this.value.length)return '';if(this.value.length===1)return this.options.find(x=>x.code===this.value[0])?.code+' · '+(this.options.find(x=>x.code===this.value[0])?.description??'');return this.value.length+' seleccionados';}
 get all(){return this.filtered.length>0&&this.filtered.every(x=>this.value.includes(x.code));}get some(){return this.filtered.some(x=>this.value.includes(x.code));}
 outside(event:MouseEvent,origin:HTMLElement){if(!origin.contains(event.target as Node))this.open=false;}
 show(element:HTMLElement){if(this.disabled)return;this.width=element.getBoundingClientRect().width;this.query='';this.open=true;}
 emit(value:string[]){if(!this.disabled)this.valueChange.emit([...new Set(value)]);}
 toggle(code:string,checked:boolean){this.emit(checked?[...this.value,code]:this.value.filter(x=>x!==code));}
 toggleAll(checked:boolean){const codes=new Set(this.filtered.map(x=>x.code));this.emit(checked?[...this.value,...codes]:this.value.filter(x=>!codes.has(x)));}
}
