import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { toOutcome } from './outcome';

describe('toOutcome', () => {
  it('value → ok with value', async () => {
    expect(await toOutcome(of(5))).toEqual({ ok: true, value: 5 });
  });

  it('error → not ok with parsed problem', async () => {
    const outcome = await toOutcome(
      throwError(() => new HttpErrorResponse({ status: 404, error: { detail: 'Gone.' } })),
    );
    expect(outcome).toEqual({
      ok: false,
      problem: { status: 404, type: null, detail: 'Gone.', fieldErrors: {} },
    });
  });

  it('completes without value → ok with undefined', async () => {
    expect(await toOutcome(of<void>())).toEqual({ ok: true, value: undefined });
  });
});
