import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ApiService } from '../../../core/services/api.service';

export interface ConnectDbDialogData {
  pipelineId:   number;
  pipelineName: string;
}

interface ColumnInfo    { name: string; type: string }
interface TestResult    { success: boolean; message: string; tables: string[] }
interface PreviewResult { columns: ColumnInfo[]; rows: Record<string, unknown>[] }
interface EtlResult     { executionId: number; status: string; totalRows: number; successRows: number; errorRows: number }

interface ColumnMapping  { source: string; destination: string; include: boolean }
interface FilterCondition {
  column:    string;
  operator:  string;
  valueType: 'literal' | 'column';  // literal = texto libre | column = columna destino
  value:     string;
  destColumn: string;
  connector: 'AND' | 'OR';
}

const OPERATORS = ['=', '!=', '>', '<', '>=', '<=', 'LIKE', 'IS NULL', 'IS NOT NULL'] as const;

@Component({
  selector: 'app-connect-db-dialog',
  standalone: true,
  imports: [FormsModule, MatProgressBarModule, MatIconModule, MatTooltipModule],
  templateUrl: './connect-db-dialog.component.html',
  styleUrl:    './connect-db-dialog.component.scss'
})
export class ConnectDbDialogComponent {
  private api       = inject(ApiService);
  private dialogRef = inject(MatDialogRef<ConnectDbDialogComponent>);
  readonly data     = inject<ConnectDbDialogData>(MAT_DIALOG_DATA);

  // Pasos: 1=Origen, 2=Filtros, 3=Destino, 4=Mapeo, 5=Ejecutar
  step = signal<1 | 2 | 3 | 4 | 5>(1);

  readonly operators = OPERATORS;

  // ── Paso 1: conexión origen ──────────────────────────────────────────────────
  conn = { type: 'mysql', host: 'localhost', port: 3306, database: '', user: '', password: '', winAuth: false };
  testing           = false;
  testResult: TestResult | null = null;
  selectedSrcTable  = '';
  previewing        = false;
  preview: PreviewResult | null = null;
  previewError      = '';

  // ── Paso 2: filtros (opcional) ───────────────────────────────────────────────
  filters: FilterCondition[] = [];
  destColumnInfos: ColumnInfo[] = [];  // columnas destino con tipo, para el dropdown del filtro

  // ── Paso 3: conexión destino ─────────────────────────────────────────────────
  // SQL Server es el tipo por defecto → winAuth activado por defecto
  destConn = { type: 'sqlserver', host: '', port: 1433, database: '', user: '', password: '', winAuth: true };
  destIsInternal    = false;
  destTesting       = false;
  destTestResult: TestResult | null = null;
  selectedDestTable = '';
  destLoadingCols   = false;
  destColumns: string[] = [];
  destColsError     = '';

  // ── Paso 4: mapeo columnas ───────────────────────────────────────────────────
  mappings: ColumnMapping[] = [];

  // ── Paso 5: ejecutar ─────────────────────────────────────────────────────────
  running = false;
  result: EtlResult | null = null;
  error   = '';

  // ── Paso 1 ───────────────────────────────────────────────────────────────────

  onTypeChange(): void {
    this.conn.port        = this.conn.type === 'mysql' ? 3306 : 1433;
    // SQL Server activa WinAuth por defecto; MySQL no tiene WinAuth
    this.conn.winAuth     = this.conn.type === 'sqlserver';
    this.testResult       = null;
    this.selectedSrcTable = '';
    this.preview          = null;
    this.previewError     = '';
    this.filters          = [];
  }

  testConnection(): void {
    this.testing          = true;
    this.testResult       = null;
    this.selectedSrcTable = '';
    this.preview          = null;
    this.previewError     = '';
    this.filters          = [];

    this.api.post<TestResult>('/connections/test', this.conn).subscribe({
      next: res => { this.testResult = res; this.testing = false; },
      error: ()  => {
        this.testResult = { success: false, message: 'Error de red.', tables: [] };
        this.testing    = false;
      }
    });
  }

