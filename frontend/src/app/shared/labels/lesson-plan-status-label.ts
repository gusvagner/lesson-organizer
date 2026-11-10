import { Pipe, PipeTransform } from '@angular/core';
import { LessonPlanStatus } from '../../features/lesson-plans/lesson-plan.model';

const LESSON_PLAN_STATUS_LABELS: Record<LessonPlanStatus, string> = {
  ToPrepare: 'To prepare',
  Ready: 'Ready',
  Delivered: 'Delivered',
};

export function lessonPlanStatusLabel(status: LessonPlanStatus): string {
  return LESSON_PLAN_STATUS_LABELS[status];
}

@Pipe({ name: 'lessonPlanStatusLabel' })
export class LessonPlanStatusLabel implements PipeTransform {
  transform(status: LessonPlanStatus): string {
    return lessonPlanStatusLabel(status);
  }
}
