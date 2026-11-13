import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthService } from '../auth/auth.service';
import { authGuard } from './auth.guard';
import { roleGuard } from './role.guard';

describe('guards', () => {
  let router: { navigate: ReturnType<typeof vi.fn> };
  let auth: { isAuthenticated: ReturnType<typeof vi.fn>; hasRole: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    router = { navigate: vi.fn().mockResolvedValue(true) };
    auth = {
      isAuthenticated: vi.fn(),
      hasRole: vi.fn(),
    };
    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router },
      ],
    });
  });

  describe('authGuard', () => {
    it('allows navigation for authenticated users', () => {
      auth.isAuthenticated.mockReturnValue(true);

      const result = TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));

      expect(result).toBe(true);
      expect(router.navigate).not.toHaveBeenCalled();
    });

    it('redirects anonymous users to login', () => {
      auth.isAuthenticated.mockReturnValue(false);

      const result = TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));

      expect(result).toBe(false);
      expect(router.navigate).toHaveBeenCalledWith(['/login']);
    });
  });

  describe('roleGuard', () => {
    it('redirects anonymous users to login', () => {
      auth.isAuthenticated.mockReturnValue(false);

      const result = TestBed.runInInjectionContext(() =>
        roleGuard(['Admin'])({} as never, [], {} as never),
      );

      expect(result).toBe(false);
      expect(router.navigate).toHaveBeenCalledWith(['/login']);
    });

    it('allows users holding one of the allowed roles', () => {
      auth.isAuthenticated.mockReturnValue(true);
      auth.hasRole.mockReturnValue(true);

      const result = TestBed.runInInjectionContext(() =>
        roleGuard(['Admin', 'ProjectManager'])({} as never, [], {} as never),
      );

      expect(result).toBe(true);
      expect(auth.hasRole).toHaveBeenCalledWith('Admin', 'ProjectManager');
    });

    it('redirects users without the required role to the unauthorized page', () => {
      auth.isAuthenticated.mockReturnValue(true);
      auth.hasRole.mockReturnValue(false);

      const result = TestBed.runInInjectionContext(() =>
        roleGuard(['Admin'])({} as never, [], {} as never),
      );

      expect(result).toBe(false);
      expect(router.navigate).toHaveBeenCalledWith(['/unauthorized']);
    });
  });
});
