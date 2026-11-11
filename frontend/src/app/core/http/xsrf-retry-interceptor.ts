import {
  HttpContextToken,
  HttpErrorResponse,
  HttpInterceptorFn,
  HttpRequest,
  HttpXsrfTokenExtractor,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthApi } from '../auth/auth-api';

export const XSRF_PROBLEM_TYPE = 'urn:lesson-organizer:problem:invalid-xsrf-token';
const XSRF_HEADER = 'X-XSRF-TOKEN';
const XSRF_RETRIED = new HttpContextToken<boolean>(() => false);

export const xsrfRetryInterceptor: HttpInterceptorFn = (request, next) => {
  const authApi = inject(AuthApi);
  const tokens = inject(HttpXsrfTokenExtractor);
  return next(request).pipe(
    catchError((error: unknown) => {
      if (!isStaleXsrfToken(error) || request.context.get(XSRF_RETRIED)) {
        return throwError(() => error);
      }
      return authApi.csrf().pipe(switchMap(() => next(retryOf(request, tokens.getToken()))));
    }),
  );
};

function isStaleXsrfToken(error: unknown): boolean {
  return (
    error instanceof HttpErrorResponse &&
    error.status === 400 &&
    error.error?.type === XSRF_PROBLEM_TYPE
  );
}

function retryOf(request: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
  const context = request.context.set(XSRF_RETRIED, true);
  return token === null
    ? request.clone({ context })
    : request.clone({ context, setHeaders: { [XSRF_HEADER]: token } });
}
