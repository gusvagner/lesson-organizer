import { FormControl } from '@angular/forms';
import { emailAddress } from './email-address';

describe('emailAddress', () => {
  it('blank or spaces → valid (optional field)', () => {
    expect(emailAddress(new FormControl(''))).toBeNull();
    expect(emailAddress(new FormControl('   '))).toBeNull();
  });

  it('padded valid address → valid', () => {
    expect(emailAddress(new FormControl('  ana@example.com '))).toBeNull();
  });

  it('"ana@" → email error', () => {
    expect(emailAddress(new FormControl('ana@'))).toEqual({ email: true });
  });
});
