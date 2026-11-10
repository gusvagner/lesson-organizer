import { FormControl } from '@angular/forms';
import { notBlank } from './not-blank';

describe('notBlank', () => {
  it('"   " → required', () =>
    expect(notBlank(new FormControl('   '))).toEqual({ required: true }));
  it('null → required', () => expect(notBlank(new FormControl(null))).toEqual({ required: true }));
  it('"Ana" → null', () => expect(notBlank(new FormControl('Ana'))).toBeNull());
});
