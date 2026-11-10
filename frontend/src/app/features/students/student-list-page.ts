import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'lo-student-list-page',
  template: '<h1>Students</h1>',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StudentListPage {}
