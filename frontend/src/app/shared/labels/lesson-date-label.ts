import { formatDate } from '@angular/common';
import { Pipe, PipeTransform } from '@angular/core';
import { parseLessonDate } from '../dates/lesson-date';

@Pipe({ name: 'lessonDateLabel' })
export class LessonDateLabel implements PipeTransform {
  transform(lessonDate: string): string {
    return formatDate(parseLessonDate(lessonDate), 'mediumDate', 'en-US');
  }
}
