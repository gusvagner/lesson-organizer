import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthApi } from '../auth/auth-api';
import { AuthStore } from '../auth/auth-store';
import { toOutcome } from './outcome';

const AUTH_URL_PREFIX = '/api/auth/';

export const unauthorizedInterceptor: HttpInterceptorFn = (request, next) => {
  const authStore = inject(AuthStore);
  const authApi = inject(AuthApi);
  const router = inject(Router);
  return next(request).pipe(
    catchError((error: unknown) => {
      if (isLostSession(request, error)) {
        authStore.clear();
        void toOutcome(authApi.csrf());
        void router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
      }
      return throwError(() => error);
    }),
  );
};

function isLostSession(request: HttpRequest<unknown>, error: unknown): boolean {
  const isUnauthorized = error instanceof HttpErrorResponse && error.status === 401;
  return isUnauthorized && !request.url.startsWith(AUTH_URL_PREFIX);
}
