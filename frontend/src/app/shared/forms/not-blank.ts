import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export const notBlank: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const value: unknown = control.value;
  const isBlank = typeof value !== 'string' || value.trim().length === 0;
  return isBlank ? { required: true } : null;
};
