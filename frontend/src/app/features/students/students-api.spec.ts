import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { StudentsApi } from './students-api';

describe('StudentsApi', () => {
  let api: StudentsApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(StudentsApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('list("") → GET /api/students with no params', () => {
    api.list('').subscribe();
    expect(http.expectOne('/api/students').request.params.keys()).toEqual([]);
  });

  it('list("   ") → no search param', () => {
    api.list('   ').subscribe();
    expect(http.expectOne('/api/students').request.params.has('search')).toBe(false);
  });

  it('list("ana %&#") → search sent URL-encoded', () => {
    api.list('ana %&#').subscribe();
    const request = http.expectOne((r) => r.url === '/api/students').request;
    expect(request.params.get('search')).toBe('ana %&#');
    expect(request.urlWithParams).toBe('/api/students?search=ana%20%25%26%23');
  });

  it('get, create, update, remove → their endpoints', () => {
    const body = { name: 'Ana', email: null, englishLevel: 'Beginner' as const, notes: null };
    api.get('s1').subscribe();
    api.create(body).subscribe();
    api.update('s1', body).subscribe();
    api.remove('s1').subscribe();
    expect(http.expectOne({ method: 'GET', url: '/api/students/s1' })).toBeTruthy();
    expect(http.expectOne({ method: 'POST', url: '/api/students' }).request.body).toEqual(body);
    expect(http.expectOne({ method: 'PUT', url: '/api/students/s1' }).request.body).toEqual(body);
    expect(http.expectOne({ method: 'DELETE', url: '/api/students/s1' })).toBeTruthy();
  });
});
