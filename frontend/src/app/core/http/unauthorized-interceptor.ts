import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthStore } from '../auth/auth-store';
import { PageReloader } from '../auth/page-reloader';

const AUTH_URL_PREFIX = '/api/auth/';

export const unauthorizedInterceptor: HttpInterceptorFn = (request, next) => {
  const authStore = inject(AuthStore);
  const reloader = inject(PageReloader);
  const router = inject(Router);
  return next(request).pipe(
    catchError((error: unknown) => {
      if (isLostSession(request, error)) {
        authStore.clear();
        reloader.load(`/login?returnUrl=${encodeURIComponent(router.url)}`);
      }
      return throwError(() => error);
    }),
  );
};

function isLostSession(request: HttpRequest<unknown>, error: unknown): boolean {
  const isUnauthorized = error instanceof HttpErrorResponse && error.status === 401;
  return isUnauthorized && !request.url.startsWith(AUTH_URL_PREFIX);
}
