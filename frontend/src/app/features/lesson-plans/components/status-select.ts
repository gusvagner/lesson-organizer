import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatSelectModule } from '@angular/material/select';
import { LessonPlanStatusLabel } from '../../../shared/labels/lesson-plan-status-label';
import { LESSON_PLAN_STATUSES, LessonPlanStatus } from '../lesson-plan.model';

@Component({
  selector: 'lo-status-select',
  imports: [MatSelectModule, LessonPlanStatusLabel],
  template: `
    <mat-select
      class="status-select"
      aria-label="Status"
      [value]="status()"
      [disabled]="disabled()"
      (selectionChange)="statusChange.emit($event.value)"
    >
      @for (option of statuses; track option) {
        <mat-option [value]="option">{{ option | lessonPlanStatusLabel }}</mat-option>
      }
    </mat-select>
  `,
  styles: `
    :host {
      display: inline-block;
      min-width: 120px;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatusSelect {
  readonly status = input.required<LessonPlanStatus>();
  readonly disabled = input(false);
  readonly statusChange = output<LessonPlanStatus>();
  protected readonly statuses = LESSON_PLAN_STATUSES;
}
