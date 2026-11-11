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
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ApiProblem } from '../../../core/http/api-problem';
import { emailAddress } from '../../../shared/forms/email-address';
import { fieldErrorMessage } from '../../../shared/forms/field-error-message';
import { notBlank } from '../../../shared/forms/not-blank';
import { optionalText } from '../../../shared/forms/optional-text';
import { applyServerErrors } from '../../../shared/forms/server-errors';
import { EnglishLevelLabel } from '../../../shared/labels/english-level-label';
import { ENGLISH_LEVELS, EnglishLevel, StudentRequest, StudentResponse } from '../student.model';

@Component({
  selector: 'lo-student-form',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    EnglishLevelLabel,
  ],
  templateUrl: './student-form.html',
  styleUrl: '../../../shared/forms/entity-form.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentForm {
  readonly student = input<StudentResponse | null>(null);
  readonly saving = input(false);
  readonly problem = input<ApiProblem | null>(null);
  readonly save = output<StudentRequest>();

  protected readonly levels = ENGLISH_LEVELS;
  protected readonly fieldError = fieldErrorMessage;
  protected readonly generalErrors = signal<string[]>([]);
  protected readonly form = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [notBlank, Validators.maxLength(150)],
    }),
    email: new FormControl('', {
      nonNullable: true,
      validators: [emailAddress, Validators.maxLength(254)],
    }),
    englishLevel: new FormControl<EnglishLevel | null>(null, Validators.required),
    notes: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(2000)] }),
  });

  constructor() {
    effect(() => {
      const student = this.student();
      untracked(() => this.fill(student));
    });
    effect(() => {
      const problem = this.problem();
      untracked(() => this.generalErrors.set(problem ? applyServerErrors(this.form, problem) : []));
    });
  }

  protected submit(): void {
    const { name, email, englishLevel, notes } = this.form.getRawValue();
    if (this.form.invalid || englishLevel === null) {
      this.form.markAllAsTouched();
      return;
    }
    this.save.emit({
      name: name.trim(),
      email: optionalText(email),
      englishLevel,
      notes: optionalText(notes),
    });
  }

  private fill(student: StudentResponse | null): void {
    this.form.reset({
      name: student?.name ?? '',
      email: student?.email ?? '',
      englishLevel: student?.englishLevel ?? null,
      notes: student?.notes ?? '',
    });
  }
}
