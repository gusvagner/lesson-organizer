import { TestBed } from '@angular/core/testing';
import { Router, provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { click, settle, textOf, typeInto } from '../../testing/dom';
import { pageFakes, providePageFakes } from '../../testing/page-fakes';
import { StudentListPage } from './student-list-page';
import { HAS_LESSON_PLANS, aStudent } from './testing/fake-students-api';

describe('StudentListPage', () => {
  let harness: RouterTestingHarness;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        providePageFakes(),
        provideRouter(
          [{ path: 'students', component: StudentListPage }],
          withComponentInputBinding(),
        ),
      ],
    });
    pageFakes().students.students = [
      aStudent({ id: 's1', name: 'Ana', email: 'ana@example.com' }),
      aStudent({ id: 's2', name: 'Bruno', englishLevel: 'UpperIntermediate' }),
    ];
    harness = await RouterTestingHarness.create();
  });

  async function open(url: string): Promise<HTMLElement> {
    await harness.navigateByUrl(url);
    await settle();
    await harness.fixture.whenStable();
    return harness.routeNativeElement!;
  }

  it('lists name, email and level', async () => {
    const text = textOf(await open('/students'));
    expect(text).toContain('Ana');
    expect(text).toContain('ana@example.com');
    expect(text).toContain('Upper intermediate');
  });

  it('?search=bru → loads with that search and the box shows it', async () => {
    const root = await open('/students?search=bru');
    expect(textOf(root)).not.toContain('Ana');
    expect(root.querySelector<HTMLInputElement>('input.search')!.value).toBe('bru');
  });

  it('typing → search query param updated after the debounce', async () => {
    const root = await open('/students');
    typeInto(root, 'input.search', 'ana');
    await settle(350);
    expect(TestBed.inject(Router).url).toBe('/students?search=ana');
  });

  it('no students and no search → "Add your first student"', async () => {
    pageFakes().students.students = [];
    expect(textOf(await open('/students'))).toContain('Add your first student');
  });

  it('search with no match → "No results"; Clear filters resets the search', async () => {
    const root = await open('/students?search=zzz');
    expect(textOf(root)).toContain('No results');
    click(root, 'lo-empty-state button');
    await settle();
    await harness.fixture.whenStable();
    expect(TestBed.inject(Router).url).toBe('/students');
  });

  it('delete confirmed with 409 → server message, list unchanged', async () => {
    pageFakes().students.students[0].lessonPlanCount = 1;
    const root = await open('/students');
    click(root, 'button.delete-student');
    await settle();
    await harness.fixture.whenStable();
    expect(pageFakes().feedback.messages).toEqual([HAS_LESSON_PLANS]);
    expect(textOf(root)).toContain('Ana');
  });

  it('delete confirmed → "Student deleted" and the list reloads', async () => {
    const root = await open('/students');
    click(root, 'button.delete-student');
    await settle();
    await harness.fixture.whenStable();
    expect(pageFakes().confirmation.asked[0].message).toContain('Ana');
    expect(pageFakes().feedback.messages).toEqual(['Student deleted']);
    expect(textOf(root)).not.toContain('ana@example.com');
  });

  it('pause after "Mary " then keep typing → box keeps the space', async () => {
    const root = await open('/students');
    typeInto(root, 'input.search', 'Mary ');
    await settle(350);
    await harness.fixture.whenStable();
    expect(root.querySelector<HTMLInputElement>('input.search')!.value).toBe('Mary ');
  });

  it('search, Clear filters, same search again → search applied again', async () => {
    const root = await open('/students');
    typeInto(root, 'input.search', 'zzz');
    await settle(350);
    await harness.fixture.whenStable();
    click(root, 'lo-empty-state button');
    await settle();
    await harness.fixture.whenStable();
    typeInto(root, 'input.search', 'zzz');
    await settle(350);
    expect(TestBed.inject(Router).url).toBe('/students?search=zzz');
  });

  it('delete cancelled → nothing removed', async () => {
    pageFakes().confirmation.answer = false;
    const root = await open('/students');
    click(root, 'button.delete-student');
    await settle();
    expect(pageFakes().students.responder.calls).not.toContain('remove');
  });
});
