import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { StudentRequest, StudentResponse } from './student.model';

const BASE_URL = '/api/students';

@Injectable({ providedIn: 'root' })
export class StudentsApi {
  private readonly http = inject(HttpClient);

  list(search: string): Observable<StudentResponse[]> {
    const term = search.trim();
    const params = term === '' ? new HttpParams() : new HttpParams().set('search', term);
    return this.http.get<StudentResponse[]>(BASE_URL, { params });
  }

  get(id: string): Observable<StudentResponse> {
    return this.http.get<StudentResponse>(`${BASE_URL}/${id}`);
  }

  create(request: StudentRequest): Observable<StudentResponse> {
    return this.http.post<StudentResponse>(BASE_URL, request);
  }

  update(id: string, request: StudentRequest): Observable<StudentResponse> {
    return this.http.put<StudentResponse>(`${BASE_URL}/${id}`, request);
  }

  remove(id: string): Observable<void> {
    return this.http.delete<void>(`${BASE_URL}/${id}`);
  }
}
