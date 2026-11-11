import { TestBed } from '@angular/core/testing';
import { ApiProblem } from '../../../core/http/api-problem';
import { chooseOption, click, query, settle, textOf, typeInto } from '../../../testing/dom';
import { StudentRequest, StudentResponse } from '../student.model';
import { aStudent } from '../testing/fake-students-api';
import { StudentForm } from './student-form';

describe('StudentForm', () => {
  async function render(
    student: StudentResponse | null,
    saving = false,
    problem: ApiProblem | null = null,
  ) {
    const fixture = TestBed.createComponent(StudentForm);
    fixture.componentRef.setInput('student', student);
    fixture.componentRef.setInput('saving', saving);
    fixture.componentRef.setInput('problem', problem);
    const saved: StudentRequest[] = [];
    fixture.componentInstance.save.subscribe((request) => saved.push(request));
    await fixture.whenStable();
    return { fixture, root: fixture.nativeElement as HTMLElement, saved };
  }

  async function submit(fixture: { whenStable(): Promise<unknown> }, root: HTMLElement) {
    click(root, 'button[type=submit]');
    await settle();
    await fixture.whenStable();
  }

  it('name "   " → "Name is required." and nothing saved', async () => {
    const { fixture, root, saved } = await render(aStudent());
    typeInto(root, 'input[formcontrolname=name]', '   ');
    await submit(fixture, root);
    expect(textOf(root)).toContain('Name is required.');
    expect(saved).toEqual([]);
  });

  it('name 151 chars → "Name must be at most 150 characters."', async () => {
    const { fixture, root } = await render(aStudent());
    typeInto(root, 'input[formcontrolname=name]', 'x'.repeat(151));
    await fixture.whenStable();
    expect(textOf(root)).toContain('Name must be at most 150 characters.');
  });

  it('email "ana@" → "Email is not a valid email address."', async () => {
    const { fixture, root } = await render(aStudent());
    typeInto(root, 'input[formcontrolname=email]', 'ana@');
    await fixture.whenStable();
    expect(textOf(root)).toContain('Email is not a valid email address.');
  });

  it('notes 2001 chars → "Notes must be at most 2000 characters."', async () => {
    const { fixture, root } = await render(aStudent());
    typeInto(root, 'textarea[formcontrolname=notes]', 'x'.repeat(2001));
    await fixture.whenStable();
    expect(textOf(root)).toContain('Notes must be at most 2000 characters.');
  });

  it('blank email and notes → null for both, name trimmed', async () => {
    const { fixture, root, saved } = await render(
      aStudent({ email: 'ana@example.com', notes: 'x' }),
    );
    typeInto(root, 'input[formcontrolname=name]', '  Ana Maria  ');
    typeInto(root, 'input[formcontrolname=email]', '  ');
    typeInto(root, 'textarea[formcontrolname=notes]', '');
    await submit(fixture, root);
    expect(saved).toEqual([
      { name: 'Ana Maria', email: null, englishLevel: 'Intermediate', notes: null },
    ]);
  });

  it('new student without level → "English level is required."', async () => {
    const { fixture, root, saved } = await render(null);
    typeInto(root, 'input[formcontrolname=name]', 'Ana');
    await submit(fixture, root);
    expect(textOf(root)).toContain('English level is required.');
    expect(saved).toEqual([]);
  });

  it('new student with level chosen → saved', async () => {
    const { fixture, root, saved } = await render(null);
    typeInto(root, 'input[formcontrolname=name]', 'Ana');
    await chooseOption(root, 'mat-select', 'Upper intermediate');
    await submit(fixture, root);
    expect(saved).toEqual([
      { name: 'Ana', email: null, englishLevel: 'UpperIntermediate', notes: null },
    ]);
  });

  it('saving → submit disabled', async () => {
    const { root } = await render(aStudent(), true);
    expect(query<HTMLButtonElement>(root, 'button[type=submit]').disabled).toBe(true);
  });

  it('problem with email field error → message under email', async () => {
    const problem = {
      status: 400,
      type: null,
      detail: null,
      fieldErrors: { email: ['Email is not a valid email address.'] },
    };
    const { fixture, root } = await render(aStudent());
    fixture.componentRef.setInput('problem', problem);
    await fixture.whenStable();
    expect(textOf(query(root, 'mat-form-field.email-field'))).toContain(
      'Email is not a valid email address.',
    );
  });
});
