import { FormControl, FormGroup } from '@angular/forms';
import { applyServerErrors } from './server-errors';

describe('applyServerErrors', () => {
  it('known field → control error and touched; unknown → returned', () => {
    const form = new FormGroup({ name: new FormControl('') });
    const rest = applyServerErrors(form, {
      status: 400,
      type: null,
      detail: null,
      fieldErrors: { name: ['Name is required.'], '': ['Bad input.'] },
    });
    expect(form.controls.name.errors).toEqual({ server: 'Name is required.' });
    expect(form.controls.name.touched).toBe(true);
    expect(rest).toEqual(['Bad input.']);
  });

  it('no field errors → nothing changed', () => {
    const form = new FormGroup({ name: new FormControl('Ana') });
    expect(
      applyServerErrors(form, { status: 409, type: null, detail: 'x', fieldErrors: {} }),
    ).toEqual([]);
    expect(form.valid).toBe(true);
  });
});
