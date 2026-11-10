import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthApi } from './auth-api';

describe('AuthApi', () => {
  let api: AuthApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(AuthApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('csrf → GET /api/auth/csrf', () => {
    api.csrf().subscribe();
    expect(http.expectOne('/api/auth/csrf').request.method).toBe('GET');
  });

  it('login → POST /api/auth/login with email and password', () => {
    api.login({ email: 'maria@example.com', password: 'Demo1234!' }).subscribe();
    const request = http.expectOne('/api/auth/login').request;
    expect([request.method, request.body]).toEqual([
      'POST',
      { email: 'maria@example.com', password: 'Demo1234!' },
    ]);
  });

  it('register, logout, me → their endpoints', () => {
    api.register({ name: 'Maria', email: 'maria@example.com', password: 'Demo1234!' }).subscribe();
    api.logout().subscribe();
    api.me().subscribe();
    expect(http.expectOne('/api/auth/register').request.method).toBe('POST');
    expect(http.expectOne('/api/auth/logout').request.method).toBe('POST');
    expect(http.expectOne('/api/auth/me').request.method).toBe('GET');
  });
});
