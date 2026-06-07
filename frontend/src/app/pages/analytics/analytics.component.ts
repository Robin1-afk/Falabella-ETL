import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpParams } from '@angular/common/http';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { NgChartsModule } from 'ng2-charts';
import { Chart, ChartData, ChartOptions, registerables } from 'chart.js';

Chart.register(...registerables);

// URL base del microservicio Python — independiente del proxy Angular
const PYTHON_API = 'http://localhost:8000';

interface Summary  { totalMonto: number; totalTransacciones: number; promedio: number; tasaExito: number }
interface SedeRow  { sede: string;  monto: number; transacciones: number }
interface MonthRow { mes:  string;  monto: number; transacciones: number }
interface CatRow   { categoria: string; monto: number; transacciones: number }

const COLORS = ['#00A651','#60a5fa','#f59e0b','#f87171','#a78bfa','#34d399','#fb923c','#38bdf8'];

@Component({
  selector: 'app-analytics',
  standalone: true,
  imports: [CommonModule, FormsModule, MatIconModule, MatTooltipModule, NgChartsModule],
  templateUrl: './analytics.component.html',
  styleUrl:    './analytics.component.scss'
})
export class AnalyticsComponent implements OnInit {
  // HttpClient directo — las peticiones van a localhost:8000, no al proxy /api
  constructor(private http: HttpClient) {}

  // ── Filtros ──────────────────────────────────────────────────────────────────
  paises:     string[] = [];
  sedes:      string[] = [];
  categorias: string[] = [];

  selectedPais      = '';
  selectedSede      = '';
  selectedCategoria = '';
  fechaInicio       = '2026-01-01';
  fechaFin          = '2026-06-30';

  loading = signal(false);

  // ── KPIs ─────────────────────────────────────────────────────────────────────
  summary: Summary | null = null;

  // ── Barras: por sede ─────────────────────────────────────────────────────────
  barData: ChartData<'bar'> = { labels: [], datasets: [] };
  barOptions: ChartOptions<'bar'> = {
    responsive: true, maintainAspectRatio: false,
    plugins: { legend: { display: false } },
    scales: {
      x: { ticks: { color: '#94a3b8', font: { size: 11 } }, grid: { color: 'rgba(148,163,184,0.1)' } },
      y: { ticks: { color: '#94a3b8', font: { size: 11 },
                    callback: v => '$' + Number(v).toLocaleString() },
           grid: { color: 'rgba(148,163,184,0.1)' } }
    }
  };

  // ── Línea: evolución mensual ──────────────────────────────────────────────────
  lineData: ChartData<'line'> = { labels: [], datasets: [] };
  lineOptions: ChartOptions<'line'> = {
    responsive: true, maintainAspectRatio: false,
    plugins: { legend: { labels: { color: '#94a3b8', font: { size: 11 } } } },
    scales: {
      x: { ticks: { color: '#94a3b8', font: { size: 11 } }, grid: { color: 'rgba(148,163,184,0.1)' } },
      y: { ticks: { color: '#94a3b8', font: { size: 11 } }, grid: { color: 'rgba(148,163,184,0.1)' } }
    }
  };

  // ── Dona: por categoría ───────────────────────────────────────────────────────
  donutData: ChartData<'doughnut'> = { labels: [], datasets: [] };
  donutOptions: ChartOptions<'doughnut'> = {
    responsive: true, maintainAspectRatio: false,
    plugins: {
      legend: { position: 'bottom', labels: { color: '#94a3b8', font: { size: 12 }, padding: 16 } }
    },
    cutout: '62%'
  };

  ngOnInit(): void {
    this.loadFilters();
    this.applyFilters();
  }

  loadFilters(): void {
    this.get<{ paises: string[]; sedes: string[]; categorias: string[] }>('/analytics/filters')
      .subscribe(f => {
        this.paises     = f.paises;
        this.categorias = f.categorias;
        this.sedes      = f.sedes;
      });
  }

  onPaisChange(): void {
    this.selectedSede = '';
    const params: Record<string, string> = {};
    if (this.selectedPais) params['pais'] = this.selectedPais;
    this.get<string[]>('/analytics/filters/sedes', params)
      .subscribe(s => this.sedes = s);
  }

  applyFilters(): void {
    this.loading.set(true);
    this._doneCount = 0;
    const params = this.buildParams();

    this.get<Summary>('/analytics/summary', params).subscribe(s => {
      this.summary = s;
      this.checkDone();
    });

    this.get<SedeRow[]>('/analytics/by-sede', params).subscribe(rows => {
      this.barData = {
        labels:   rows.map(r => r.sede),
        datasets: [{
          data:            rows.map(r => r.monto),
          backgroundColor: rows.map((_, i) => COLORS[i % COLORS.length]),
          borderRadius:    6,
          borderWidth:     0
        }]
      };
      this.checkDone();
    });

    this.get<MonthRow[]>('/analytics/by-month', params).subscribe(rows => {
      this.lineData = {
        labels:   rows.map(r => r.mes),
        datasets: [
          {
            label: 'Monto', data: rows.map(r => r.monto),
            borderColor: '#00A651', backgroundColor: 'rgba(0,166,81,0.1)',
            fill: true, tension: 0.4, pointRadius: 4, pointBackgroundColor: '#00A651'
          },
          {
            label: 'Transacciones', data: rows.map(r => r.transacciones),
            borderColor: '#60a5fa', backgroundColor: 'rgba(96,165,250,0.08)',
            fill: false, tension: 0.4, pointRadius: 4, pointBackgroundColor: '#60a5fa',
            yAxisID: 'y'
          }
        ]
      };
      this.checkDone();
    });

    this.get<CatRow[]>('/analytics/by-category', params).subscribe(rows => {
      this.donutData = {
        labels:   rows.map(r => r.categoria),
        datasets: [{
          data:            rows.map(r => r.monto),
          backgroundColor: COLORS.slice(0, rows.length),
          borderWidth:     0,
          hoverOffset:     10
        }]
      };
      this.checkDone();
    });
  }

  formatMonto(v: number): string {
    if (v >= 1_000_000) return '$' + (v / 1_000_000).toFixed(1) + 'M';
    if (v >= 1_000)     return '$' + (v / 1_000).toFixed(0) + 'K';
    return '$' + v.toFixed(0);
  }

  private _doneCount = 0;

  private checkDone(): void {
    this._doneCount++;
    if (this._doneCount >= 4) { this.loading.set(false); this._doneCount = 0; }
  }

  // GET directo al microservicio Python (bypass del proxy /api)
  private get<T>(path: string, params: Record<string, string> = {}) {
    let p = new HttpParams();
    Object.entries(params).forEach(([k, v]) => (p = p.set(k, v)));
    return this.http.get<T>(`${PYTHON_API}${path}`, { params: p });
  }

  private buildParams(): Record<string, string> {
    const p: Record<string, string> = {};
    if (this.selectedPais)      p['pais']        = this.selectedPais;
    if (this.selectedSede)      p['sede']        = this.selectedSede;
    if (this.selectedCategoria) p['categoria']   = this.selectedCategoria;
    if (this.fechaInicio)       p['fechaInicio'] = this.fechaInicio;
    if (this.fechaFin)          p['fechaFin']    = this.fechaFin;
    return p;
  }
}
