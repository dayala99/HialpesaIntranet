import {Component,Input,Output,EventEmitter,inject} from '@angular/core';
import {MatDialog} from '@angular/material/dialog';
import {ControlFilter,ControlOption,newControlFilter} from '../../models/import-control.models';
import {AnnexLookupComponent} from '../dialogs/annex-lookup/annex-lookup.component';
@Component({selector:'app-import-control-filters',templateUrl:'./control-filters.component.html',styleUrl:'./control-filters.component.scss'})
export class ControlFiltersComponent {
 @Input() options:ControlOption[]=[];@Input() busy=false;@Output() searched=new EventEmitter<ControlFilter>();
 @Input() filter=newControlFilter();annexName='';private dialog=inject(MatDialog);
 modes=[['1','Núm. importación'],['7','Anexo contable'],['8','Estado'],['9','Número de guía'],['3','Documento de referencia'],['0','Lugar de origen'],['A','Por ítem'],['D','País DUA'],['C','Importación asociada'],['6','Todas'],['4','Póliza de importación'],['2','Período'],['5','Tipo de ítem'],['B','Por NP']];
 get noCriteria(){const f=this.filter;const keys:Record<string,string[]>={'1':['number'],'2':['year','month'],'3':['documentNumber','documentSeries'],'4':['policy'],'5':['itemType'],'7':['annex'],'8':['status'],'9':['guide'],'0':['origin'],'A':['item'],'B':['factory','productionOrder'],'C':['associated'],'D':['country']};return !(keys[f.mode]??[]).some(key=>!!String((f as unknown as Record<string,unknown>)[key]??'').trim()&&String((f as unknown as Record<string,unknown>)[key])!=='0')&&!f.from&&!f.to;}
 get ranged(){return ['0','5','7','8','D'].includes(this.filter.mode);}
 choices(kind:string){return this.options.filter(o=>o.kind===kind);}
 changeMode(){this.filter.from='';this.filter.to='';}
 lookup(){this.dialog.open(AnnexLookupComponent,{width:'680px',maxWidth:'95vw',data:{type:this.filter.annexType}}).afterClosed().subscribe(r=>{if(r){this.filter.annex=r.code;this.annexName=r.description;}});}
 submit(){this.searched.emit({...this.filter,page:1});}
}
