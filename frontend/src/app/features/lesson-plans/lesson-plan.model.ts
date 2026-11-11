export const LESSON_PLAN_STATUSES = ['ToPrepare', 'Ready', 'Delivered'] as const;

export type LessonPlanStatus = (typeof LESSON_PLAN_STATUSES)[number];

export interface LessonPlanResponse {
  id: string;
  studentId: string;
  studentName: string;
  title: string;
  description: string | null;
  lessonDate: string;
  status: LessonPlanStatus;
}

export interface LessonPlanRequest {
  studentId: string;
  title: string;
  description: string | null;
  lessonDate: string;
}

export interface LessonPlanFilter {
  studentId: string | null;
  status: LessonPlanStatus | null;
}
