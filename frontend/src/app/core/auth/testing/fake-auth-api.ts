import { Observable, Subject } from 'rxjs';
import { FakeResponder } from '../../../testing/fake-responder';
import { PublicApi } from '../../../testing/public-api';
import { AuthApi } from '../auth-api';
import { User } from '../user.model';

export type AuthCall = 'csrf' | 'register' | 'login' | 'logout' | 'me';

export class FakeAuthApi implements PublicApi<AuthApi> {
  readonly responder = new FakeResponder<AuthCall>();
  readonly calls = this.responder.calls;
  user: User = { id: 'u1', name: 'Maria', email: 'maria@example.com' };
  sessionUser: User | null = this.user;

  fail(call: AuthCall, status: number, detail: string | null = null): void {
    this.responder.fail(call, status, { detail });
  }

  hold(call: AuthCall): Subject<void> {
    return this.responder.hold(call);
  }

  csrf(): Observable<void> {
    return this.responder.respond('csrf', () => undefined);
  }

  register(): Observable<User> {
    return this.responder.respond('register', () => this.user);
  }

  login(): Observable<User> {
    return this.responder.respond('login', () => this.user);
  }

  logout(): Observable<void> {
    return this.responder.respond('logout', () => undefined);
  }

  me(): Observable<User | null> {
    return this.responder.respond('me', () => this.sessionUser);
  }
}
