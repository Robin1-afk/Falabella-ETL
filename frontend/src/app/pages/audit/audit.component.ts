import { Component, inject, OnInit, ViewChild, AfterViewInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatPaginatorModule, MatPaginator } from '@angular/material/paginator';
import { MatCardModule } from '@angular/material/card';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { ApiService } from '../../core/services/api.service';

interface AuditLog {
  id:             number;
  pipelineId:     number;
  pipelineName:   string;
  fileName:       string;
  totalRows:      number;
  successRows:    number;
  errorRows:      number;
  executedBy:     number;
  executedByName: string;
  executedAt:     string;
}

@Component({
  selector: 'app-audit',
  standalone: true,
  imports: [
    DatePipe,
    MatTableModule,
    MatSortModule,
    MatPaginatorModule,
    MatCardModule,
    MatProgressBarModule,
    MatChipsModule,
    MatIconModule
  ],
  templateUrl: './audit.component.html',
  styleUrl: './audit.component.scss'
})
export class AuditComponent implements OnInit, AfterViewInit {
  private api = inject(ApiService);

  loading = true;
  error   = '';

  displayedColumns = [
    'pipelineName', 'fileName',
    'successRows', 'errorRows',
    'executedAt',  'executedByName'
  ];

  dataSource = new MatTableDataSource<AuditLog>();

  @ViewChild(MatSort)      sort!: MatSort;
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  ngAfterViewInit(): void {
    this.dataSource.sort      = this.sort;
    this.dataSource.paginator = this.paginator;
  }

  ngOnInit(): void {
    this.api.get<AuditLog[]>('/audit').subscribe({
      next: data => {
        this.dataSource.data = data;
        this.loading = false;
      },
      error: () => {
        this.error   = 'Error al cargar el historial de auditoría.';
        this.loading = false;
      }
    });
  }
}
