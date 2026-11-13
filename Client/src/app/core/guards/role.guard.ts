import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';

export const roleGuard =
  (allowedRoles: string[]): CanMatchFn =>
  () => {
    const authService = inject(AuthService);
    const router = inject(Router);

    if (!authService.isAuthenticated()) {
      router.navigate(['/login']);
      return false;
    }

    if (authService.hasRole(...allowedRoles)) {
      return true;
    }

    router.navigate(['/unauthorized']);
    return false;
  };
