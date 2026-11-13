import { FormControl, FormGroup } from '@angular/forms';
import { describe, expect, it } from 'vitest';
import { passwordsMatch } from './passwords-match.validator';

function buildGroup(password: string, confirm: string): FormGroup {
  return new FormGroup(
    {
      password: new FormControl(password),
      confirmPassword: new FormControl(confirm),
    },
    { validators: passwordsMatch('password', 'confirmPassword') },
  );
}

describe('passwordsMatch', () => {
  it('returns no error when the values match', () => {
    expect(buildGroup('Password123', 'Password123').errors).toBeNull();
  });

  it('returns a mismatch error when values differ', () => {
    expect(buildGroup('Password123', 'Password124').errors).toEqual({ passwordsMismatch: true });
  });

  it('ignores groups that do not contain the configured controls', () => {
    const group = new FormGroup({ other: new FormControl('a') }, { validators: passwordsMatch('password', 'confirmPassword') });

    expect(group.errors).toBeNull();
  });
});
