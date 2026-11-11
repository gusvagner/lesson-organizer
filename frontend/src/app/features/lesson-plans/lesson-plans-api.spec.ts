import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { LessonPlansApi } from './lesson-plans-api';

describe('LessonPlansApi', () => {
  let api: LessonPlansApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(LessonPlansApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('list with no filters → no params', () => {
    api.list({ studentId: null, status: null }).subscribe();
    expect(http.expectOne('/api/lesson-plans').request.params.keys()).toEqual([]);
  });

  it('list with student and status → studentId=s1&status=Ready', () => {
    api.list({ studentId: 's1', status: 'Ready' }).subscribe();
    const request = http.expectOne((r) => r.url === '/api/lesson-plans').request;
    expect(request.urlWithParams).toBe('/api/lesson-plans?studentId=s1&status=Ready');
  });

  it('changeStatus → PATCH /api/lesson-plans/p1/status with { status: "Ready" }', () => {
    api.changeStatus('p1', 'Ready').subscribe();
    const request = http.expectOne('/api/lesson-plans/p1/status').request;
    expect([request.method, request.body]).toEqual(['PATCH', { status: 'Ready' }]);
  });

  it('get, create, update, remove → their endpoints', () => {
    const body = { studentId: 's1', title: 'Review', description: null, lessonDate: '2026-11-12' };
    api.get('p1').subscribe();
    api.create(body).subscribe();
    api.update('p1', body).subscribe();
    api.remove('p1').subscribe();
    expect(http.expectOne({ method: 'GET', url: '/api/lesson-plans/p1' })).toBeTruthy();
    expect(http.expectOne({ method: 'POST', url: '/api/lesson-plans' }).request.body).toEqual(body);
    expect(http.expectOne({ method: 'PUT', url: '/api/lesson-plans/p1' }).request.body).toEqual(
      body,
    );
    expect(http.expectOne({ method: 'DELETE', url: '/api/lesson-plans/p1' })).toBeTruthy();
  });
});
