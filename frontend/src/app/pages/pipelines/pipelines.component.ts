import { Component, inject, OnInit, ViewChild } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatTableModule, MatTableDataSource } from '@angular/material/table';
import { MatSortModule, MatSort } from '@angular/material/sort';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ApiService } from '../../core/services/api.service';
import { ConnectDbDialogComponent } from './connect-db-dialog/connect-db-dialog.component';

interface Pipeline {
  id:          number;
  name:        string;
  description: string;
  createdAt:   string;
}

@Component({
  selector: 'app-pipelines',
  standalone: true,
  imports: [
    DatePipe,
    MatTableModule,
    MatSortModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatProgressBarModule,
    MatTooltipModule,
    MatDialogModule,
    MatSnackBarModule
  ],
  templateUrl: './pipelines.component.html',
  styleUrl: './pipelines.component.scss'
})
export class PipelinesComponent implements OnInit {
  private api      = inject(ApiService);
  private dialog   = inject(MatDialog);
  private snackBar = inject(MatSnackBar);

  loading = true;
  error   = '';

  displayedColumns = ['id', 'name', 'description', 'createdAt', 'actions'];
  dataSource = new MatTableDataSource<Pipeline>();

  @ViewChild(MatSort) sort!: MatSort;

  ngOnInit(): void {
    this.loadPipelines();
  }

  loadPipelines(): void {
    this.loading = true;
    this.api.get<Pipeline[]>('/pipelines').subscribe({
      next: data => {
        this.dataSource.data = data;
        this.loading = false;
        // Asigna sort después de tener datos
        setTimeout(() => (this.dataSource.sort = this.sort));
      },
      error: () => {
        this.error   = 'Error al cargar los pipelines.';
        this.loading = false;
      }
    });
  }

  openConnectDbDialog(pipeline: Pipeline): void {
    const ref = this.dialog.open(ConnectDbDialogComponent, {
      width:      '620px',
      maxWidth:   '95vw',
      maxHeight:  '90vh',
      panelClass: 'zero-padding-dialog',
      data: { pipelineId: pipeline.id, pipelineName: pipeline.name }
    });

    ref.afterClosed().subscribe(result => {
      if (result) {
        this.snackBar.open(
          `BD importada: ${result.successRows} filas cargadas`,
          'Cerrar',
          { duration: 5000, panelClass: 'snack-success' }
        );
      }
    });
  }

}
