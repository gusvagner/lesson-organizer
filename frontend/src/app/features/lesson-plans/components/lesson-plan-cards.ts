import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { LessonDateLabel } from '../../../shared/labels/lesson-date-label';
import { LessonPlanResponse } from '../lesson-plan.model';
import { StatusChange } from './lesson-plan-events';
import { StatusSelect } from './status-select';

@Component({
  selector: 'lo-lesson-plan-cards',
  imports: [
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    RouterLink,
    LessonDateLabel,
    StatusSelect,
  ],
  template: `
    @for (plan of plans(); track plan.id) {
      <mat-card appearance="outlined">
        <mat-card-header>
          <mat-card-title>
            <a [routerLink]="['/lesson-plans', plan.id, 'edit']">{{ plan.title }}</a>
          </mat-card-title>
          <mat-card-subtitle>
            {{ plan.lessonDate | lessonDateLabel }}
            @if (showStudent()) {
              · <a [routerLink]="['/students', plan.studentId]">{{ plan.studentName }}</a>
            }
          </mat-card-subtitle>
        </mat-card-header>
        <mat-card-actions>
          <lo-status-select
            [status]="plan.status"
            [label]="'Status of ' + plan.title"
            [disabled]="saving()"
            (statusChange)="statusChange.emit({ id: plan.id, status: $event })"
          />
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
        </mat-card-actions>
      </mat-card>
    }
  `,
  styleUrl: './lesson-plan-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LessonPlanCards {
  readonly plans = input.required<LessonPlanResponse[]>();
  readonly saving = input(false);
  readonly showStudent = input(true);
  readonly statusChange = output<StatusChange>();
  readonly remove = output<LessonPlanResponse>();
}
