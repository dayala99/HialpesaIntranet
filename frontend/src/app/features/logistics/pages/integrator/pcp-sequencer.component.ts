import {Component,DestroyRef,inject} from '@angular/core';
import {CommonModule} from '@angular/common';
import {ActivatedRoute,Router} from '@angular/router';
import {HttpClient} from '@angular/common/http';
import {firstValueFrom} from 'rxjs';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {environment} from '../../../../../environments/environment';
import {apiError} from '../../../../core/http/api-error';
import {PendingComponent} from './pending.component';
import {PcpInboxComponent} from './pcp-inbox.component';
import {PcpEntry} from './pcp.models';
@Component({selector:'app-pcp-sequencer',standalone:true,imports:[CommonModule,PendingComponent,PcpInboxComponent],template:`
<div class="page-title"><div><div class="eyebrow">PCP</div><h1>Secuenciador</h1><p>Selecciona una NP del programa de Planeamiento, define sus procesos y envíala a Logística.</p></div></div>
<p *ngIf="sent" class="feedback success" role="status">NP {{np}} enviada a Logística como {{sent}}.</p>
<div class="tabs" role="tablist" aria-label="Estado del secuenciador">
<button role="tab" id="pending-tab" aria-controls="pending-panel" [attr.aria-selected]="tab==='pending'" (click)="tab='pending'">Pendientes</button>
<button role="tab" id="sent-tab" aria-controls="sent-panel" [attr.aria-selected]="tab==='sent'" (click)="tab='sent'">Enviado a Logística</button>
</div>
<section id="pending-panel" role="tabpanel" aria-labelledby="pending-tab" [hidden]="tab!=='pending'">
<p *ngIf="loading" role="status">Consultando NP enviadas…</p>
<div *ngIf="error" class="feedback error" role="alert">{{error}} <button type="button" (click)="loadSent()">Reintentar</button></div>
<app-analytics-pending *ngIf="!loading&&!error" [pcpMode]="true" [excludedNps]="excludedNps" (openNp)="open($event)"></app-analytics-pending>
</section>
<section id="sent-panel" role="tabpanel" aria-labelledby="sent-tab" [hidden]="tab!=='sent'"><app-pcp-inbox *ngIf="tab==='sent'" [pcpMode]="true"></app-pcp-inbox></section>
`,styles:[`.tabs{display:flex;flex-wrap:wrap;border-bottom:1px solid var(--line);margin-bottom:20px;gap:8px}.tabs button{border:0;border-bottom:2px solid transparent;border-radius:0;background:transparent;padding:14px 20px;color:var(--muted)}.tabs button[aria-selected="true"]{color:var(--accent);border-bottom-color:var(--accent);font-weight:600}[hidden]{display:none!important}`]})
export class PcpSequencerComponent{
 private route=inject(ActivatedRoute);private router=inject(Router);private http=inject(HttpClient);private destroy=inject(DestroyRef);
 tab:'pending'|'sent'='pending';excludedNps:ReadonlySet<string>=new Set<string>();loading=true;error='';
 get sent(){const value=this.route.snapshot.queryParamMap.get('sent');return value==='completo'||value==='parcial'?value:null;}
 get np(){return this.route.snapshot.queryParamMap.get('np');}
 ngOnInit(){void this.loadSent();}
 async loadSent(){this.loading=true;this.error='';try{const excluded=new Set<string>();
 for(const status of ['E','A']){let cursor:number|null=null;do{const params:Record<string,string>={status};if(cursor!==null)params['cursor']=String(cursor);
 const result: {rows:PcpEntry[];hasMore:boolean;nextCursor:number|null}=await firstValueFrom(this.http.get<{rows:PcpEntry[];hasMore:boolean;nextCursor:number|null}>(environment.apiBaseUrl+'/pcp/secuenciador/envios',{params,withCredentials:true}).pipe(takeUntilDestroyed(this.destroy)));
 for(const row of result.rows)excluded.add(row.np.trim().toUpperCase());
 if(!result.hasMore||result.nextCursor===null)break;if(result.nextCursor===cursor)throw new Error('No se pudo completar la consulta de envíos.');cursor=result.nextCursor;
 }while(true);}
 this.excludedNps=excluded;
 }catch(e){this.error=apiError(e);}finally{this.loading=false;}}
 open(np:string){void this.router.navigate(['/pcp/secuenciador/procesos'],{queryParams:{np}});}
}
