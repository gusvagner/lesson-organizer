import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { RouterLink } from '@angular/router';
import { LessonDateLabel } from '../../../shared/labels/lesson-date-label';
import { LessonPlanResponse } from '../lesson-plan.model';
import { StatusChange } from './lesson-plan-events';
import { StatusSelect } from './status-select';

@Component({
  selector: 'lo-lesson-plan-table',
  imports: [
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    RouterLink,
    LessonDateLabel,
    StatusSelect,
  ],
  template: `
    <div class="table-card">
      <table mat-table [dataSource]="plans()" [trackBy]="byId">
        <ng-container matColumnDef="title">
          <th mat-header-cell *matHeaderCellDef>Title</th>
          <td mat-cell *matCellDef="let plan">
            <a [routerLink]="['/lesson-plans', plan.id, 'edit']">{{ plan.title }}</a>
          </td>
        </ng-container>
        <ng-container matColumnDef="student">
          <th mat-header-cell *matHeaderCellDef>Student</th>
          <td mat-cell *matCellDef="let plan">
            <a [routerLink]="['/students', plan.studentId]">{{ plan.studentName }}</a>
          </td>
        </ng-container>
        <ng-container matColumnDef="date">
          <th mat-header-cell *matHeaderCellDef>Date</th>
          <td mat-cell *matCellDef="let plan">{{ plan.lessonDate | lessonDateLabel }}</td>
        </ng-container>
        <ng-container matColumnDef="status">
          <th mat-header-cell *matHeaderCellDef>Status</th>
          <td mat-cell *matCellDef="let plan">
            <lo-status-select
              [status]="plan.status"
              [label]="'Status of ' + plan.title"
              [disabled]="saving()"
              (statusChange)="statusChange.emit({ id: plan.id, status: $event })"
            />
          </td>
        </ng-container>
        <ng-container matColumnDef="actions">
          <th mat-header-cell *matHeaderCellDef><span class="visually-hidden">Actions</span></th>
          <td mat-cell *matCellDef="let plan" class="actions">
            <button
              mat-icon-button
              class="delete-lesson-plan"
              type="button"
              [disabled]="saving()"
              [attr.aria-label]="'Delete ' + plan.title"
              (click)="remove.emit(plan)"
            >
              <mat-icon>delete</mat-icon>
            </button>
          </td>
        </ng-container>
        <tr mat-header-row *matHeaderRowDef="columns()"></tr>
        <tr mat-row *matRowDef="let row; columns: columns()"></tr>
      </table>
    </div>
  `,
  styleUrl: '../../../shared/layout/table-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LessonPlanTable {
  readonly plans = input.required<LessonPlanResponse[]>();
  readonly saving = input(false);
  readonly showStudent = input(true);
  readonly statusChange = output<StatusChange>();
  readonly remove = output<LessonPlanResponse>();

  protected readonly byId = (_index: number, plan: LessonPlanResponse) => plan.id;

  protected readonly columns = computed(() =>
    this.showStudent()
      ? ['title', 'student', 'date', 'status', 'actions']
      : ['title', 'date', 'status', 'actions'],
  );
}
