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
      --strong: var(--lo-status-to-prepare);
      --soft: var(--lo-status-to-prepare-soft);
      display: inline-flex;
    }
    :host([data-status='Ready']) {
      --strong: var(--lo-status-ready);
      --soft: var(--lo-status-ready-soft);
    }
    :host([data-status='Delivered']) {
      --strong: var(--lo-status-delivered);
      --soft: var(--lo-status-delivered-soft);
    }
    .status-select {
      --mat-select-enabled-trigger-text-color: var(--strong);
      --mat-select-enabled-arrow-color: var(--strong);
      min-width: 132px;
      padding: 4px 10px 4px 14px;
      border-radius: var(--lo-radius-full);
      background-color: var(--soft);
      font-size: 13px;
      font-weight: 600;
      line-height: 20px;
    }
  `,
  host: { '[attr.data-status]': 'status()' },
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
