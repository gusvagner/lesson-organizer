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
import { StudentForm } from './components/student-form';
import { StudentRequest, StudentResponse } from './student.model';
import { StudentsStore } from './students-store';

@Component({
  selector: 'lo-student-form-page',
  imports: [RouterLink, MatButtonModule, MatIconModule, StudentForm],
  template: `
    <a mat-button class="back" [routerLink]="backLink()">
      <mat-icon>arrow_back</mat-icon>
      Back
    </a>
    <h1>{{ id() ? 'Edit student' : 'New student' }}</h1>
    <lo-student-form
      [student]="student()"
      [saving]="store.saving()"
      [problem]="problem()"
      (save)="save($event)"
    />
  `,
  styleUrl: '../../shared/layout/page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentFormPage {
  private readonly router = inject(Router);
  private readonly feedback = inject(Feedback);
  protected readonly store = inject(StudentsStore);

  readonly id = input<string | undefined>();

  protected readonly problem = signal<ApiProblem | null>(null);
  protected readonly student = computed(() => {
    const student = this.store.selected();
    return this.id() !== undefined && student?.id === this.id() ? student : null;
  });
  protected readonly backLink = computed(() => {
    const id = this.id();
    return id === undefined ? ['/students'] : ['/students', id];
  });

  constructor() {
    effect(() => {
      const id = this.id();
      untracked(() => id !== undefined && void this.load(id));
    });
  }

  protected async save(request: StudentRequest): Promise<void> {
    this.problem.set(null);
    const id = this.id();
    const outcome =
      id === undefined ? await this.store.create(request) : await this.store.update(id, request);
    await this.handleSaved(outcome);
  }

  private async handleSaved(outcome: Outcome<StudentResponse>): Promise<void> {
    if (outcome.ok) {
      this.feedback.success('Student saved');
      await this.router.navigate(['/students', outcome.value.id]);
      return;
    }
    if (Object.keys(outcome.problem.fieldErrors).length > 0) {
      this.problem.set(outcome.problem);
      return;
    }
    await this.showFailure(outcome.problem);
  }

  private async load(id: string): Promise<void> {
    const outcome = await this.store.loadOne(id);
    if (!outcome.ok) {
      await this.showFailure(outcome.problem);
    }
  }

  private async showFailure(problem: ApiProblem): Promise<void> {
    this.feedback.failure(problem);
    if (problem.status === 404) {
      await this.router.navigate(['/students']);
    }
  }
}
