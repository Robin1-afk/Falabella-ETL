import { Component, EventEmitter, inject, Output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { UiService } from '../../../core/services/ui.service';

interface NavLink {
  label: string;
  icon:  string;
  path:  string;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, MatIconModule, MatTooltipModule],
  templateUrl: './sidebar.component.html',
  styleUrl:    './sidebar.component.scss'
})
export class SidebarComponent {
  @Output() linkClicked = new EventEmitter<void>();
  readonly ui = inject(UiService);

  links: NavLink[] = [
    { label: 'Dashboard', icon: 'dashboard',         path: '/dashboard' },
    { label: 'Pipelines', icon: 'settings_ethernet', path: '/pipelines' },
    { label: 'Analytics', icon: 'bar_chart',         path: '/analytics' },
    { label: 'Auditoría', icon: 'history',           path: '/audit'     }
  ];
}
