import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { LessonPlanStatusLabel } from '../../../shared/labels/lesson-plan-status-label';
import { StudentResponse } from '../../students/student.model';
import { LESSON_PLAN_STATUSES, LessonPlanFilter, LessonPlanStatus } from '../lesson-plan.model';
import { isFiltered } from '../lesson-plan-query';

@Component({
  selector: 'lo-lesson-plan-filters',
  imports: [MatButtonModule, MatFormFieldModule, MatSelectModule, LessonPlanStatusLabel],
  template: `
    <mat-form-field appearance="outline">
      <mat-label>Student</mat-label>
      <mat-select
        class="student-filter"
        [value]="filter().studentId"
        (selectionChange)="changeStudent($event.value)"
      >
        <mat-option [value]="null">All students</mat-option>
        @for (student of students(); track student.id) {
          <mat-option [value]="student.id">{{ student.name }}</mat-option>
        }
      </mat-select>
    </mat-form-field>
    <mat-form-field appearance="outline">
      <mat-label>Status</mat-label>
      <mat-select
        class="status-filter"
        [value]="filter().status"
        (selectionChange)="changeStatus($event.value)"
      >
        <mat-option [value]="null">All statuses</mat-option>
        @for (status of statuses; track status) {
          <mat-option [value]="status">{{ status | lessonPlanStatusLabel }}</mat-option>
        }
      </mat-select>
    </mat-form-field>
    @if (filtered()) {
      <button mat-button type="button" class="clear-filters" (click)="clear()">
        Clear filters
      </button>
    }
  `,
  styles: `
    :host {
      display: flex;
      flex-wrap: wrap;
      align-items: baseline;
      gap: 8px 16px;
    }
    mat-form-field {
      width: 220px;
      max-width: 100%;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LessonPlanFilters {
  readonly students = input.required<StudentResponse[]>();
  readonly filter = input.required<LessonPlanFilter>();
  readonly filterChange = output<LessonPlanFilter>();

  protected readonly statuses = LESSON_PLAN_STATUSES;
  protected readonly filtered = computed(() => isFiltered(this.filter()));

  protected changeStudent(studentId: string | null): void {
    this.filterChange.emit({ ...this.filter(), studentId });
  }

  protected changeStatus(status: LessonPlanStatus | null): void {
    this.filterChange.emit({ ...this.filter(), status });
  }

  protected clear(): void {
    this.filterChange.emit({ studentId: null, status: null });
  }
}
