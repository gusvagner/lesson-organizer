import { Injectable, inject } from '@angular/core';
import { Feedback } from '../../core/feedback/feedback';
import { Confirmation } from '../../shared/confirm-dialog/confirmation';
import { lessonPlanStatusLabel } from '../../shared/labels/lesson-plan-status-label';
import { StatusChange } from './components/lesson-plan-events';
import { LessonPlanResponse } from './lesson-plan.model';
import { LessonPlansStore } from './lesson-plans-store';

@Injectable({ providedIn: 'root' })
export class LessonPlanActions {
  private readonly store = inject(LessonPlansStore);
  private readonly feedback = inject(Feedback);
  private readonly confirmation = inject(Confirmation);

  async changeStatus(change: StatusChange): Promise<void> {
    const outcome = await this.store.changeStatus(change.id, change.status);
    if (outcome.ok) {
      this.feedback.success(`Status changed to ${lessonPlanStatusLabel(change.status)}`);
      return;
    }
    this.feedback.failure(outcome.problem);
  }

  async remove(plan: LessonPlanResponse): Promise<boolean> {
    const confirmed = await this.confirmation.ask({
      title: 'Delete lesson plan?',
      message: `Delete "${plan.title}"? This cannot be undone.`,
      confirmLabel: 'Delete',
    });
    if (!confirmed) {
      return false;
    }
    const outcome = await this.store.remove(plan.id);
    if (outcome.ok) {
      this.feedback.success('Lesson plan deleted');
      return true;
    }
    this.feedback.failure(outcome.problem);
    return false;
  }
}
