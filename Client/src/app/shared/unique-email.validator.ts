import { AbstractControl, AsyncValidatorFn, ValidationErrors } from '@angular/forms';
import { Observable, catchError, map, of, switchMap, timer } from 'rxjs';
import { AuthService } from '../core/auth/auth.service';

const EMAIL_CHECK_DEBOUNCE_MS = 300;

export function uniqueEmail(
  authService: AuthService,
  ignoreEmail?: () => string | null | undefined,
): AsyncValidatorFn {
  return (control: AbstractControl): Observable<ValidationErrors | null> => {
    const value = String(control.value ?? '').trim().toLowerCase();
    if (!value) {
      return of(null);
    }

    const ignored = ignoreEmail?.()?.trim().toLowerCase();
    if (ignored && value === ignored) {
      return of(null);
    }

    return timer(EMAIL_CHECK_DEBOUNCE_MS).pipe(
      switchMap(() => authService.isEmailAvailable(value)),
      map((available) => (available ? null : { emailTaken: true })),
      catchError(() => of(null)),
    );
  };
}
