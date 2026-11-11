export const ENGLISH_LEVELS = [
  'Beginner',
  'Elementary',
  'Intermediate',
  'UpperIntermediate',
  'Advanced',
] as const;

export const HAS_LESSON_PLANS_MESSAGE =
  'This student has lesson plans. Delete them first or move them to another student.';

export type EnglishLevel = (typeof ENGLISH_LEVELS)[number];

export interface StudentResponse {
  id: string;
  name: string;
  email: string | null;
  englishLevel: EnglishLevel;
  notes: string | null;
  lessonPlanCount: number;
}

export interface StudentRequest {
  name: string;
  email: string | null;
  englishLevel: EnglishLevel;
  notes: string | null;
}
