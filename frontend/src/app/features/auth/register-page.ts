import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Router, RouterLink } from '@angular/router';
import { AuthStore } from '../../core/auth/auth-store';
import { AppLogo } from '../../core/layout/app-logo';
import { failureMessage } from '../../core/feedback/feedback';
import { ApiProblem } from '../../core/http/api-problem';
import { fieldErrorMessage } from '../../shared/forms/field-error-message';
import { notBlank } from '../../shared/forms/not-blank';
import { applyServerErrors } from '../../shared/forms/server-errors';

@Component({
  selector: 'lo-register-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    AppLogo,
  ],
  templateUrl: './register-page.html',
  styleUrl: './auth-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterPage {
  private readonly router = inject(Router);
  protected readonly auth = inject(AuthStore);

  protected readonly failure = signal<string | null>(null);
  protected readonly fieldError = fieldErrorMessage;
  protected readonly form = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [notBlank, Validators.maxLength(150)],
    }),
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email, Validators.maxLength(254)],
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(8), Validators.maxLength(128)],
    }),
  });

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.failure.set(null);
    const { name, email, password } = this.form.getRawValue();
    const outcome = await this.auth.register(name.trim(), email.trim(), password);
    if (outcome.ok) {
      await this.router.navigate(['/login'], { queryParams: { registered: 1 } });
      return;
    }
    this.showProblem(outcome.problem);
  }

  private showProblem(problem: ApiProblem): void {
    if (problem.status === 409) {
      this.form.controls.email.setErrors({ server: problem.detail });
      this.form.controls.email.markAsTouched();
      return;
    }
    const unmatched = applyServerErrors(this.form, problem);
    const hasFieldErrors = Object.keys(problem.fieldErrors).length > 0;
    this.failure.set(hasFieldErrors ? (unmatched[0] ?? null) : failureMessage(problem));
  }
}
