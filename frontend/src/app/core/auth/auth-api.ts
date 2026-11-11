import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { LoginRequest, RegisterRequest, User } from './user.model';

const BASE_URL = '/api/auth';

@Injectable({ providedIn: 'root' })
export class AuthApi {
  private readonly http = inject(HttpClient);

  csrf(): Observable<void> {
    return this.http.get<void>(`${BASE_URL}/csrf`);
  }

  register(request: RegisterRequest): Observable<User> {
    return this.http.post<User>(`${BASE_URL}/register`, request);
  }

  login(request: LoginRequest): Observable<User> {
    return this.http.post<User>(`${BASE_URL}/login`, request);
  }

  logout(): Observable<void> {
    return this.http.post<void>(`${BASE_URL}/logout`, null);
  }

  me(): Observable<User | null> {
    return this.http.get<User | null>(`${BASE_URL}/me`);
  }
}
