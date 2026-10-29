import { Component, DestroyRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { environment } from '../../../../../environments/environment';
import { AnalysisDraftService } from '../integrator/analysis-draft.service';
import { apiError } from '../../../../core/http/api-error';
interface Operation {sector:string;blockCode:string;blockName:string;sequence:string;code:string;variant:string;description:string;machineFamily:string;machineGroup:string;machineGroupName:string;category:string;familyType:string;standardTime:number;}
interface Operations {sectors:{code:string;name:string}[];selectedSector:string;np:string;tariff:string;styleName:string;assignedVariant:string;selectedVariant:string;variants:{code:string;description:string;status:string}[];rows:Operation[];totalTime:number;machineTime:number;}
@Component({selector:'app-sewing-operations',standalone:true,imports:[CommonModule,FormsModule,RouterLink],templateUrl:'./sewing-operations.component.html',styleUrl:'./sewing-operations.component.scss'})
export class SewingOperationsComponent {
 private router=inject(Router);private drafts=inject(AnalysisDraftService);
 private http=inject(HttpClient);private route=inject(ActivatedRoute);private destroy=inject(DestroyRef);
 np=(this.route.snapshot.queryParamMap.get('np')??'').trim().toUpperCase();busy=false;error='';result:Operations|null=null;variant='';sector='CS';blocks:{code:string;name:string;rows:Operation[];total:number}[]=[];
 trackVariant(_index:number,item:{code:string}){return item.code;}
 ngOnInit(){if(!/^[A-Z0-9]{5}$/.test(this.np)){this.error='Abre las operaciones desde la consulta de una NP válida.';return;}const previous=this.drafts.operationsLocation;if(previous?.np===this.np)this.load(previous.variant,previous.sector);else this.load();}
 get sectorName(){return this.result?.sectors.find(item=>item.code===this.sector)?.name??this.sector;}
 load(variant?:string,sector?:string){if(this.busy)return;this.remember();this.busy=true;this.error='';const params:Record<string,string>={np:this.np};if(variant)params['variant']=variant;if(sector)params['sector']=sector;this.http.get<Operations>(environment.apiBaseUrl+'/logistica/analisis-rb/costura',{params,withCredentials:true}).pipe(takeUntilDestroyed(this.destroy)).subscribe({next:result=>{this.result=result;this.variant=result.selectedVariant;this.sector=result.selectedSector;const groups=new Map<string,{code:string;name:string;rows:Operation[];total:number}>();for(const row of result.rows){let group=groups.get(row.blockCode);if(!group){group={code:row.blockCode,name:row.blockName,rows:[],total:0};groups.set(row.blockCode,group);}group.rows.push(row);group.total+=row.standardTime;}this.blocks=[...groups.values()];const previous=this.operationSelections.get(this.scope(result));const valid=new Set(result.rows.map(row=>this.key(row)));this.selected=new Set(previous?previous.filter(key=>valid.has(key)):valid);this.drafts.operationsLocation={np:this.np,variant:result.selectedVariant,sector:result.selectedSector};this.busy=false;},error:error=>{this.busy=false;this.result=null;this.blocks=[];this.error=apiError(error);}});}
 private operationSelections=new Map<string,string[]>();
 selected=new Set<string>();
 key(row:Operation){return JSON.stringify([row.blockCode,row.sequence,row.code,row.variant]);}
 private scope(data:Operations){return JSON.stringify([this.np,data.tariff,data.selectedVariant,data.selectedSector]);}
 private remember(){if(this.result)this.operationSelections.set(this.scope(this.result),[...this.selected]);}
 ngOnDestroy(){this.remember();}
 isSelected(row:Operation){return this.selected.has(this.key(row));}
 count(rows:Operation[]){return rows.filter(row=>this.isSelected(row)).length;}
 get selectedRows(){return this.result?.rows.filter(row=>this.isSelected(row))??[];}
 get selectedTime(){return this.sum(this.selectedRows);}
 sum(rows:Operation[]){return Math.round(rows.reduce((total,row)=>total+row.standardTime,0)*1e6)/1e6;}
 selectedBlockTime(rows:Operation[]){return this.sum(rows.filter(row=>this.isSelected(row)));}
 toggleRow(row:Operation,checked:boolean){if(this.busy)return;if(checked)this.selected.add(this.key(row));else this.selected.delete(this.key(row));this.remember();}
 toggleRows(rows:Operation[],checked:boolean){if(this.busy)return;for(const row of rows){if(checked)this.selected.add(this.key(row));else this.selected.delete(this.key(row));}this.remember();}
 next(){if(this.busy||!this.result||!this.selectedRows.length||this.selectedTime<=0)return;this.remember();const isComplete=this.selectedRows.length===this.result.rows.length;this.drafts.pendingAnalysis={np:this.np,sectorAnalysis:{tariff:this.result.tariff,variant:this.result.selectedVariant,sector:this.result.selectedSector,sectorName:this.sectorName,totalTime:this.result.totalTime,totalOperations:this.result.rows.length,selectedOperations:this.selectedRows.length,isComplete},selection:isComplete?null:{tariff:this.result.tariff,variant:this.result.selectedVariant,sector:this.result.selectedSector,sectorName:this.sectorName,sectorTotalTime:this.result.totalTime,operations:this.selectedRows.map(row=>({blockCode:row.blockCode,blockName:row.blockName,sequence:row.sequence,code:row.code,variant:row.variant,description:row.description,standardTime:row.standardTime}))}};void this.router.navigate(['/logistica/analiticas'],{queryParams:{np:this.np}});}
}

