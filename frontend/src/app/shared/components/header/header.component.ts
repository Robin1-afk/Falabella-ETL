import { Component, EventEmitter, inject, Input, Output } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatMenuModule } from '@angular/material/menu';
import { MatDividerModule } from '@angular/material/divider';
import { AuthService } from '../../../core/services/auth.service';
import { UiService } from '../../../core/services/ui.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [MatIconModule, MatTooltipModule, MatMenuModule, MatDividerModule],
  templateUrl: './header.component.html',
  styleUrl:    './header.component.scss'
})
export class HeaderComponent {
  private auth = inject(AuthService);
  readonly ui  = inject(UiService);

  @Input() showMenuBtn = false;
  @Output() menuToggle = new EventEmitter<void>();

  get userName():    string { return this.auth.getUserName(); }
  get userEmail():   string { return this.auth.getUserEmail(); }
  get userInitial(): string { return this.auth.getUserInitial(); }

  toggleTheme(): void { this.ui.toggleTheme(); }
  logout():      void { this.auth.logout(); }
}
