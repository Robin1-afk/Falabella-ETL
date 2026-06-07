import { Component, inject, OnInit } from '@angular/core';
import { forkJoin } from 'rxjs';
import { DatePipe, DecimalPipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { NgChartsModule } from 'ng2-charts';
import {
  Chart, ChartConfiguration, ChartData, ChartType,
  BarController, BarElement, CategoryScale, LinearScale, Legend, Tooltip
} from 'chart.js';
import { ApiService } from '../../core/services/api.service';

Chart.register(BarController, BarElement, CategoryScale, LinearScale, Legend, Tooltip);

interface Pipeline { id: number; name: string; description: string; createdAt: string; }
interface AuditLog  { id: number; pipelineName: string; successRows: number; errorRows: number; executedAt: string; }

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [DatePipe, DecimalPipe, MatCardModule, MatProgressSpinnerModule, MatIconModule, NgChartsModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  private api = inject(ApiService);

  loading = true;
  error   = '';

  totalPipelines = 0;
  totalRecords   = 0;
  lastExecution: string | null = null;

  barChartType: ChartType = 'bar';

  // Colores y grids adaptados al dark theme
  barChartOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: 'top',
        labels: { color: '#94a3b8', font: { family: 'Inter', size: 12 }, padding: 16 }
      }
    },
    scales: {
      x: {
        ticks: { color: '#64748b', font: { family: 'Inter', size: 11 } },
        grid:  { color: 'rgba(51,65,85,0.5)', drawTicks: false }
      },
      y: {
        beginAtZero: true,
        ticks: { color: '#64748b', font: { family: 'Inter', size: 11 }, precision: 0 },
        grid:  { color: 'rgba(51,65,85,0.5)' }
      }
    }
  };

  barChartData: ChartData<'bar'> = { labels: [], datasets: [] };

  ngOnInit(): void {
    forkJoin({
      pipelines: this.api.get<Pipeline[]>('/pipelines'),
      audit:     this.api.get<AuditLog[]>('/audit')
    }).subscribe({
      next: ({ pipelines, audit }) => {
        this.totalPipelines = pipelines.length;
        this.totalRecords   = audit.reduce((sum, l) => sum + l.successRows, 0);
        this.lastExecution  = audit.length ? audit[0].executedAt : null;
        this.buildChart(audit);
        this.loading = false;
      },
      error: () => { this.error = 'Error al cargar los datos.'; this.loading = false; }
    });
  }

  private buildChart(logs: AuditLog[]): void {
    const map = new Map<string, { ok: number; err: number }>();
    for (const log of logs) {
      const e = map.get(log.pipelineName) ?? { ok: 0, err: 0 };
      e.ok  += log.successRows;
      e.err += log.errorRows;
      map.set(log.pipelineName, e);
    }

    this.barChartData = {
      labels: [...map.keys()],
      datasets: [
        {
          label: 'Exitosos',
          data:  [...map.values()].map(v => v.ok),
          backgroundColor: 'rgba(16, 185, 129, 0.75)',
          hoverBackgroundColor: '#10b981',
          borderRadius: 6,
          borderSkipped: false
        },
        {
          label: 'Errores',
          data:  [...map.values()].map(v => v.err),
          backgroundColor: 'rgba(239, 68, 68, 0.75)',
          hoverBackgroundColor: '#ef4444',
          borderRadius: 6,
          borderSkipped: false
        }
      ]
    };
  }
}
