import { Injectable, inject, signal } from '@angular/core';
import { Outcome, toOutcome } from '../../core/http/outcome';
import { InFlight, LatestOnly } from '../../core/state/in-flight';
import {
  LessonPlanFilter,
  LessonPlanRequest,
  LessonPlanResponse,
  LessonPlanStatus,
} from './lesson-plan.model';
import { LessonPlansApi } from './lesson-plans-api';

@Injectable({ providedIn: 'root' })
export class LessonPlansStore {
  private readonly api = inject(LessonPlansApi);
  private readonly items = signal<LessonPlanResponse[]>([]);
  private readonly current = signal<LessonPlanResponse | null>(null);
  private readonly loads = new InFlight();
  private readonly writes = new InFlight();
  private readonly latestList = new LatestOnly();

  readonly plans = this.items.asReadonly();
  readonly selected = this.current.asReadonly();
  readonly loading = this.loads.active;
  readonly saving = this.writes.active;

  load(filter: LessonPlanFilter): Promise<Outcome<LessonPlanResponse[]>> {
    const isLatest = this.latestList.next();
    return this.loads.run(async () => {
      const outcome = await toOutcome(this.api.list(filter));
      if (outcome.ok && isLatest()) {
        this.items.set(outcome.value);
      }
      return outcome;
    });
  }

  loadOne(id: string): Promise<Outcome<LessonPlanResponse>> {
    return this.loads.run(async () => {
      const outcome = await toOutcome(this.api.get(id));
      this.current.set(outcome.ok ? outcome.value : null);
      return outcome;
    });
  }

  create(request: LessonPlanRequest): Promise<Outcome<LessonPlanResponse>> {
    return this.writes.run(() => toOutcome(this.api.create(request)));
  }

  update(id: string, request: LessonPlanRequest): Promise<Outcome<LessonPlanResponse>> {
    return this.writes.run(async () => this.keep(await toOutcome(this.api.update(id, request))));
  }

  changeStatus(id: string, status: LessonPlanStatus): Promise<Outcome<LessonPlanResponse>> {
    return this.writes.run(async () =>
      this.keep(await toOutcome(this.api.changeStatus(id, status))),
    );
  }

  remove(id: string): Promise<Outcome<void>> {
    return this.writes.run(async () => {
      const outcome = await toOutcome(this.api.remove(id));
      if (outcome.ok) {
        this.items.update((plans) => plans.filter((plan) => plan.id !== id));
      }
      return outcome;
    });
  }

  private keep(outcome: Outcome<LessonPlanResponse>): Outcome<LessonPlanResponse> {
    if (outcome.ok) {
      const saved = outcome.value;
      this.items.update((plans) => plans.map((plan) => (plan.id === saved.id ? saved : plan)));
      this.current.update((plan) => (plan?.id === saved.id ? saved : plan));
    }
    return outcome;
  }
}
