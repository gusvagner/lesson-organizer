import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { LessonDateLabel } from '../../../shared/labels/lesson-date-label';
import { LessonPlanResponse } from '../lesson-plan.model';
import { StatusChange } from './lesson-plan-events';
import { StatusSelect } from './status-select';

@Component({
  selector: 'lo-lesson-plan-board-card',
  imports: [MatButtonModule, MatIconModule, RouterLink, LessonDateLabel, StatusSelect],
  template: `
    <article class="card">
      <a class="card-title" [routerLink]="['/lesson-plans', plan().id, 'edit']">
        {{ plan().title }}
      </a>
      <div class="meta">
        <a class="student" [routerLink]="['/students', plan().studentId]">
          <mat-icon aria-hidden="true">person</mat-icon>
          {{ plan().studentName }}
        </a>
        <span class="date">
          <mat-icon aria-hidden="true">event</mat-icon>
          {{ plan().lessonDate | lessonDateLabel }}
        </span>
      </div>
      <footer>
        <lo-status-select
          [status]="plan().status"
          [disabled]="saving()"
          [label]="'Status of ' + plan().title"
          (statusChange)="statusChange.emit({ id: plan().id, status: $event })"
        />
        <button
          mat-icon-button
          class="delete-lesson-plan"
          type="button"
          [disabled]="saving()"
          [attr.aria-label]="'Delete ' + plan().title"
          (click)="remove.emit(plan())"
        >
          <mat-icon>delete</mat-icon>
        </button>
      </footer>
    </article>
  `,
  styleUrl: './lesson-plan-board-card.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LessonPlanBoardCard {
  readonly plan = input.required<LessonPlanResponse>();
  readonly saving = input(false);
  readonly statusChange = output<StatusChange>();
  readonly remove = output<LessonPlanResponse>();
}
