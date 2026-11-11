import { Provider } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Feedback } from '../core/feedback/feedback';
import { FakeFeedback } from '../core/feedback/testing/fake-feedback';
import { LessonPlansApi } from '../features/lesson-plans/lesson-plans-api';
import { FakeLessonPlansApi } from '../features/lesson-plans/testing/fake-lesson-plans-api';
import { StudentsApi } from '../features/students/students-api';
import { FakeStudentsApi } from '../features/students/testing/fake-students-api';
import { Confirmation } from '../shared/confirm-dialog/confirmation';
import { FakeConfirmation } from '../shared/confirm-dialog/testing/fake-confirmation';

export function providePageFakes(): Provider[] {
  return [
    { provide: StudentsApi, useClass: FakeStudentsApi },
    { provide: LessonPlansApi, useClass: FakeLessonPlansApi },
    { provide: Feedback, useClass: FakeFeedback },
    { provide: Confirmation, useClass: FakeConfirmation },
  ];
}

export function pageFakes() {
  return {
    students: TestBed.inject(StudentsApi) as unknown as FakeStudentsApi,
    plans: TestBed.inject(LessonPlansApi) as unknown as FakeLessonPlansApi,
    feedback: TestBed.inject(Feedback) as unknown as FakeFeedback,
    confirmation: TestBed.inject(Confirmation) as unknown as FakeConfirmation,
  };
}
