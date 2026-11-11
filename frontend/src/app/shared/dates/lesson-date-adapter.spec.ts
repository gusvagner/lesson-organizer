import { TestBed } from '@angular/core/testing';
import { DateAdapter } from '@angular/material/core';
import { provideLessonDateAdapter } from './lesson-date-adapter';

interface NodeProcess {
  process: { env: Record<string, string | undefined> };
}

describe('LessonDateAdapter', () => {
  const env = (globalThis as unknown as NodeProcess).process.env;
  const originalTimeZone = env['TZ'];

  beforeEach(() => {
    env['TZ'] = 'America/Sao_Paulo';
    TestBed.configureTestingModule({ providers: [provideLessonDateAdapter()] });
  });

  afterEach(() => {
    env['TZ'] = originalTimeZone;
  });

  it('typed "2026-11-10" in UTC-3 → local Nov 10, not Nov 9', () => {
    const date = TestBed.inject(DateAdapter<Date>).parse('2026-11-10', null) as Date;
    expect([date.getFullYear(), date.getMonth(), date.getDate()]).toEqual([2026, 10, 10]);
  });

  it('typed "11/12/2026" → Nov 12', () => {
    const date = TestBed.inject(DateAdapter<Date>).parse('11/12/2026', null) as Date;
    expect(date.getDate()).toBe(12);
  });
});
