import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class UiService {
  private readonly COLLAPSE_KEY = 'sidebar_collapsed';
  private readonly THEME_KEY    = 'theme';

  readonly sidebarCollapsed = signal(false);
  readonly isDark           = signal(true);

  constructor() {
    this.sidebarCollapsed.set(localStorage.getItem(this.COLLAPSE_KEY) === 'true');
    this.isDark.set(localStorage.getItem(this.THEME_KEY) !== 'light');
    this.applyTheme(this.isDark());
    this.applySidebar(this.sidebarCollapsed());
  }

  toggleSidebar(): void {
    this.sidebarCollapsed.update(v => !v);
    this.applySidebar(this.sidebarCollapsed());
    localStorage.setItem(this.COLLAPSE_KEY, String(this.sidebarCollapsed()));
  }

  toggleTheme(): void {
    this.isDark.update(v => !v);
    this.applyTheme(this.isDark());
    localStorage.setItem(this.THEME_KEY, this.isDark() ? 'dark' : 'light');
  }

  private applySidebar(collapsed: boolean): void {
    document.documentElement.style.setProperty(
      '--sidebar-w',
      collapsed ? '64px' : '240px'
    );
  }

  private applyTheme(dark: boolean): void {
    document.body.classList.toggle('light-theme', !dark);
  }
}
