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
import { Router, RouterLink } from '@angular/router';
import { Feedback } from '../../core/feedback/feedback';
import { Viewport } from '../../core/layout/viewport';
import { EmptyState } from '../../shared/empty-state/empty-state';
import { EnglishLevelLabel } from '../../shared/labels/english-level-label';
import { LessonPlanCards } from '../lesson-plans/components/lesson-plan-cards';
import { StatusChange } from '../lesson-plans/components/lesson-plan-events';
import { LessonPlanTable } from '../lesson-plans/components/lesson-plan-table';
import { LessonPlanActions } from '../lesson-plans/lesson-plan-actions';
import { LessonPlanResponse } from '../lesson-plans/lesson-plan.model';
import { LessonPlansStore } from '../lesson-plans/lesson-plans-store';
import { StudentActions } from './student-actions';
import { StudentsStore } from './students-store';

@Component({
  selector: 'lo-student-details-page',
  imports: [
    RouterLink,
    MatButtonModule,
    MatIconModule,
    EmptyState,
    EnglishLevelLabel,
    LessonPlanCards,
    LessonPlanTable,
  ],
  templateUrl: './student-details-page.html',
  styleUrl: '../../shared/layout/page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentDetailsPage {
  private readonly router = inject(Router);
  private readonly feedback = inject(Feedback);
  private readonly studentActions = inject(StudentActions);
  private readonly planActions = inject(LessonPlanActions);
  private readonly studentsStore = inject(StudentsStore);
  protected readonly plansStore = inject(LessonPlansStore);
  protected readonly viewport = inject(Viewport);

  readonly id = input.required<string>();

  protected readonly loaded = signal(false);
  protected readonly student = computed(() => {
    const student = this.studentsStore.selected();
    return student?.id === this.id() ? student : null;
  });
  protected readonly plans = computed(() =>
    this.plansStore.plans().filter((plan) => plan.studentId === this.id()),
  );

  constructor() {
    effect(() => {
      const id = this.id();
      untracked(() => void this.load(id));
    });
  }

  protected newLessonPlan(): void {
    void this.router.navigate(['/lesson-plans/new'], { queryParams: { studentId: this.id() } });
  }

  protected changeStatus(change: StatusChange): Promise<void> {
    return this.planActions.changeStatus(change);
  }

  protected removePlan(plan: LessonPlanResponse): Promise<boolean> {
    return this.planActions.remove(plan);
  }

  protected async removeStudent(): Promise<void> {
    const student = this.student();
    if (student !== null && (await this.studentActions.remove(student))) {
      await this.router.navigate(['/students']);
    }
  }

  private async load(id: string): Promise<void> {
    this.loaded.set(false);
    const [student] = await Promise.all([
      this.studentsStore.loadOne(id),
      this.plansStore.load({ studentId: id, status: null }),
    ]);
    if (!student.ok) {
      this.feedback.failure(student.problem);
      if (student.problem.status === 404) {
        await this.router.navigate(['/students']);
      }
      return;
    }
    this.loaded.set(true);
  }
}
