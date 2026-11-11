import { LessonPlanStatus } from '../lesson-plan.model';

export interface StatusChange {
  id: string;
  status: LessonPlanStatus;
}
