import { formatLessonDate, parseLessonDate } from './lesson-date';

describe('lesson date', () => {
  it('parse → local date with the same day', () => {
    const date = parseLessonDate('2026-11-12');
    expect([date.getFullYear(), date.getMonth(), date.getDate()]).toEqual([2026, 10, 12]);
  });

  it('format → zero padded yyyy-MM-dd', () => {
    expect(formatLessonDate(new Date(2026, 0, 5))).toBe('2026-01-05');
  });

  it('round trip → keeps the day', () => {
    expect(formatLessonDate(parseLessonDate('2026-03-01'))).toBe('2026-03-01');
  });
});
