import { Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [MatCardModule],
  template: `
    <div class="page-container">
      <h2 class="page-title">Usuarios</h2>
      <mat-card>
        <mat-card-content><p>Administración de usuarios y roles.</p></mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`.page-container { padding: 24px; } .page-title { margin-bottom: 16px; }`]
})
export class UsersComponent {}
