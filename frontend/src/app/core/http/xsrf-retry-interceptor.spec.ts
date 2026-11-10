import {
  HttpClient,
  provideHttpClient,
  withInterceptors,
  withXsrfConfiguration,
} from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { xsrfRetryInterceptor } from './xsrf-retry-interceptor';

const XSRF_TYPE = 'urn:lesson-organizer:problem:invalid-xsrf-token';
const badRequest = { status: 400, statusText: 'Bad Request' };

describe('xsrfRetryInterceptor', () => {
  let http: HttpTestingController;
  let client: HttpClient;

  beforeEach(() => {
    document.cookie = 'XSRF-TOKEN=old';
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(
          withXsrfConfiguration({ cookieName: 'XSRF-TOKEN', headerName: 'X-XSRF-TOKEN' }),
          withInterceptors([xsrfRetryInterceptor]),
        ),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    client = TestBed.inject(HttpClient);
  });

  afterEach(() => http.verify());

  it('400 with xsrf type → refreshes csrf and retries once with the new token', () => {
    let body: unknown = null;
    client.post('/api/students', { name: 'Ana' }).subscribe((value) => (body = value));
    http.expectOne('/api/students').flush({ type: XSRF_TYPE }, badRequest);
    document.cookie = 'XSRF-TOKEN=new';
    http.expectOne('/api/auth/csrf').flush(null, { status: 204, statusText: 'No Content' });
    const retry = http.expectOne('/api/students');
    expect(retry.request.headers.get('X-XSRF-TOKEN')).toBe('new');
    retry.flush({ id: 's1' });
    expect(body).toEqual({ id: 's1' });
  });

  it('retry fails again with xsrf type → error reaches caller, no third request', () => {
    let status = 0;
    client
      .post('/api/students', {})
      .subscribe({ error: (error: { status: number }) => (status = error.status) });
    http.expectOne('/api/students').flush({ type: XSRF_TYPE }, badRequest);
    http.expectOne('/api/auth/csrf').flush(null, { status: 204, statusText: 'No Content' });
    http.expectOne('/api/students').flush({ type: XSRF_TYPE }, badRequest);
    expect(status).toBe(400);
  });

  it('400 with field errors → no retry', () => {
    let status = 0;
    client
      .post('/api/students', {})
      .subscribe({ error: (error: { status: number }) => (status = error.status) });
    http.expectOne('/api/students').flush({ errors: { name: ['Name is required.'] } }, badRequest);
    expect(status).toBe(400);
  });
});
