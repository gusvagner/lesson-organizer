import { TestBed } from '@angular/core/testing';
import { provideNativeDateAdapter } from '@angular/material/core';
import { chooseOption, click, query, settle, textOf, typeInto } from '../../../testing/dom';
import { aStudent } from '../../students/testing/fake-students-api';
import { LessonPlanRequest, LessonPlanResponse } from '../lesson-plan.model';
import { aLessonPlan } from '../testing/fake-lesson-plans-api';
import { LessonPlanForm } from './lesson-plan-form';

const students = [aStudent({ id: 's1', name: 'Ana' }), aStudent({ id: 's2', name: 'Bruno' })];

describe('LessonPlanForm', () => {
  beforeEach(() => TestBed.configureTestingModule({ providers: [provideNativeDateAdapter()] }));

  async function render(
    plan: LessonPlanResponse | null,
    preselectedStudentId: string | null = null,
  ) {
    const fixture = TestBed.createComponent(LessonPlanForm);
    fixture.componentRef.setInput('plan', plan);
    fixture.componentRef.setInput('students', students);
    fixture.componentRef.setInput('preselectedStudentId', preselectedStudentId);
    const saved: LessonPlanRequest[] = [];
    fixture.componentInstance.save.subscribe((request) => saved.push(request));
    await fixture.whenStable();
    return { fixture, root: fixture.nativeElement as HTMLElement, saved };
  }

  async function submit(fixture: { whenStable(): Promise<unknown> }, root: HTMLElement) {
    click(root, 'button[type=submit]');
    await settle();
    await fixture.whenStable();
  }

  it('title "   " → "Title is required."', async () => {
    const { fixture, root, saved } = await render(aLessonPlan());
    typeInto(root, 'input[formcontrolname=title]', '   ');
    await submit(fixture, root);
    expect(textOf(root)).toContain('Title is required.');
    expect(saved).toEqual([]);
  });

  it('title 151 and description 5001 → length messages', async () => {
    const { fixture, root } = await render(aLessonPlan());
    typeInto(root, 'input[formcontrolname=title]', 'x'.repeat(151));
    typeInto(root, 'textarea[formcontrolname=description]', 'x'.repeat(5001));
    await fixture.whenStable();
    expect(textOf(root)).toContain('Title must be at most 150 characters.');
    expect(textOf(root)).toContain('Description must be at most 5000 characters.');
  });

  it('no date → "Lesson date is required."', async () => {
    const { fixture, root } = await render(null, 's1');
    typeInto(root, 'input[formcontrolname=title]', 'Review');
    await submit(fixture, root);
    expect(textOf(root)).toContain('Lesson date is required.');
  });

  it('preselectedStudentId → student select starts with it', async () => {
    const { root } = await render(null, 's2');
    expect(textOf(query(root, 'mat-select'))).toContain('Bruno');
  });

  it('date typed as 11/12/2026 → emits lessonDate "2026-11-12"', async () => {
    const { fixture, root, saved } = await render(null, 's1');
    typeInto(root, 'input[formcontrolname=title]', '  Present perfect  ');
    typeInto(root, 'input[formcontrolname=lessonDate]', '11/12/2026');
    await submit(fixture, root);
    expect(saved).toEqual([
      { studentId: 's1', title: 'Present perfect', description: null, lessonDate: '2026-11-12' },
    ]);
  });

  it('student chosen from the list → emitted', async () => {
    const { fixture, root, saved } = await render(aLessonPlan({ studentId: 's1' }));
    await chooseOption(root, 'mat-select', 'Bruno');
    await submit(fixture, root);
    expect(saved[0].studentId).toBe('s2');
  });

  it('edit → fields start with the plan; date "2026-11-12" shows as 11/12/2026', async () => {
    const { root } = await render(aLessonPlan({ title: 'Review', lessonDate: '2026-11-12' }));
    expect(query<HTMLInputElement>(root, 'input[formcontrolname=title]').value).toBe('Review');
    expect(query<HTMLInputElement>(root, 'input[formcontrolname=lessonDate]').value).toBe(
      '11/12/2026',
    );
  });

  it('server studentId error → "Student not found." under student', async () => {
    const { fixture, root } = await render(aLessonPlan());
    fixture.componentRef.setInput('problem', {
      status: 400,
      type: null,
      detail: null,
      fieldErrors: { studentId: ['Student not found.'] },
    });
    await fixture.whenStable();
    expect(textOf(query(root, 'mat-form-field.student-field'))).toContain('Student not found.');
  });

  it('saving → submit disabled', async () => {
    const { fixture, root } = await render(aLessonPlan());
    fixture.componentRef.setInput('saving', true);
    await fixture.whenStable();
    expect(query<HTMLButtonElement>(root, 'button[type=submit]').disabled).toBe(true);
  });
});
