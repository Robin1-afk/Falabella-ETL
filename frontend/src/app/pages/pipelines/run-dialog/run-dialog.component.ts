import { Component, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogConfig } from '@angular/material/dialog';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatIconModule } from '@angular/material/icon';
import { ApiService } from '../../../core/services/api.service';

export interface RunDialogData {
  pipelineId:   number;
  pipelineName: string;
}

interface EtlResult {
  executionId: number;
  status:      string;
  totalRows:   number;
  successRows: number;
  errorRows:   number;
  executedAt:  string;
  finishedAt:  string;
}

@Component({
  selector: 'app-run-dialog',
  standalone: true,
  // Usa solo los módulos necesarios — el layout es HTML puro con estilos propios
  imports: [MatProgressBarModule, MatIconModule],
  templateUrl: './run-dialog.component.html',
  styleUrl:    './run-dialog.component.scss'
})
export class RunDialogComponent {
  private api       = inject(ApiService);
  private dialogRef = inject(MatDialogRef<RunDialogComponent>);
  readonly data     = inject<RunDialogData>(MAT_DIALOG_DATA);

  selectedFile: File | null = null;
  uploading = false;
  result:    EtlResult | null = null;
  error      = '';

  onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile = input.files?.[0] ?? null;
    this.error  = '';
    this.result = null;
  }

  upload(): void {
    if (!this.selectedFile) return;

    const form = new FormData();
    form.append('file', this.selectedFile);

    this.uploading = true;
    this.error     = '';

    this.api.postForm<EtlResult>(`/pipelines/${this.data.pipelineId}/run`, form)
      .subscribe({
        next: res => {
          this.result    = res;
          this.uploading = false;
        },
        error: err => {
          this.error     = err.error?.message ?? 'Error al ejecutar el pipeline.';
          this.uploading = false;
        }
      });
  }

  close(): void {
    this.dialogRef.close(this.result);
  }
}
