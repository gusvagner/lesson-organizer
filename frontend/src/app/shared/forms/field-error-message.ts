import { AbstractControl } from '@angular/forms';

interface LengthError {
  requiredLength: number;
}

export function fieldErrorMessage(control: AbstractControl, label: string): string | null {
  const errors = control.errors;
  if (errors === null || !control.touched) {
    return null;
  }
  if (typeof errors['server'] === 'string') {
    return errors['server'];
  }
  if (errors['required']) {
    return `${label} is required.`;
  }
  if (errors['email']) {
    return `${label} is not a valid email address.`;
  }
  if (errors['maxlength']) {
    return `${label} must be at most ${(errors['maxlength'] as LengthError).requiredLength} characters.`;
  }
  if (errors['minlength']) {
    return `${label} must be at least ${(errors['minlength'] as LengthError).requiredLength} characters.`;
  }
  return `${label} is not valid.`;
}
