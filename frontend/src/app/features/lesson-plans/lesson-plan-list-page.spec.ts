import { CdkDragDrop, CdkDropList } from '@angular/cdk/drag-drop';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { Router, provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { click, settle, textOf } from '../../testing/dom';
import { pageFakes, providePageFakes } from '../../testing/page-fakes';
import { aStudent } from '../students/testing/fake-students-api';
import { LessonPlanFilters } from './components/lesson-plan-filters';
import { StatusSelect } from './components/status-select';
import { LessonPlanListPage } from './lesson-plan-list-page';
import { LessonPlanResponse, LessonPlanStatus } from './lesson-plan.model';
import { aLessonPlan } from './testing/fake-lesson-plans-api';

const ANA = '01a1c6e4-b0ef-7355-873b-c8c3fed7c18d';
const BRUNO = '01a1c6e4-b127-71f0-88f6-f51cce079809';

@Component({ template: '' })
class Blank {}

describe('LessonPlanListPage', () => {
  let harness: RouterTestingHarness;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        providePageFakes(),
        provideRouter(
          [
            { path: 'lesson-plans', component: LessonPlanListPage },
            { path: 'students/new', component: Blank },
          ],
          withComponentInputBinding(),
        ),
      ],
    });
    pageFakes().students.students = [
      aStudent({ id: ANA, name: 'Ana' }),
      aStudent({ id: BRUNO, name: 'Bruno' }),
    ];
    pageFakes().plans.plans = [
      aLessonPlan({ id: 'p1', studentId: ANA, studentName: 'Ana', title: 'Present perfect' }),
      aLessonPlan({
        id: 'p2',
        studentId: BRUNO,
        studentName: 'Bruno',
        title: 'Phrasal verbs',
        status: 'Ready',
      }),
    ];
    harness = await RouterTestingHarness.create();
  });

  async function open(url: string): Promise<HTMLElement> {
    await harness.navigateByUrl(url);
    await settle();
    await harness.fixture.whenStable();
    return harness.routeNativeElement!;
  }

  it('shows title, student, date and status of every plan', async () => {
    const text = textOf(await open('/lesson-plans'));
    expect(text).toContain('Present perfect');
    expect(text).toContain('Bruno');
    expect(text).toContain('Nov 12, 2026');
  });

  it('board with three columns: To prepare, Ready, Delivered', async () => {
    const root = await open('/lesson-plans');
    const headings = Array.from(root.querySelectorAll('section[data-status] h2')).map(textOf);
    expect(headings).toEqual(['To prepare', 'Ready', 'Delivered']);
    expect(root.querySelector('table')).toBeNull();
  });

  it('?status=Ready → only the Ready column', async () => {
    const root = await open('/lesson-plans?status=Ready');
    const headings = Array.from(root.querySelectorAll('section[data-status] h2')).map(textOf);
    expect(headings).toEqual(['Ready']);
  });

  it('?studentId=<guid>&status=Ready → loads with both', async () => {
    await open(`/lesson-plans?studentId=${BRUNO}&status=Ready`);
    expect(pageFakes().plans.filters.at(-1)).toEqual({ studentId: BRUNO, status: 'Ready' });
  });

  it('?status=Done → loads unfiltered', async () => {
    await open('/lesson-plans?status=Done&studentId=abc');
    expect(pageFakes().plans.filters.at(-1)).toEqual({ studentId: null, status: null });
  });

  it('filter changed → query params updated', async () => {
    await open('/lesson-plans');
    const filters = harness.fixture.debugElement.query(By.directive(LessonPlanFilters));
    (filters.componentInstance as LessonPlanFilters).filterChange.emit({
      studentId: ANA,
      status: 'Ready',
    });
    await settle();
    expect(TestBed.inject(Router).url).toBe(`/lesson-plans?studentId=${ANA}&status=Ready`);
  });

  it('no students → "Create a student first" leading to /students/new', async () => {
    pageFakes().students.students = [];
    pageFakes().plans.plans = [];
    const root = await open('/lesson-plans');
    expect(textOf(root)).toContain('Create a student first');
    click(root, 'lo-empty-state button');
    await settle();
    expect(TestBed.inject(Router).url).toBe('/students/new');
  });

  it('filters match nothing → "No results"; Clear filters → unfiltered', async () => {
    const root = await open(`/lesson-plans?studentId=${ANA}&status=Delivered`);
    expect(textOf(root)).toContain('No results');
    click(root, 'lo-empty-state button');
    await settle();
    expect(TestBed.inject(Router).url).toBe('/lesson-plans');
  });

  it('status change → select disabled while saving, then "Status changed to Ready"', async () => {
    await open('/lesson-plans');
    const gate = pageFakes().plans.responder.hold('changeStatus');
    const selectOf = (title: string) =>
      harness.fixture.debugElement
        .queryAll(By.directive(StatusSelect))
        .map((debug) => debug.componentInstance as StatusSelect)
        .find((select) => select.label() === `Status of ${title}`)!;
    selectOf('Present perfect').statusChange.emit('Ready');
    await harness.fixture.whenStable();
    const ready = harness.routeNativeElement!.querySelector('section[data-status="Ready"]')!;
    expect(textOf(ready)).toContain('Present perfect');
    expect(selectOf('Present perfect').disabled()).toBe(true);
    gate.next();
    await settle();
    await harness.fixture.whenStable();
    expect(pageFakes().feedback.messages).toEqual(['Status changed to Ready']);
    expect(selectOf('Present perfect').disabled()).toBe(false);
  });

  it('drop on Ready, save fails → card moves, then returns to To prepare with the error message', async () => {
    const root = await open('/lesson-plans');
    const gate = pageFakes().plans.responder.hold('changeStatus');
    pageFakes().plans.responder.fail('changeStatus', 500);
    const lists = harness.fixture.debugElement
      .queryAll(By.directive(CdkDropList))
      .map((debug) => debug.injector.get(CdkDropList) as CdkDropList<LessonPlanStatus>);
    const source = lists.find((list) => list.data === 'ToPrepare')!;
    const target = lists.find((list) => list.data === 'Ready')!;
    const plan = pageFakes().plans.plans.find((candidate) => candidate.id === 'p1')!;
    target.dropped.emit({
      container: target,
      previousContainer: source,
      item: { data: plan },
    } as unknown as CdkDragDrop<LessonPlanStatus, LessonPlanStatus, LessonPlanResponse>);
    await harness.fixture.whenStable();
    const column = (status: string) => root.querySelector(`section[data-status="${status}"]`)!;
    expect(textOf(column('Ready'))).toContain('Present perfect');
    gate.next();
    await settle();
    await harness.fixture.whenStable();
    expect(textOf(column('ToPrepare'))).toContain('Present perfect');
    expect(textOf(column('Ready'))).not.toContain('Present perfect');
    expect(pageFakes().feedback.messages).toEqual(['Something went wrong. Try again.']);
  });

  it('delete confirmed → "Lesson plan deleted" and the row is gone', async () => {
    const root = await open('/lesson-plans');
    click(root, 'button.delete-lesson-plan');
    await settle();
    await harness.fixture.whenStable();
    expect(pageFakes().feedback.messages).toEqual(['Lesson plan deleted']);
    expect(textOf(root)).not.toContain('Present perfect');
  });
});
