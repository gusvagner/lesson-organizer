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
import { ApiProblem } from '../../core/http/api-problem';
import { Outcome } from '../../core/http/outcome';
import { OneSubmission } from '../../core/state/one-submission';
import { EmptyState } from '../../shared/empty-state/empty-state';
import { LessonPlanStatusLabel } from '../../shared/labels/lesson-plan-status-label';
import { StudentsStore } from '../students/students-store';
import { LessonPlanForm } from './components/lesson-plan-form';
import { LessonPlanRequest, LessonPlanResponse } from './lesson-plan.model';
import { LessonPlansStore } from './lesson-plans-store';

@Component({
  selector: 'lo-lesson-plan-form-page',
  imports: [
    RouterLink,
    MatButtonModule,
    MatIconModule,
    EmptyState,
    LessonPlanForm,
    LessonPlanStatusLabel,
  ],
  templateUrl: './lesson-plan-form-page.html',
  styleUrl: '../../shared/layout/page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LessonPlanFormPage {
  private readonly router = inject(Router);
  private readonly feedback = inject(Feedback);
  protected readonly store = inject(LessonPlansStore);
  protected readonly studentsStore = inject(StudentsStore);

  readonly id = input<string | undefined>();
  readonly studentId = input<string | undefined>();

  protected readonly problem = signal<ApiProblem | null>(null);
  protected readonly submission = new OneSubmission();
  protected readonly studentsLoaded = signal(false);
  protected readonly plan = computed(() => {
    const plan = this.store.selected();
    return this.id() !== undefined && plan?.id === this.id() ? plan : null;
  });
  protected readonly preselectedStudentId = computed(() => this.studentId() ?? null);

  constructor() {
    effect(() => {
      const id = this.id();
      untracked(() => void this.load(id));
    });
  }

  protected goToNewStudent(): void {
    void this.router.navigate(['/students/new']);
  }

  protected save(request: LessonPlanRequest): Promise<void> {
    return this.submission.run(async () => {
      this.problem.set(null);
      const id = this.id();
      const outcome =
        id === undefined ? await this.store.create(request) : await this.store.update(id, request);
      return this.handleSaved(outcome);
    });
  }

  private async handleSaved(outcome: Outcome<LessonPlanResponse>): Promise<boolean> {
    if (outcome.ok) {
      this.feedback.success('Lesson plan saved');
      await this.router.navigateByUrl(this.returnPath(outcome.value));
      return true;
    }
    if (Object.keys(outcome.problem.fieldErrors).length > 0) {
      this.problem.set(outcome.problem);
      return false;
    }
    await this.showFailure(outcome.problem);
    return false;
  }

  private returnPath(saved: LessonPlanResponse): string {
    const cameFromStudent = this.id() === undefined && this.studentId() === saved.studentId;
    return cameFromStudent ? `/students/${saved.studentId}` : '/lesson-plans';
  }

  private async load(id: string | undefined): Promise<void> {
    const [students, plan] = await Promise.all([
      this.studentsStore.load(''),
      id === undefined ? Promise.resolve(null) : this.store.loadOne(id),
    ]);
    this.studentsLoaded.set(true);
    const failed = plan !== null && !plan.ok ? plan : !students.ok ? students : null;
    if (failed !== null) {
      await this.showFailure(failed.problem);
    }
  }

  private async showFailure(problem: ApiProblem): Promise<void> {
    this.feedback.failure(problem);
    if (problem.status === 404) {
      await this.router.navigate(['/lesson-plans']);
    }
  }
}
