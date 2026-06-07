import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

// Guard funcional: redirige a /login si no hay token en localStorage
export const authGuard: CanActivateFn = () => {
  if (inject(AuthService).getToken()) return true;
  return inject(Router).createUrlTree(['/login']);
};
