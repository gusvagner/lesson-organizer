import { EnglishLevelLabel, englishLevelLabel } from './english-level-label';
import { LessonDateLabel } from './lesson-date-label';
import { LessonPlanStatusLabel, lessonPlanStatusLabel } from './lesson-plan-status-label';

describe('labels', () => {
  it('UpperIntermediate → "Upper intermediate"', () => {
    expect(englishLevelLabel('UpperIntermediate')).toBe('Upper intermediate');
    expect(new EnglishLevelLabel().transform('Beginner')).toBe('Beginner');
  });

  it('ToPrepare → "To prepare"', () => {
    expect(lessonPlanStatusLabel('ToPrepare')).toBe('To prepare');
    expect(new LessonPlanStatusLabel().transform('Delivered')).toBe('Delivered');
  });

  it('lesson date label → "Nov 12, 2026"', () => {
    expect(new LessonDateLabel().transform('2026-11-12')).toBe('Nov 12, 2026');
  });
});
