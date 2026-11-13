import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AuthService } from './auth.service';
import { TokenService } from './token.service';
import { API_BASE_URL } from '../api.config';
import { AuthResult } from '../../shared/models/user.model';

const authResult: AuthResult = {
  user: { id: 'u1', email: 'user@example.com', name: 'Jane', role: 0 as never },
  accessToken: 'access-token',
  accessTokenExpiresAt: new Date().toISOString(),
  refreshToken: 'refresh-token',
};

describe('AuthService', () => {
  let service: AuthService;
  let tokenService: TokenService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    service = TestBed.inject(AuthService);
    tokenService = TestBed.inject(TokenService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  it('starts unauthenticated when storage is empty', () => {
    expect(service.currentUserValue).toBeNull();
    expect(service.isAuthenticated()).toBe(false);
    expect(service.hasRole('Admin')).toBe(false);
  });

  it('stores the session and normalizes the role on login', () => {
    let emittedRole = '';
    service.login({ email: 'user@example.com', password: 'Password123' }).subscribe((user) => {
      emittedRole = user.role;
    });

    const req = httpMock.expectOne(`${API_BASE_URL}/auth/login`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'user@example.com', password: 'Password123' });
    req.flush({ success: true, statusCode: 200, data: authResult });

    expect(emittedRole).toBe('Admin');
    expect(service.currentUserValue?.role).toBe('Admin');
    expect(tokenService.getRefreshToken()).toBe('refresh-token');
    expect(service.hasRole('Admin', 'ProjectManager')).toBe(true);
    expect(service.hasRole('TeamMember')).toBe(false);
  });

  it('normalizes the registered user payload', () => {
    let name: string | null | undefined;
    service.register({ email: 'new@example.com', password: 'Password123', fullName: 'New' }).subscribe((user) => {
      name = user?.name;
    });

    const req = httpMock.expectOne(`${API_BASE_URL}/auth/register`);
    expect(req.request.method).toBe('POST');
    req.flush({
      success: true,
      statusCode: 201,
      data: { id: 'u2', email: 'new@example.com', name: 'New', role: 'TeamMember' },
    });

    expect(name).toBe('New');
  });

  it('refreshes the token when a refresh token is stored', () => {
    tokenService.setSession('stale', 'refresh-token', authResult.user as never);

    service.refreshToken().subscribe();

    const req = httpMock.expectOne(`${API_BASE_URL}/auth/refresh-token`);
    expect(req.request.body).toBe(JSON.stringify('refresh-token'));
    req.flush({ success: true, statusCode: 200, data: authResult });

    expect(tokenService.getToken()).toBe('access-token');
  });

  it('logs out and errors when no refresh token exists', () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    let message = '';
    service.refreshToken().subscribe({ error: (error: Error) => (message = error.message) });

    expect(message).toBe('No refresh token available');
    expect(navigate).toHaveBeenCalledWith(['/login']);
  });

  it('clears the session on logout', () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    tokenService.setSession('access', 'refresh', authResult.user as never);

    service.logout();

    expect(tokenService.getToken()).toBeNull();
    expect(service.currentUserValue).toBeNull();
    expect(navigate).toHaveBeenCalledWith(['/login']);
  });
});
