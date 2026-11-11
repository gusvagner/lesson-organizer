import { Observable, defaultIfEmpty, firstValueFrom } from 'rxjs';
import { ApiProblem, parseProblem } from './api-problem';

export type Outcome<T> = { ok: true; value: T } | { ok: false; problem: ApiProblem };

export async function toOutcome<T>(source: Observable<T>): Promise<Outcome<T>> {
  try {
    const value = await firstValueFrom(source.pipe(defaultIfEmpty(undefined as T)));
    return { ok: true, value };
  } catch (error: unknown) {
    return { ok: false, problem: parseProblem(error) };
  }
}
