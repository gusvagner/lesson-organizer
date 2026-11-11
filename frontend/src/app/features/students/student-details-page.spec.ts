import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { Router, provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { click, settle, textOf } from '../../testing/dom';
import { pageFakes, providePageFakes } from '../../testing/page-fakes';
import { StatusSelect } from '../lesson-plans/components/status-select';
import { aLessonPlan } from '../lesson-plans/testing/fake-lesson-plans-api';
import { StudentDetailsPage } from './student-details-page';
import { aStudent } from './testing/fake-students-api';

@Component({ template: '' })
class Blank {}

describe('StudentDetailsPage', () => {
  let harness: RouterTestingHarness;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        providePageFakes(),
        provideRouter(
          [
            { path: 'students', component: Blank },
            { path: 'students/:id', component: StudentDetailsPage },
            { path: 'lesson-plans/new', component: Blank },
          ],
          withComponentInputBinding(),
        ),
      ],
    });
    pageFakes().students.students = [aStudent({ id: 's1', name: 'Ana', notes: 'Likes music' })];
    pageFakes().plans.plans = [
      aLessonPlan({ id: 'p1', studentId: 's1', title: 'Present perfect' }),
      aLessonPlan({ id: 'p2', studentId: 's2', title: 'Other student plan' }),
    ];
    harness = await RouterTestingHarness.create();
  });

  async function open(url: string): Promise<HTMLElement> {
    await harness.navigateByUrl(url);
    await settle();
    await harness.fixture.whenStable();
    return harness.routeNativeElement!;
  }

  it('shows the student and only their lesson plans', async () => {
    const text = textOf(await open('/students/s1'));
    expect(text).toContain('Likes music');
    expect(text).toContain('Present perfect');
    expect(text).not.toContain('Other student plan');
    expect(pageFakes().plans.filters).toEqual([{ studentId: 's1', status: null }]);
  });

  it('"New lesson plan" → /lesson-plans/new?studentId=s1', async () => {
    const root = await open('/students/s1');
    click(root, 'a.new-lesson-plan');
    await settle();
    expect(TestBed.inject(Router).url).toBe('/lesson-plans/new?studentId=s1');
  });

  it('student 404 → "Not found" and back to /students', async () => {
    await open('/students/missing');
    expect(pageFakes().feedback.messages).toEqual(['Not found']);
    expect(TestBed.inject(Router).url).toBe('/students');
  });

  it('no plans → "No lesson plans yet" with "New lesson plan"', async () => {
    pageFakes().plans.plans = [];
    const text = textOf(await open('/students/s1'));
    expect(text).toContain('No lesson plans yet');
    expect(text).toContain('New lesson plan');
  });

  it('status change → "Status changed to Ready"', async () => {
    await open('/students/s1');
    const select = harness.fixture.debugElement.query(By.directive(StatusSelect));
    (select.componentInstance as StatusSelect).statusChange.emit('Ready');
    await settle();
    expect(pageFakes().feedback.messages).toEqual(['Status changed to Ready']);
    expect(pageFakes().plans.plans[0].status).toBe('Ready');
  });

  it('delete student confirmed with plans → 409 message, stays', async () => {
    pageFakes().students.students[0].lessonPlanCount = 1;
    const root = await open('/students/s1');
    click(root, 'button.delete-student');
    await settle();
    expect(pageFakes().feedback.messages[0]).toContain('This student has lesson plans');
    expect(TestBed.inject(Router).url).toBe('/students/s1');
  });

  it('delete student confirmed → "Student deleted" and /students', async () => {
    const root = await open('/students/s1');
    click(root, 'button.delete-student');
    await settle();
    expect(pageFakes().feedback.messages).toEqual(['Student deleted']);
    expect(TestBed.inject(Router).url).toBe('/students');
  });
});
