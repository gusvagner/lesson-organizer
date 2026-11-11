import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { chooseOption, click, settle, textOf, typeInto } from '../../testing/dom';
import { pageFakes, providePageFakes } from '../../testing/page-fakes';
import { By } from '@angular/platform-browser';
import { StudentForm } from './components/student-form';
import { StudentFormPage } from './student-form-page';
import { aStudent } from './testing/fake-students-api';

@Component({ template: '' })
class Blank {}

describe('StudentFormPage', () => {
  let harness: RouterTestingHarness;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        providePageFakes(),
        provideRouter(
          [
            { path: 'students', component: Blank },
            { path: 'students/new', component: StudentFormPage },
            { path: 'students/:id', component: Blank },
            { path: 'students/:id/edit', component: StudentFormPage },
          ],
          withComponentInputBinding(),
        ),
      ],
    });
    pageFakes().students.students = [aStudent({ id: 's1', name: 'Ana' })];
    harness = await RouterTestingHarness.create();
  });

  async function open(url: string): Promise<HTMLElement> {
    await harness.navigateByUrl(url);
    await settle();
    await harness.fixture.whenStable();
    return harness.routeNativeElement!;
  }

  async function submit(root: HTMLElement): Promise<void> {
    click(root, 'button[type=submit]');
    await settle();
    await harness.fixture.whenStable();
  }

  it('create → "Student saved" and the new student details', async () => {
    const root = await open('/students/new');
    typeInto(root, 'input[formcontrolname=name]', 'Carla');
    await chooseOption(root, 'mat-select', 'Advanced');
    await submit(root);
    expect(pageFakes().feedback.messages).toEqual(['Student saved']);
    expect(TestBed.inject(Router).url).toBe('/students/new-1');
  });

  it('edit → form starts with the student and saves changes', async () => {
    const root = await open('/students/s1/edit');
    expect(root.querySelector<HTMLInputElement>('input[formcontrolname=name]')!.value).toBe('Ana');
    typeInto(root, 'input[formcontrolname=name]', 'Ana Maria');
    await submit(root);
    expect(pageFakes().students.students[0].name).toBe('Ana Maria');
    expect(TestBed.inject(Router).url).toBe('/students/s1');
  });

  it('edit when loading fails with 500 → no form, generic message', async () => {
    pageFakes().students.responder.fail('get', 500);
    const root = await open('/students/s1/edit');
    expect(root.querySelector('form')).toBeNull();
    expect(pageFakes().feedback.messages).toEqual(['Something went wrong. Try again.']);
  });

  it('save emitted twice quickly → one request', async () => {
    await open('/students/s1/edit');
    const form = harness.fixture.debugElement.query(By.directive(StudentForm));
    const request = { name: 'Ana', email: null, englishLevel: 'Beginner' as const, notes: null };
    (form.componentInstance as StudentForm).save.emit(request);
    (form.componentInstance as StudentForm).save.emit(request);
    await settle();
    expect(pageFakes().students.responder.calls.filter((call) => call === 'update')).toHaveLength(
      1,
    );
  });

  it('edit with unknown id → "Not found" and /students', async () => {
    await open('/students/missing/edit');
    expect(pageFakes().feedback.messages).toEqual(['Not found']);
    expect(TestBed.inject(Router).url).toBe('/students');
  });

  it('400 → errors under the fields, stays on the page', async () => {
    const root = await open('/students/s1/edit');
    pageFakes().students.responder.fail('update', 400, { errors: { name: ['Name is required.'] } });
    await submit(root);
    expect(textOf(root)).toContain('Name is required.');
    expect(TestBed.inject(Router).url).toBe('/students/s1/edit');
  });
});
