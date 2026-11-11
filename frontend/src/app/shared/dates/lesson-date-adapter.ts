import { Injectable, Provider } from '@angular/core';
import { DateAdapter, NativeDateAdapter, provideNativeDateAdapter } from '@angular/material/core';
import { parseLessonDate } from './lesson-date';

const ISO_DATE = /^\d{4}-\d{2}-\d{2}$/;

@Injectable()
export class LessonDateAdapter extends NativeDateAdapter {
  override parse(value: unknown, parseFormat?: unknown): Date | null {
    if (typeof value === 'string' && ISO_DATE.test(value.trim())) {
      return parseLessonDate(value.trim());
    }
    return super.parse(value, parseFormat);
  }
}

export function provideLessonDateAdapter(): Provider[] {
  return [provideNativeDateAdapter(), { provide: DateAdapter, useClass: LessonDateAdapter }];
}
