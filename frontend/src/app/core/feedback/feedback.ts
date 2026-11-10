import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiProblem } from '../http/api-problem';

const GENERIC_FAILURE = 'Something went wrong. Try again.';
const DURATION_MS = 4000;

export function failureMessage(problem: ApiProblem): string {
  if (problem.status === 404) {
    return 'Not found';
  }
  if (problem.status === 0 || problem.status >= 500) {
    return GENERIC_FAILURE;
  }
  return problem.detail ?? 'Please check the form.';
}

@Injectable({ providedIn: 'root' })
export class Feedback {
  private readonly snackBar = inject(MatSnackBar);

  success(message: string): void {
    this.snackBar.open(message, 'Close', { duration: DURATION_MS });
  }

  failure(problem: ApiProblem): void {
    this.snackBar.open(failureMessage(problem), 'Close', { duration: DURATION_MS });
  }
}
