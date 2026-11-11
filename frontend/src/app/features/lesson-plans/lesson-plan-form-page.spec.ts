import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideNativeDateAdapter } from '@angular/material/core';
import { Router, provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { chooseOption, click, query, settle, textOf, typeInto } from '../../testing/dom';
import { pageFakes, providePageFakes } from '../../testing/page-fakes';
import { aStudent } from '../students/testing/fake-students-api';
import { LessonPlanFormPage } from './lesson-plan-form-page';
import { aLessonPlan } from './testing/fake-lesson-plans-api';

@Component({ template: '' })
class Blank {}

describe('LessonPlanFormPage', () => {
  let harness: RouterTestingHarness;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        providePageFakes(),
        provideNativeDateAdapter(),
        provideRouter(
          [
            { path: 'lesson-plans', component: Blank },
            { path: 'lesson-plans/new', component: LessonPlanFormPage },
            { path: 'lesson-plans/:id/edit', component: LessonPlanFormPage },
            { path: 'students/new', component: Blank },
            { path: 'students/:id', component: Blank },
          ],
          withComponentInputBinding(),
        ),
      ],
    });
    pageFakes().students.students = [
      aStudent({ id: 's1', name: 'Ana' }),
      aStudent({ id: 's2', name: 'Bruno' }),
    ];
    pageFakes().plans.plans = [
      aLessonPlan({ id: 'p1', studentId: 's1', title: 'Review', status: 'Ready' }),
    ];
    harness = await RouterTestingHarness.create();
  });

  async function open(url: string): Promise<HTMLElement> {
    await harness.navigateByUrl(url);
    await settle();
    await harness.fixture.whenStable();
    return harness.routeNativeElement!;
  }

  async function fillAndSubmit(root: HTMLElement): Promise<void> {
    typeInto(root, 'input[formcontrolname=title]', 'Present perfect');
    typeInto(root, 'input[formcontrolname=lessonDate]', '11/12/2026');
    click(root, 'button[type=submit]');
    await settle();
    await harness.fixture.whenStable();
  }

  it('new with ?studentId=s2 → Bruno preselected', async () => {
    const root = await open('/lesson-plans/new?studentId=s2');
    expect(textOf(query(root, 'mat-select'))).toContain('Bruno');
  });

  it('no students → "Create a student first"', async () => {
    pageFakes().students.students = [];
    const root = await open('/lesson-plans/new');
    expect(textOf(root)).toContain('Create a student first');
    expect(root.querySelector('form')).toBeNull();
  });

  it('create → "Lesson plan saved", back to /lesson-plans', async () => {
    const root = await open('/lesson-plans/new');
    await chooseOption(root, 'mat-select', 'Ana');
    await fillAndSubmit(root);
    expect(pageFakes().feedback.messages).toEqual(['Lesson plan saved']);
    expect(pageFakes().plans.plans.at(-1)).toMatchObject({
      studentId: 's1',
      title: 'Present perfect',
      lessonDate: '2026-11-12',
      status: 'ToPrepare',
    });
    expect(TestBed.inject(Router).url).toBe('/lesson-plans');
  });

  it('create from a student → "Lesson plan saved" and back to that student', async () => {
    const root = await open('/lesson-plans/new?studentId=s2');
    await fillAndSubmit(root);
    expect(pageFakes().feedback.messages).toEqual(['Lesson plan saved']);
    expect(pageFakes().plans.plans.at(-1)?.studentId).toBe('s2');
    expect(TestBed.inject(Router).url).toBe('/students/s2');
  });

  it('edit → shows status, saves and goes back to /lesson-plans', async () => {
    const root = await open('/lesson-plans/p1/edit');
    expect(textOf(root)).toContain('Ready');
    typeInto(root, 'input[formcontrolname=title]', 'Review 2');
    click(root, 'button[type=submit]');
    await settle();
    await harness.fixture.whenStable();
    expect(pageFakes().plans.plans[0].title).toBe('Review 2');
    expect(TestBed.inject(Router).url).toBe('/lesson-plans');
  });

  it('edit unknown id → "Not found", navigates /lesson-plans', async () => {
    await open('/lesson-plans/missing/edit');
    expect(pageFakes().feedback.messages).toEqual(['Not found']);
    expect(TestBed.inject(Router).url).toBe('/lesson-plans');
  });
});
