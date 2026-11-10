import { FormGroup } from '@angular/forms';
import { ApiProblem } from '../../core/http/api-problem';

export function applyServerErrors(form: FormGroup, problem: ApiProblem): string[] {
  const unmatched: string[] = [];
  for (const [field, messages] of Object.entries(problem.fieldErrors)) {
    const control = form.get(field);
    if (control === null || field === '') {
      unmatched.push(...messages);
      continue;
    }
    control.setErrors({ server: messages[0] });
    control.markAsTouched();
  }
  return unmatched;
}
