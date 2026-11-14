import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, catchError, map, throwError } from 'rxjs';
import { API_BASE_URL } from '../api.config';
import { ApiResponse } from '../../shared/models/api-response.model';
import { AuthResult, User, normalizeUser } from '../../shared/models/user.model';
import { TokenService } from './token.service';

export interface AuthCredentials {
  email: string;
  password: string;
}

export interface RegisterDto {
  email: string;
  password: string;
  fullName: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly tokenService = inject(TokenService);

  private readonly apiUrl = `${API_BASE_URL}/auth`;
  private readonly currentUserSubject = new BehaviorSubject<User | null>(this.tokenService.getUser());
  readonly currentUser$ = this.currentUserSubject.asObservable();

  get currentUserValue(): User | null {
    return this.currentUserSubject.value;
  }

  login(credentials: AuthCredentials): Observable<User> {
    return this.http.post<ApiResponse<AuthResult>>(`${this.apiUrl}/login`, credentials).pipe(
      map((res) => this.applySession(res.data)),
    );
  }

  register(dto: RegisterDto): Observable<User | null> {
    return this.http
      .post<ApiResponse<User>>(`${this.apiUrl}/register`, dto)
      .pipe(map((res) => normalizeUser(res.data)));
  }

  refreshToken(): Observable<User> {
    const refreshToken = this.tokenService.getRefreshToken();
    if (!refreshToken) {
      this.logout();
      return throwError(() => new Error('No refresh token available'));
    }
    return this.http
      .post<ApiResponse<AuthResult>>(`${this.apiUrl}/refresh-token`, JSON.stringify(refreshToken))
      .pipe(
        map((res) => this.applySession(res.data)),
        catchError((error) => {
          this.logout();
          return throwError(() => error);
        }),
      );
  }

  logout(): void {
    this.tokenService.removeToken();
    this.currentUserSubject.next(null);
    this.router.navigate(['/login']);
  }

  isAuthenticated(): boolean {
    return !!this.tokenService.getToken() && !this.tokenService.isExpired();
  }

  hasRole(...roles: string[]): boolean {
    const role = this.currentUserValue?.role;
    return !!role && roles.includes(role);
  }
  updateCurrentUser(user: User): void {
    this.tokenService.setUser(user);
    this.currentUserSubject.next(user);
  }



  private applySession(result: AuthResult): User {
    const user = normalizeUser(result.user) as User;
    this.tokenService.setSession(result.accessToken, result.refreshToken, user);
    this.currentUserSubject.next(user);
    return user;
  }
}
