import {
  ChangeDetectionStrategy,
  Component,
  effect,
  input,
  output,
  signal,
  untracked,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ApiProblem } from '../../../core/http/api-problem';
import { formatLessonDate, parseLessonDate } from '../../../shared/dates/lesson-date';
import { fieldErrorMessage } from '../../../shared/forms/field-error-message';
import { notBlank } from '../../../shared/forms/not-blank';
import { optionalText } from '../../../shared/forms/optional-text';
import { applyServerErrors } from '../../../shared/forms/server-errors';
import { StudentResponse } from '../../students/student.model';
import { LessonPlanRequest, LessonPlanResponse } from '../lesson-plan.model';

@Component({
  selector: 'lo-lesson-plan-form',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDatepickerModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
  ],
  templateUrl: './lesson-plan-form.html',
  styleUrl: '../../../shared/forms/entity-form.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LessonPlanForm {
  readonly plan = input<LessonPlanResponse | null>(null);
  readonly students = input.required<StudentResponse[]>();
  readonly preselectedStudentId = input<string | null>(null);
  readonly saving = input(false);
  readonly problem = input<ApiProblem | null>(null);
  readonly save = output<LessonPlanRequest>();

  protected readonly fieldError = fieldErrorMessage;
  protected readonly generalErrors = signal<string[]>([]);
  protected readonly form = new FormGroup({
    studentId: new FormControl<string | null>(null, Validators.required),
    title: new FormControl('', {
      nonNullable: true,
      validators: [notBlank, Validators.maxLength(150)],
    }),
    lessonDate: new FormControl<Date | null>(null, Validators.required),
    description: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(5000)],
    }),
  });

  constructor() {
    effect(() => {
      const plan = this.plan();
      const preselected = this.preselectedStudentId();
      untracked(() => this.fill(plan, preselected));
    });
    effect(() => {
      const problem = this.problem();
      untracked(() => this.generalErrors.set(problem ? applyServerErrors(this.form, problem) : []));
    });
  }

  protected submit(): void {
    const { studentId, title, lessonDate, description } = this.form.getRawValue();
    if (this.form.invalid || studentId === null || lessonDate === null) {
      this.form.markAllAsTouched();
      return;
    }
    this.save.emit({
      studentId,
      title: title.trim(),
      description: optionalText(description),
      lessonDate: formatLessonDate(lessonDate),
    });
  }

  private fill(plan: LessonPlanResponse | null, preselectedStudentId: string | null): void {
    this.form.reset({
      studentId: plan?.studentId ?? preselectedStudentId,
      title: plan?.title ?? '',
      lessonDate: plan ? parseLessonDate(plan.lessonDate) : null,
      description: plan?.description ?? '',
    });
  }
}
