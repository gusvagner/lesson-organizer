import { Injectable, inject } from '@angular/core';
import { Feedback } from '../../core/feedback/feedback';
import { Confirmation } from '../../shared/confirm-dialog/confirmation';
import { HAS_LESSON_PLANS_MESSAGE, StudentResponse } from './student.model';
import { StudentsStore } from './students-store';

@Injectable({ providedIn: 'root' })
export class StudentActions {
  private readonly store = inject(StudentsStore);
  private readonly feedback = inject(Feedback);
  private readonly confirmation = inject(Confirmation);

  async remove(student: StudentResponse): Promise<boolean> {
    if (student.lessonPlanCount > 0) {
      this.feedback.inform(HAS_LESSON_PLANS_MESSAGE);
      return false;
    }
    const confirmed = await this.confirmation.ask({
      title: 'Delete student?',
      message: `Delete ${student.name}? This cannot be undone.`,
      confirmLabel: 'Delete',
    });
    if (!confirmed) {
      return false;
    }
    const outcome = await this.store.remove(student.id);
    if (outcome.ok) {
      this.feedback.success('Student deleted');
      return true;
    }
    this.feedback.failure(outcome.problem);
    return false;
  }
}
