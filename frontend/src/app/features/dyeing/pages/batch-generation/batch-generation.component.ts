import { QualityControlDialogComponent } from '../../components/dialogs/quality-control-dialog/quality-control-dialog.component';
import { QualityControlDialogData, QualityControlDialogResult } from '../../models/quality-control.models';
import { CommonModule } from '@angular/common';
import { Component, DestroyRef, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { SharedModule } from '../../../../shared/shared.module';
import { apiError } from '../../../../core/http/api-error';
import { DyeingApiService } from '../../services/dyeing-api.service';
import { DyeingCatalogOption, DyeingFilter, DyeingRow } from '../../models/dyeing.models';

@Component({
  selector: 'app-batch-generation', standalone: true,
  imports: [CommonModule, FormsModule, SharedModule, QualityControlDialogComponent],
  templateUrl: './batch-generation.component.html', styleUrl: './batch-generation.component.scss'
})
export class BatchGenerationComponent {
  private api = inject(DyeingApiService);
  qualityData:QualityControlDialogData|null=null;
  closeQuality(result:QualityControlDialogResult){this.qualityData=null;if(result.saved){this.qualityMessage='Datos de control de calidad guardados.';this.search(this.page);} }
  qualityMessage = '';
  openQuality() {
    if (!this.selected || this.busy) return;
    const batch=String(this.selected['cod_ordtra']||'');const sequence=Number(this.selected['num_secuencia']);
    if(!batch||!Number.isInteger(sequence)||sequence<1)return;
    this.qualityData={batch,sequence};
  }
  private destroyRef = inject(DestroyRef);
  mode = 'number';
  clientMode = 2;
  filter: DyeingFilter = { mode: 4, client: '', series: '', orderNumber: '', color: '', batch: '', status: '', factory: '', productionOrder: '', fiber: '', from: this.today(), to: this.today(), page: 1 };
  options: DyeingCatalogOption[] = [];
  rows: DyeingRow[] = [];
  columns: string[] = [];
  selected: DyeingRow | null = null;
  totalRows = 0;
  page = 1;
  pageSize = 50;
  busy = false;
  catalogsBusy = false;
  catalogsError = '';
  error = '';
  searched = false;
  private lastFilter: DyeingFilter | null = null;
  readonly labels: Record<string, string> = {
    cod_ordtra: 'Partida', np: 'N/P', num_secuencia: 'Sec.', tela: 'Código tela', nombre: 'Tela',
    comb: 'Combinación', color: 'Color', cod_talla: 'Talla', fec_creacion: 'Fecha',
    kgs_progr: 'Kgs. programados', kgs_asignados: 'Kgs. asignados', kgs_crudo: 'Kgs. crudo',
    kgs_tenidos_1ras: 'Kgs. teñidos de primera', kgs_despachados: 'Kgs. despachados',
    uni_progr: 'Und. programadas', uni_asignados: 'Und. asignadas', uni_crudo: 'Und. crudo',
    uni_tenidas_1ras: 'Und. teñidas de primera', uni_despachados: 'Und. despachadas',
    rollos_a_tenir: 'Rollos a teñir', rollos_tenidos: 'Rollos teñidos',
    rollos_despachados: 'Rollos despachados', status: 'Estado', cliente: 'Cliente', ocompra: 'O/C',
    receta: 'Receta', fec_1er_tenido: 'Primer teñido', fec_ult_tenido: 'Último teñido',
    reproceso: 'Reproceso', cod_maquina_tinto: 'Máquina', cod_grupotex: 'Grupo textil',
    cliente_manuf: 'Cliente manufactura', clase_oc: 'Clase O/C'
  };
  constructor() { this.loadCatalogs(); }
  today() { const d = new Date(); return d.getFullYear() + '-' + String(d.getMonth()+1).padStart(2,'0') + '-' + String(d.getDate()).padStart(2,'0'); }
  loadCatalogs() {
    if (this.catalogsBusy) return;
    this.catalogsBusy = true; this.catalogsError = '';
    this.api.catalogs().pipe(finalize(() => this.catalogsBusy = false), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: options => { this.options = options; const factories = this.catalog('factories'); if (factories.length === 1) this.filter.factory = factories[0].code; },
      error: e => this.catalogsError = apiError(e)
    });
  }
  catalog(kind: string) { return this.options.filter(option => option.kind === kind); }
  get sqlMode() { return ({ client: this.clientMode, number: 4, status: 5, np: 6, fiber: 7 } as Record<string, number>)[this.mode]; }
  get usesDates() { return this.sqlMode === 3 || this.sqlMode === 7; }
  get totalPages() { return Math.max(1, Math.ceil(this.totalRows / this.pageSize)); }
  changed() { this.qualityMessage = ''; this.rows = []; this.columns = []; this.selected = null; this.searched = false; this.error = ''; this.lastFilter = null; this.totalRows = 0; this.page = 1; }
  search(page = 1) {
    if (this.busy || this.catalogsBusy || this.catalogsError) return;
    const f = page === 1 ? { ...this.filter, mode: this.sqlMode, page } : { ...this.lastFilter!, page };
    if (!f || (f.mode <= 3 && !f.client.trim()) || (f.mode === 1 && (!f.series.trim() || !f.orderNumber.trim()))
      || (f.mode === 4 && !f.batch.trim()) || (f.mode === 5 && !f.status.trim())
      || (f.mode === 6 && (!f.factory.trim() || !f.productionOrder.trim())) || (f.mode === 7 && !f.fiber.trim())) {
      this.error = 'Completa los campos del filtro seleccionado.'; return;
    }
    if ((f.mode === 3 || f.mode === 7) && (!f.from || !f.to || f.from > f.to || f.from < '1900-01-01' || f.to >= '2100-01-01')) {
      this.error = 'Revisa el rango de fechas.'; return;
    }
    const previousSelection = this.selected;
    this.busy = true; this.error = ''; this.selected = null; this.rows = []; this.columns = []; this.searched = false;
    this.api.search(f).pipe(finalize(() => this.busy = false), takeUntilDestroyed(this.destroyRef)).subscribe({
      next: result => {
        this.rows = result.rows; this.totalRows = result.totalRows; this.page = result.page; this.pageSize = result.pageSize;
        this.lastFilter = f; this.searched = true;
        this.selected = result.rows.find(row => previousSelection && String(row['cod_ordtra']) === String(previousSelection['cod_ordtra']) && String(row['num_secuencia']) === String(previousSelection['num_secuencia'])) || result.rows[0] || null;
        const hidden = ['cod_comb','cod_color','cod_cliente_tex','ser_ordcomp','cod_ordcomp','sec_ordcomp','flg_status'];
        const available = Object.keys(result.rows[0] || {}).filter(key => !hidden.includes(key));
        const preferred = Object.keys(this.labels).filter(key => available.includes(key));
        this.columns = [...preferred, ...available.filter(key => !preferred.includes(key))];
      }, error: e => { this.error = apiError(e); this.totalRows = 0; }
    });
  }
  label(key: string) { return this.labels[key] || key.replace(/_/g,' '); }
  display(row: DyeingRow, key: string) {
    const value = row[key];
    if (value === null || value === undefined || value === '') return '—';
    if (typeof value === 'number') return value.toLocaleString('es-PE', { maximumFractionDigits: 5 });
    if (/^fec|fecha/.test(key) && typeof value === 'string' && /^\d{4}-\d{2}-\d{2}T/.test(value)) return new Date(value).toLocaleDateString('es-PE');
    return String(value);
  }
}
