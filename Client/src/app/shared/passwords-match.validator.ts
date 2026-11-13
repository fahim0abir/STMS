import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export function passwordsMatch(passwordKey: string, confirmKey: string): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const password = group.get(passwordKey);
    const confirm = group.get(confirmKey);
    if (!password || !confirm) return null;
    return password.value === confirm.value ? null : { passwordsMismatch: true };
  };
}
