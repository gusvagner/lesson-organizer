import { HttpErrorResponse } from '@angular/common/http';
import { parseProblem } from './api-problem';

describe('parseProblem', () => {
  it('validation problem → status 400 and field errors', () => {
    const type = 'https://tools.ietf.org/html/rfc9110#section-15.5.1';
    const error = new HttpErrorResponse({
      status: 400,
      error: { type, errors: { name: ['Name is required.'] } },
    });
    expect(parseProblem(error)).toEqual({
      status: 400,
      type,
      detail: null,
      fieldErrors: { name: ['Name is required.'] },
    });
  });

  it('problem with detail → detail kept', () => {
    const error = new HttpErrorResponse({ status: 409, error: { detail: 'Conflict here.' } });
    expect(parseProblem(error).detail).toBe('Conflict here.');
  });

  it('network failure → status 0', () => {
    expect(parseProblem(new HttpErrorResponse({ status: 0 })).status).toBe(0);
  });

  it('non-http error → status 0 and no fields', () => {
    expect(parseProblem(new Error('x'))).toEqual({
      status: 0,
      type: null,
      detail: null,
      fieldErrors: {},
    });
  });

  it('non-json body → status kept, no fields', () => {
    expect(parseProblem(new HttpErrorResponse({ status: 500, error: 'boom' }))).toEqual({
      status: 500,
      type: null,
      detail: null,
      fieldErrors: {},
    });
  });
});
