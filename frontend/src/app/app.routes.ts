import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { LayoutComponent } from './shared/layout/layout.component';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },

  // Ruta pública
  {
    path: 'login',
    loadComponent: () =>
      import('./pages/auth/login/login.component').then(m => m.LoginComponent)
  },

  // Rutas protegidas: renderizan dentro del LayoutComponent (sidebar + header)
  {
    path: '',
    component: LayoutComponent,
    canActivate: [authGuard],
    children: [
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./pages/dashboard/dashboard.component').then(m => m.DashboardComponent)
      },
      {
        path: 'pipelines',
        loadComponent: () =>
          import('./pages/pipelines/pipelines.component').then(m => m.PipelinesComponent)
      },
      {
        path: 'users',
        loadComponent: () =>
          import('./pages/users/users.component').then(m => m.UsersComponent)
      },
      {
        path: 'audit',
        loadComponent: () =>
          import('./pages/audit/audit.component').then(m => m.AuditComponent)
      },
      {
        path: 'analytics',
        loadComponent: () =>
          import('./pages/analytics/analytics.component').then(m => m.AnalyticsComponent)
      }
    ]
  },

  { path: '**', redirectTo: 'dashboard' }
];
