import { FormControl, Validators } from '@angular/forms';
import { fieldErrorMessage } from './field-error-message';
import { notBlank } from './not-blank';

describe('fieldErrorMessage', () => {
  it('untouched invalid → null', () => {
    expect(fieldErrorMessage(new FormControl('', notBlank), 'Name')).toBeNull();
  });

  it('required → "<Label> is required."', () => {
    const control = new FormControl('   ', notBlank);
    control.markAsTouched();
    expect(fieldErrorMessage(control, 'Name')).toBe('Name is required.');
  });

  it('maxlength → "<Label> must be at most N characters."', () => {
    const control = new FormControl('x'.repeat(151), Validators.maxLength(150));
    control.markAsTouched();
    expect(fieldErrorMessage(control, 'Name')).toBe('Name must be at most 150 characters.');
  });

  it('minlength → "<Label> must be at least N characters."', () => {
    const control = new FormControl('short', Validators.minLength(8));
    control.markAsTouched();
    expect(fieldErrorMessage(control, 'Password')).toBe('Password must be at least 8 characters.');
  });

  it('email → "<Label> is not a valid email address."', () => {
    const control = new FormControl('ana@', Validators.email);
    control.markAsTouched();
    expect(fieldErrorMessage(control, 'Email')).toBe('Email is not a valid email address.');
  });

  it('server → server message', () => {
    const control = new FormControl('a');
    control.setErrors({ server: 'Student not found.' });
    control.markAsTouched();
    expect(fieldErrorMessage(control, 'Student')).toBe('Student not found.');
  });
});
