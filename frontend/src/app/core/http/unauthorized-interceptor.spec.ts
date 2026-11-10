import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { AuthStore } from '../auth/auth-store';
import { fakeAuthApi, provideFakeAuth } from '../auth/testing/auth-testing';
import { unauthorizedInterceptor } from './unauthorized-interceptor';

@Component({ template: '' })
class Blank {}

const settle = () => new Promise((resolve) => setTimeout(resolve));

describe('unauthorizedInterceptor', () => {
  let http: HttpTestingController;
  let client: HttpClient;
  let store: AuthStore;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideFakeAuth(),
        provideHttpClient(withInterceptors([unauthorizedInterceptor])),
        provideHttpClientTesting(),
        provideRouter([
          { path: 'students/:id', component: Blank },
          { path: 'login', component: Blank },
        ]),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    client = TestBed.inject(HttpClient);
    store = TestBed.inject(AuthStore);
    await store.login('maria@example.com', 'Demo1234!');
    fakeAuthApi().calls.length = 0;
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/students/5');
  });

  afterEach(() => http.verify());

  it('401 from /api/students → clears user, refreshes csrf, navigates to login with returnUrl', async () => {
    let failed = false;
    client.get('/api/students/5').subscribe({ error: () => (failed = true) });
    http.expectOne('/api/students/5').flush(null, { status: 401, statusText: 'Unauthorized' });
    await settle();
    expect(failed).toBe(true);
    expect(store.currentUser()).toBeNull();
    expect(fakeAuthApi().calls).toEqual(['csrf']);
    expect(TestBed.inject(Router).url).toBe('/login?returnUrl=%2Fstudents%2F5');
  });

  it('401 from /api/auth/login or /api/auth/me → passes through untouched', async () => {
    let failures = 0;
    client.post('/api/auth/login', {}).subscribe({ error: () => failures++ });
    client.get('/api/auth/me').subscribe({ error: () => failures++ });
    http.expectOne('/api/auth/login').flush(null, { status: 401, statusText: 'Unauthorized' });
    http.expectOne('/api/auth/me').flush(null, { status: 401, statusText: 'Unauthorized' });
    await settle();
    expect(failures).toBe(2);
    expect(store.isLoggedIn()).toBe(true);
    expect(TestBed.inject(Router).url).toBe('/students/5');
  });

  it('404 → passes through, user kept', async () => {
    let status = 0;
    client
      .get('/api/students/9')
      .subscribe({ error: (error: { status: number }) => (status = error.status) });
    http.expectOne('/api/students/9').flush(null, { status: 404, statusText: 'Not Found' });
    await settle();
    expect(status).toBe(404);
    expect(store.isLoggedIn()).toBe(true);
  });
});
