import { Component, DestroyRef, inject } from '@angular/core';
import { SelectedProcessesComponent } from '../integrator/selected-processes.component';
import { AnalysisPdfExportComponent } from '../integrator/analysis-pdf-export.component';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { environment } from '../../../../../environments/environment';
import { apiError } from '../../../../core/http/api-error';
import { AnalysisHeaderComponent } from '../integrator/analysis-header.component';
import { HistoryPage, HistoryRow, SavedSnapshot } from '../integrator/analysis.models';
@Component({selector:'app-analysis-history',standalone:true,imports:[CommonModule,FormsModule,RouterLink,AnalysisHeaderComponent,SelectedProcessesComponent,AnalysisPdfExportComponent],templateUrl:'./analysis-history.component.html',styleUrl:'../integrator/integrator.component.scss'})
export class AnalysisHistoryComponent {
 private http=inject(HttpClient);private destroy=inject(DestroyRef);private route=inject(ActivatedRoute);
 private url=environment.apiBaseUrl+'/logistica/analisis-rb/historial';
 np=(this.route.snapshot.queryParamMap.get('np')??'').trim().toUpperCase();
 busy=false;error='';rows:HistoryRow[]=[];page=1;totalPages=1;viewedHistory:HistoryRow|null=null;snapshot:SavedSnapshot|null=null;
 get analysisTotalTime(){return this.snapshot?.sectorAnalysis?.totalTime??this.current?.sewingTime??null;}
 get current(){return this.snapshot?.source??null;}get quotation(){return this.snapshot?.calculated??null;}
 get usedTime(){return this.snapshot?.usedTime??null;}get servicePrice(){return this.snapshot?.servicePrice??null;}get otherExpenses(){return this.snapshot?.otherExpenses??null;}
 get today(){return this.snapshot?.analysisDate.slice(0,10).split('-').reverse().join('/')??'';}
 ngOnInit(){if(!/^[A-Z0-9]{5}$/.test(this.np)){this.error='Ingresa al historial desde la consulta de una NP válida.';return;}this.load(1);}
 load(page:number){if(this.busy)return;this.busy=true;this.error='';this.http.get<HistoryPage>(this.url,{params:{np:this.np,page},withCredentials:true}).pipe(takeUntilDestroyed(this.destroy)).subscribe({next:result=>{this.busy=false;this.rows=result.rows;this.page=result.page;this.totalPages=result.totalPages;},error:error=>{this.busy=false;this.error=apiError(error);}});}
 view(row:HistoryRow){if(this.busy)return;this.busy=true;this.error='';this.snapshot=null;this.viewedHistory=null;this.http.get<HistoryPage>(this.url,{params:{np:this.np,id:row.id},withCredentials:true}).pipe(takeUntilDestroyed(this.destroy)).subscribe({next:result=>{this.busy=false;try{const saved=result.rows[0];if(!saved?.snapshotJson)throw new Error();const snapshot=JSON.parse(saved.snapshotJson) as SavedSnapshot;if(snapshot.source?.np!==this.np||!snapshot.calculated)throw new Error();this.snapshot=snapshot;this.viewedHistory=saved;}catch{this.error='No se pudo leer la información del análisis guardado.';}},error:error=>{this.busy=false;this.error=apiError(error);}});}
 actionLabel(action:string){return action==='D'?'Eliminado':action==='U'?'Corregido':'Guardado';}
}
