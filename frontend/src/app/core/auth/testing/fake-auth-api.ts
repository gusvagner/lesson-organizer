import { HttpErrorResponse } from '@angular/common/http';
import { Observable, Subject, map, of, take, throwError } from 'rxjs';
import { PublicApi } from '../../../testing/public-api';
import { AuthApi } from '../auth-api';
import { User } from '../user.model';

export type AuthCall = 'csrf' | 'register' | 'login' | 'logout' | 'me';

export class FakeAuthApi implements PublicApi<AuthApi> {
  readonly calls: AuthCall[] = [];
  readonly failures: Partial<Record<AuthCall, HttpErrorResponse>> = {};
  readonly gates: Partial<Record<AuthCall, Subject<void>>> = {};
  user: User = { id: 'u1', name: 'Maria', email: 'maria@example.com' };

  fail(call: AuthCall, status: number, detail: string | null = null): void {
    this.failures[call] = new HttpErrorResponse({ status, error: { detail } });
  }

  hold(call: AuthCall): Subject<void> {
    const gate = new Subject<void>();
    this.gates[call] = gate;
    return gate;
  }

  csrf(): Observable<void> {
    return this.respond('csrf', undefined);
  }

  register(): Observable<User> {
    return this.respond('register', this.user);
  }

  login(): Observable<User> {
    return this.respond('login', this.user);
  }

  logout(): Observable<void> {
    return this.respond('logout', undefined);
  }

  me(): Observable<User> {
    return this.respond('me', this.user);
  }

  private respond<T>(call: AuthCall, value: T): Observable<T> {
    this.calls.push(call);
    const failure = this.failures[call];
    if (failure) {
      return throwError(() => failure);
    }
    const gate = this.gates[call];
    return gate
      ? gate.pipe(
          take(1),
          map(() => value),
        )
      : of(value);
  }
}
