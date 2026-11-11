import { ChangeDetectionStrategy, Component, input, output, viewChild } from '@angular/core';
import { MatSelect, MatSelectChange, MatSelectModule } from '@angular/material/select';
import { LessonPlanStatusLabel } from '../../../shared/labels/lesson-plan-status-label';
import { LESSON_PLAN_STATUSES, LessonPlanStatus } from '../lesson-plan.model';

@Component({
  selector: 'lo-status-select',
  imports: [MatSelectModule, LessonPlanStatusLabel],
  template: `
    <mat-select
      class="status-select"
      [aria-label]="label()"
      [value]="status()"
      [disabled]="disabled()"
      (selectionChange)="choose($event)"
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
  readonly label = input('Status');
  readonly statusChange = output<LessonPlanStatus>();
  protected readonly statuses = LESSON_PLAN_STATUSES;
  private readonly select = viewChild.required(MatSelect);

  protected choose(change: MatSelectChange<LessonPlanStatus>): void {
    this.select().value = this.status();
    this.statusChange.emit(change.value);
  }
}
