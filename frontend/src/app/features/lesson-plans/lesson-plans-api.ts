import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  LessonPlanFilter,
  LessonPlanRequest,
  LessonPlanResponse,
  LessonPlanStatus,
} from './lesson-plan.model';

const BASE_URL = '/api/lesson-plans';

@Injectable({ providedIn: 'root' })
export class LessonPlansApi {
  private readonly http = inject(HttpClient);

  list(filter: LessonPlanFilter): Observable<LessonPlanResponse[]> {
    return this.http.get<LessonPlanResponse[]>(BASE_URL, { params: paramsOf(filter) });
  }

  get(id: string): Observable<LessonPlanResponse> {
    return this.http.get<LessonPlanResponse>(`${BASE_URL}/${id}`);
  }

  create(request: LessonPlanRequest): Observable<LessonPlanResponse> {
    return this.http.post<LessonPlanResponse>(BASE_URL, request);
  }

  update(id: string, request: LessonPlanRequest): Observable<LessonPlanResponse> {
    return this.http.put<LessonPlanResponse>(`${BASE_URL}/${id}`, request);
  }

  changeStatus(id: string, status: LessonPlanStatus): Observable<LessonPlanResponse> {
    return this.http.patch<LessonPlanResponse>(`${BASE_URL}/${id}/status`, { status });
  }

  remove(id: string): Observable<void> {
    return this.http.delete<void>(`${BASE_URL}/${id}`);
  }
}

function paramsOf(filter: LessonPlanFilter): HttpParams {
  let params = new HttpParams();
  if (filter.studentId !== null) {
    params = params.set('studentId', filter.studentId);
  }
  if (filter.status !== null) {
    params = params.set('status', filter.status);
  }
  return params;
}