  loadPreview(): void {
    if (!this.selectedSrcTable) return;
    this.previewing   = true;
    this.preview      = null;
    this.previewError = '';
    this.filters      = [];

    this.api.post<PreviewResult>('/connections/preview',
      { ...this.conn, table: this.selectedSrcTable }
    ).subscribe({
      next: res => { this.preview = res; this.previewing = false; },
      error: err => {
        this.previewError = err.error?.message ?? 'Error al cargar la vista previa.';
        this.previewing   = false;
      }
    });
  }

  get canGoStep2(): boolean {
    return !!(this.testResult?.success && this.selectedSrcTable && this.preview);
  }

  goStep2(): void { this.step.set(2); }

  // ── Paso 2: filtros ───────────────────────────────────────────────────────────

  addFilter(): void {
    const col     = this.preview?.columns?.[0]?.name ?? '';
    const dstCol  = this.destColumns[0] ?? '';
    this.filters.push({ column: col, operator: '=', valueType: 'literal', value: '', destColumn: dstCol, connector: 'AND' });
  }

  removeFilter(i: number): void { this.filters.splice(i, 1); }

  toggleValueType(f: FilterCondition): void {
    f.valueType = f.valueType === 'literal' ? 'column' : 'literal';
  }

  // Vista previa del WHERE en tiempo real
  get wherePreview(): string {
    if (!this.filters.length) return '';
    const parts = this.filters.map((f, i) => {
      const prefix = i === 0 ? '' : ` ${f.connector} `;
      const col    = f.column || '?';
      if (f.operator === 'IS NULL' || f.operator === 'IS NOT NULL') {
        return `${prefix}${col} ${f.operator}`;
      }
      if (f.valueType === 'column') {
        const dc = f.destColumn || '?';
        // Muestra la comparación como referencia a columna destino
        return `${prefix}${col} ${f.operator} (SELECT ${dc} FROM ${this.selectedDestTable})`;
      }
      const raw = f.value === '' ? '?' : f.value;
      const val = f.value !== '' && !isNaN(Number(f.value)) ? raw : `'${raw}'`;
      return `${prefix}${col} ${f.operator} ${val}`;
    });
    return `WHERE ${parts.join('')}`;
  }

  // Filtros siempre opcionales — avanza directo al paso 3
  goStep3(): void { this.step.set(3); }

  // ── Paso 3: destino ───────────────────────────────────────────────────────────

  onDestTypeChange(): void {
    this.destConn.port    = this.destConn.type === 'mysql' ? 3306 : 1433;
    this.destConn.winAuth = this.destConn.type === 'sqlserver';
    this.destTestResult   = null;
    this.selectedDestTable = '';
    this.destColumns      = [];
    this.destColsError    = '';
  }

  testDestConnection(): void {
    this.destIsInternal    = false;
    this.destTesting       = true;
    this.destTestResult    = null;
    this.selectedDestTable = '';
    this.destColumns       = [];
    this.destColsError     = '';

    this.api.post<TestResult>('/connections/test', this.destConn).subscribe({
      next: res => { this.destTestResult = res; this.destTesting = false; },
      error: ()  => {
        this.destTestResult = { success: false, message: 'Error de red al conectar.', tables: [] };
        this.destTesting    = false;
      }
    });
  }

  useInternalDb(): void {
    this.destIsInternal    = true;
    this.destTesting       = true;
    this.destTestResult    = null;
    this.selectedDestTable = '';
    this.destColumns       = [];
    this.destColsError     = '';

    this.api.get<string[]>('/connections/internal/tables').subscribe({
      next: tables => {
        this.destTestResult = { success: true, message: 'BD interna conectada correctamente.', tables };
        this.destTesting    = false;
      },
      error: () => {
        this.destTestResult = { success: false, message: 'Error al conectar con la BD interna.', tables: [] };
        this.destTesting    = false;
        this.destIsInternal = false;
      }
    });
  }

