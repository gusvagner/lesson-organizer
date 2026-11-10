import { Injectable, inject, signal } from '@angular/core';
import { Outcome, toOutcome } from '../../core/http/outcome';
import { InFlight, LatestOnly } from '../../core/state/in-flight';
import { StudentRequest, StudentResponse } from './student.model';
import { StudentsApi } from './students-api';

@Injectable({ providedIn: 'root' })
export class StudentsStore {
  private readonly api = inject(StudentsApi);
  private readonly items = signal<StudentResponse[]>([]);
  private readonly current = signal<StudentResponse | null>(null);
  private readonly loads = new InFlight();
  private readonly writes = new InFlight();
  private readonly latestList = new LatestOnly();

  readonly students = this.items.asReadonly();
  readonly selected = this.current.asReadonly();
  readonly loading = this.loads.active;
  readonly saving = this.writes.active;

  load(search: string): Promise<Outcome<StudentResponse[]>> {
    const isLatest = this.latestList.next();
    return this.loads.run(async () => {
      const outcome = await toOutcome(this.api.list(search));
      if (outcome.ok && isLatest()) {
        this.items.set(outcome.value);
      }
      return outcome;
    });
  }

  loadOne(id: string): Promise<Outcome<StudentResponse>> {
    return this.loads.run(async () => {
      const outcome = await toOutcome(this.api.get(id));
      this.current.set(outcome.ok ? outcome.value : null);
      return outcome;
    });
  }

  create(request: StudentRequest): Promise<Outcome<StudentResponse>> {
    return this.writes.run(() => toOutcome(this.api.create(request)));
  }

  update(id: string, request: StudentRequest): Promise<Outcome<StudentResponse>> {
    return this.writes.run(async () => {
      const outcome = await toOutcome(this.api.update(id, request));
      if (outcome.ok) {
        this.current.set(outcome.value);
      }
      return outcome;
    });
  }

  remove(id: string): Promise<Outcome<void>> {
    return this.writes.run(async () => {
      const outcome = await toOutcome(this.api.remove(id));
      if (outcome.ok) {
        this.items.update((students) => students.filter((student) => student.id !== id));
      }
      return outcome;
    });
  }
}
