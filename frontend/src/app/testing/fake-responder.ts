import { HttpErrorResponse } from '@angular/common/http';
import { Observable, Subject, defer, of, switchMap, take, throwError } from 'rxjs';

export class FakeResponder<TCall extends string> {
  readonly calls: TCall[] = [];
  private readonly failures = new Map<TCall, HttpErrorResponse>();
  private readonly gates = new Map<TCall, Subject<void>>();

  fail(call: TCall, status: number, body: object = {}): void {
    this.failures.set(call, new HttpErrorResponse({ status, error: body }));
  }

  hold(call: TCall): Subject<void> {
    const gate = new Subject<void>();
    this.gates.set(call, gate);
    return gate;
  }

  respond<T>(call: TCall, produce: () => T): Observable<T> {
    this.calls.push(call);
    const failure = this.take(this.failures, call);
    const gate = this.take(this.gates, call);
    const result = defer(() => (failure ? throwError(() => failure) : of(produce())));
    return gate
      ? gate.pipe(
          take(1),
          switchMap(() => result),
        )
      : result;
  }

  private take<T>(map: Map<TCall, T>, call: TCall): T | undefined {
    const value = map.get(call);
    map.delete(call);
    return value;
  }
}

export function notFound(): HttpErrorResponse {
  return new HttpErrorResponse({ status: 404, error: { detail: 'Not found.' } });
}
