import {
  CdkDrag,
  CdkDragDrop,
  CdkDragPlaceholder,
  CdkDropList,
  CdkDropListGroup,
} from '@angular/cdk/drag-drop';
import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { lessonPlanStatusLabel } from '../../../shared/labels/lesson-plan-status-label';
import { LESSON_PLAN_STATUSES, LessonPlanResponse, LessonPlanStatus } from '../lesson-plan.model';
import { LessonPlanBoardCard } from './lesson-plan-board-card';
import { StatusChange } from './lesson-plan-events';

interface BoardColumn {
  status: LessonPlanStatus;
  label: string;
  plans: LessonPlanResponse[];
}

type BoardDrop = CdkDragDrop<LessonPlanStatus, LessonPlanStatus, LessonPlanResponse>;

@Component({
  selector: 'lo-lesson-plan-board',
  imports: [CdkDrag, CdkDragPlaceholder, CdkDropList, CdkDropListGroup, LessonPlanBoardCard],
  templateUrl: './lesson-plan-board.html',
  styleUrl: './lesson-plan-board.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LessonPlanBoard {
  readonly plans = input.required<LessonPlanResponse[]>();
  readonly saving = input(false);
  readonly statuses = input<readonly LessonPlanStatus[]>(LESSON_PLAN_STATUSES);
  readonly statusChange = output<StatusChange>();
  readonly remove = output<LessonPlanResponse>();

  protected readonly dragStartDelay = { touch: 250, mouse: 0 };

  protected readonly columns = computed<BoardColumn[]>(() =>
    this.statuses().map((status) => ({
      status,
      label: lessonPlanStatusLabel(status),
      plans: this.plans()
        .filter((plan) => plan.status === status)
        .sort(byDateThenTitle),
    })),
  );

  protected drop(event: BoardDrop): void {
    if (event.previousContainer === event.container) {
      return;
    }
    this.statusChange.emit({ id: event.item.data.id, status: event.container.data });
  }
}

function byDateThenTitle(a: LessonPlanResponse, b: LessonPlanResponse): number {
  return a.lessonDate.localeCompare(b.lessonDate) || a.title.localeCompare(b.title);
}
