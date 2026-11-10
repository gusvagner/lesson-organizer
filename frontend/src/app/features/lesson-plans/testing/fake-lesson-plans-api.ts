import { Observable } from 'rxjs';
import { FakeResponder, notFound } from '../../../testing/fake-responder';
import { PublicApi } from '../../../testing/public-api';
import {
  LessonPlanFilter,
  LessonPlanRequest,
  LessonPlanResponse,
  LessonPlanStatus,
} from '../lesson-plan.model';
import { LessonPlansApi } from '../lesson-plans-api';

export type LessonPlansCall = 'list' | 'get' | 'create' | 'update' | 'changeStatus' | 'remove';

export function aLessonPlan(overrides: Partial<LessonPlanResponse> = {}): LessonPlanResponse {
  return {
    id: 'p1',
    studentId: 's1',
    studentName: 'Ana',
    title: 'Present perfect',
    description: null,
    lessonDate: '2026-11-12',
    status: 'ToPrepare',
    ...overrides,
  };
}

export class FakeLessonPlansApi implements PublicApi<LessonPlansApi> {
  readonly responder = new FakeResponder<LessonPlansCall>();
  readonly filters: LessonPlanFilter[] = [];
  plans: LessonPlanResponse[] = [];
  private nextId = 1;

  list(filter: LessonPlanFilter): Observable<LessonPlanResponse[]> {
    this.filters.push(filter);
    return this.responder.respond('list', () =>
      this.plans.filter(
        (plan) =>
          (filter.studentId === null || plan.studentId === filter.studentId) &&
          (filter.status === null || plan.status === filter.status),
      ),
    );
  }

  get(id: string): Observable<LessonPlanResponse> {
    return this.responder.respond('get', () => this.find(id));
  }

  create(request: LessonPlanRequest): Observable<LessonPlanResponse> {
    return this.responder.respond('create', () => {
      const plan = aLessonPlan({ ...request, id: `new-${this.nextId++}`, status: 'ToPrepare' });
      this.plans = [...this.plans, plan];
      return plan;
    });
  }

  update(id: string, request: LessonPlanRequest): Observable<LessonPlanResponse> {
    return this.responder.respond('update', () => this.replace({ ...this.find(id), ...request }));
  }

  changeStatus(id: string, status: LessonPlanStatus): Observable<LessonPlanResponse> {
    return this.responder.respond('changeStatus', () => this.replace({ ...this.find(id), status }));
  }

  remove(id: string): Observable<void> {
    return this.responder.respond('remove', () => {
      this.find(id);
      this.plans = this.plans.filter((plan) => plan.id !== id);
    });
  }

  private replace(plan: LessonPlanResponse): LessonPlanResponse {
    this.plans = this.plans.map((current) => (current.id === plan.id ? plan : current));
    return plan;
  }

  private find(id: string): LessonPlanResponse {
    const plan = this.plans.find((current) => current.id === id);
    if (plan === undefined) {
      throw notFound();
    }
    return plan;
  }
}
