import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';

import { routes } from './app.routes';
import { jwtInterceptor } from './core/interceptors/jwt.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    // Registra el interceptor JWT de forma funcional (Angular 15+)
    provideHttpClient(withInterceptors([jwtInterceptor])),
    // Animaciones de Angular Material cargadas de forma diferida
    provideAnimationsAsync()
  ]
};