  onDestTableChange(): void {
    this.destColumns   = [];
    this.destColsError = '';
    if (!this.selectedDestTable) return;
    this.destLoadingCols = true;

    if (this.destIsInternal) {
      this.api.get<string[]>(`/connections/internal/columns/${this.selectedDestTable}`).subscribe({
        next: cols => { this.destColumns = cols; this.destLoadingCols = false; },
        error: err  => {
          this.destColsError   = err.error?.message ?? 'Error al cargar columnas.';
          this.destLoadingCols = false;
        }
      });
    } else {
      this.api.post<string[]>('/connections/columns',
        { ...this.destConn, table: this.selectedDestTable }
      ).subscribe({
        next: cols => { this.destColumns = cols; this.destLoadingCols = false; },
        error: err  => {
          this.destColsError   = err.error?.message ?? 'Error al cargar columnas.';
          this.destLoadingCols = false;
        }
      });
    }
  }

  // Puede avanzar de Destino (paso 2) a Filtros (paso 3)
  get canGoStep3(): boolean {
    return this.selectedDestTable !== '' && this.destColumns.length > 0;
  }

  goStep4(): void {
    // Auto-match por nombre; deja vacío si no hay coincidencia
    const srcCols = this.preview?.columns?.map(c => c.name) ?? [];
    this.mappings = srcCols.map(src => ({
      source:      src,
      destination: this.destColumns.find(d => d.toLowerCase() === src.toLowerCase()) ?? '',
      include:     true
    }));
    this.step.set(4);
  }

  // ── Paso 4: mapeo ─────────────────────────────────────────────────────────────

  get includedMappings(): ColumnMapping[] {
    return this.mappings.filter(m => m.include && m.destination !== '');
  }

  get allIncluded(): boolean { return this.mappings.length > 0 && this.mappings.every(m => m.include); }

  toggleAll(event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.mappings.forEach(m => m.include = checked);
  }

  get canGoStep5(): boolean { return this.includedMappings.length > 0; }

  goStep5(): void { this.step.set(5); }

  // ── Paso 5: ejecutar ─────────────────────────────────────────────────────────

  get destSummaryType(): string {
    return this.destIsInternal ? 'SQL Server' : this.destConn.type.toUpperCase();
  }

  get destSummaryDb(): string {
    return this.destIsInternal ? 'DataFlowPlatform' : this.destConn.database;
  }

  run(): void {
    if (!this.includedMappings.length) { this.error = 'Incluye al menos una columna mapeada.'; return; }
    this.running = true;
    this.error   = '';
    this.result  = null;

    const body = {
      // Fuente
      srcType:     this.conn.type,
      srcHost:     this.conn.host,
      srcPort:     this.conn.port,
      srcDatabase: this.conn.database,
      srcUser:     this.conn.user,
      srcPassword: this.conn.password,
      srcTable:    this.selectedSrcTable,
      srcWinAuth:  this.conn.winAuth,
      // Filtros (vacío = sin WHERE)
      filters: this.filters.map(f => ({
        column:     f.column,
        operator:   f.operator,
        valueType:  f.valueType,
        value:      f.value,
        destColumn: f.destColumn,
        connector:  f.connector
      })),
      // Destino
      dstIsInternal: this.destIsInternal,
      dstType:     this.destConn.type,
      dstHost:     this.destConn.host,
      dstPort:     this.destConn.port,
      dstDatabase: this.destConn.database,
      dstUser:     this.destConn.user,
      dstPassword: this.destConn.password,
      dstTable:    this.selectedDestTable,
      dstWinAuth:  this.destConn.winAuth,
      // Mapeo columna origen → columna destino
      columnMappings: this.includedMappings.map(m => ({ source: m.source, destination: m.destination }))
    };

    this.api.post<EtlResult>(`/pipelines/${this.data.pipelineId}/run-from-db`, body).subscribe({
      next: res => { this.result = res; this.running = false; },
      error: err => { this.error  = err.error?.message ?? 'Error al ejecutar.'; this.running = false; }
    });
  }

  // ── Navegación ───────────────────────────────────────────────────────────────

  back(): void {
    const s = this.step();
    if (s === 2) this.step.set(1);
    if (s === 3) this.step.set(2);
    if (s === 4) this.step.set(3);
    if (s === 5) { this.result = null; this.error = ''; this.step.set(4); }
  }

  close(): void { this.dialogRef.close(this.result); }
}
