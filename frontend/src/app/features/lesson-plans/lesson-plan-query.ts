import { LESSON_PLAN_STATUSES, LessonPlanFilter, LessonPlanStatus } from './lesson-plan.model';

const GUID_SHAPE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export function filterFromQuery(
  studentId: string | undefined,
  status: string | undefined,
): LessonPlanFilter {
  return {
    studentId: studentId !== undefined && GUID_SHAPE.test(studentId) ? studentId : null,
    status: isLessonPlanStatus(status) ? status : null,
  };
}

export function isFiltered(filter: LessonPlanFilter): boolean {
  return filter.studentId !== null || filter.status !== null;
}

function isLessonPlanStatus(value: string | undefined): value is LessonPlanStatus {
  return LESSON_PLAN_STATUSES.some((status) => status === value);
}
