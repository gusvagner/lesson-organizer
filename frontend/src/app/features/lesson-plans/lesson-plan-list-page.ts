import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'lo-lesson-plan-list-page',
  template: '<h1>Lesson plans</h1>',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LessonPlanListPage {}
