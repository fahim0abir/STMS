import { AbstractControl, ValidationErrors } from '@angular/forms';
import { Observable, firstValueFrom, of, throwError } from 'rxjs';
import { AuthService } from '../core/auth/auth.service';
import { uniqueEmail } from './unique-email.validator';

function createAuthService(isAvailable: (email: string) => boolean) {
  const calls: string[] = [];
  const service = {
    isEmailAvailable: (email: string) => {
      calls.push(email);
      return of(isAvailable(email));
    },
  } as unknown as AuthService;
  return { service, calls };
}

function runValidator(
  service: AuthService,
  email: string,
  ignoreEmail?: () => string | null | undefined,
): Promise<ValidationErrors | null> {
  const control = { value: email } as AbstractControl;
  return firstValueFrom(uniqueEmail(service, ignoreEmail)(control) as Observable<ValidationErrors | null>);
}

describe('uniqueEmail', () => {
  it('flags an email that already exists', async () => {
    const { service } = createAuthService(() => false);

    const errors = await runValidator(service, 'Taken@Example.com');

    expect(errors).toEqual({ emailTaken: true });
  });

  it('accepts an available email and normalizes the lookup value', async () => {
    const { service, calls } = createAuthService(() => true);

    const errors = await runValidator(service, '  Free@Example.com ');

    expect(calls).toEqual(['free@example.com']);
    expect(errors).toBeNull();
  });

  it('skips the check for the ignored (current) email', async () => {
    const { service, calls } = createAuthService(() => false);

    const errors = await runValidator(service, 'me@example.com', () => 'Me@Example.com');

    expect(calls).toEqual([]);
    expect(errors).toBeNull();
  });

  it('fails open when the availability endpoint errors', async () => {
    const service = {
      isEmailAvailable: () => throwError(() => new Error('offline')),
    } as unknown as AuthService;

    const errors = await runValidator(service, 'any@example.com');

    expect(errors).toBeNull();
  });
});
