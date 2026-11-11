import { Pipe, PipeTransform } from '@angular/core';
import { EnglishLevel } from '../../features/students/student.model';

const ENGLISH_LEVEL_LABELS: Record<EnglishLevel, string> = {
  Beginner: 'Beginner',
  Elementary: 'Elementary',
  Intermediate: 'Intermediate',
  UpperIntermediate: 'Upper intermediate',
  Advanced: 'Advanced',
};

export function englishLevelLabel(level: EnglishLevel): string {
  return ENGLISH_LEVEL_LABELS[level];
}

@Pipe({ name: 'englishLevelLabel' })
export class EnglishLevelLabel implements PipeTransform {
  transform(level: EnglishLevel): string {
    return englishLevelLabel(level);
  }
}
