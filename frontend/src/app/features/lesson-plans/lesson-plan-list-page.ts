import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Feedback } from '../../core/feedback/feedback';
import { Viewport } from '../../core/layout/viewport';
import { EmptyState } from '../../shared/empty-state/empty-state';
import { StudentsStore } from '../students/students-store';
import { LessonPlanCards } from './components/lesson-plan-cards';
import { StatusChange } from './components/lesson-plan-events';
import { LessonPlanFilters } from './components/lesson-plan-filters';
import { LessonPlanTable } from './components/lesson-plan-table';
import { LessonPlanActions } from './lesson-plan-actions';
import { LessonPlanFilter, LessonPlanResponse } from './lesson-plan.model';
import { filterFromQuery, isFiltered } from './lesson-plan-query';
import { LessonPlansStore } from './lesson-plans-store';

@Component({
  selector: 'lo-lesson-plan-list-page',
  imports: [
    RouterLink,
    MatButtonModule,
    MatIconModule,
    EmptyState,
    LessonPlanCards,
    LessonPlanFilters,
    LessonPlanTable,
  ],
  templateUrl: './lesson-plan-list-page.html',
  styleUrl: '../../shared/layout/page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LessonPlanListPage {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly feedback = inject(Feedback);
  private readonly actions = inject(LessonPlanActions);
  protected readonly store = inject(LessonPlansStore);
  protected readonly studentsStore = inject(StudentsStore);
  protected readonly viewport = inject(Viewport);

  readonly studentId = input<string | undefined>();
  readonly status = input<string | undefined>();

  protected readonly filter = computed(() => filterFromQuery(this.studentId(), this.status()));
  protected readonly filtered = computed(() => isFiltered(this.filter()));
  protected readonly loaded = signal(false);
  protected readonly hasStudents = computed(() => this.studentsStore.students().length > 0);

  constructor() {
    effect(() => {
      const filter = this.filter();
      untracked(() => void this.load(filter));
    });
  }

  protected applyFilter(filter: LessonPlanFilter): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { studentId: filter.studentId, status: filter.status },
      replaceUrl: true,
    });
  }

  protected clearFilters(): void {
    this.applyFilter({ studentId: null, status: null });
  }

  protected goTo(path: string): void {
    void this.router.navigateByUrl(path);
  }

  protected changeStatus(change: StatusChange): Promise<void> {
    return this.actions.changeStatus(change);
  }

  protected remove(plan: LessonPlanResponse): Promise<boolean> {
    return this.actions.remove(plan);
  }

  private async load(filter: LessonPlanFilter): Promise<void> {
    const [plans, students] = await Promise.all([
      this.store.load(filter),
      this.studentsStore.load(''),
    ]);
    const failed = !plans.ok ? plans : !students.ok ? students : null;
    if (failed !== null) {
      this.feedback.failure(failed.problem);
    }
    this.loaded.set(true);
  }
}
