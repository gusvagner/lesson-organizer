import { ChangeDetectionStrategy, Component, inject, input, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Router, RouterLink } from '@angular/router';
import { AuthStore } from '../../core/auth/auth-store';
import { AppLogo } from '../../core/layout/app-logo';
import { safeReturnUrl } from '../../core/auth/safe-return-url';
import { ApiProblem } from '../../core/http/api-problem';
import { failureMessage } from '../../core/feedback/feedback';
import { fieldErrorMessage } from '../../shared/forms/field-error-message';

@Component({
  selector: 'lo-login-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    AppLogo,
  ],
  templateUrl: './login-page.html',
  styleUrl: './auth-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginPage {
  private readonly router = inject(Router);
  protected readonly auth = inject(AuthStore);

  readonly returnUrl = input<string | undefined>();
  readonly registered = input<string | undefined>();

  protected readonly failure = signal<string | null>(null);
  protected readonly fieldError = fieldErrorMessage;
  protected readonly form = new FormGroup({
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email, Validators.maxLength(254)],
    }),
    password: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.failure.set(null);
    const { email, password } = this.form.getRawValue();
    const outcome = await this.auth.login(email.trim(), password);
    if (outcome.ok) {
      await this.router.navigateByUrl(safeReturnUrl(this.returnUrl()));
      return;
    }
    this.failure.set(loginFailureMessage(outcome.problem));
  }
}

function loginFailureMessage(problem: ApiProblem): string | null {
  if (problem.status === 401) {
    return problem.detail ?? 'Invalid email or password.';
  }
  if (problem.status === 429) {
    return 'Too many attempts. Try again in a minute.';
  }
  return failureMessage(problem);
}
