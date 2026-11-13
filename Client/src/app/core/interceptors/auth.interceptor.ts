import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from '../auth/auth.service';
import { TokenService } from '../auth/token.service';

let isRefreshing = false;

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const tokenService = inject(TokenService);
  const authService = inject(AuthService);
  const token = tokenService.getToken();

  const request = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(request).pipe(
    catchError((error: HttpErrorResponse) => {
      if (
        error.status === 401 &&
        !isRefreshing &&
        !req.url.endsWith('/refresh')
      ) {
        isRefreshing = true;
        return authService.refreshToken().pipe(
          switchMap(() => {
            isRefreshing = false;
            const newToken = tokenService.getToken();
            return next(
              req.clone({ setHeaders: { Authorization: `Bearer ${newToken}` } })
            );
          }),
          catchError(() => {
            isRefreshing = false;
            authService.logout();
            return throwError(() => error);
          })
        );
      }
      return throwError(() => error);
    })
  );
};
