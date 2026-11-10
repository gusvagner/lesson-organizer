import { TestBed } from '@angular/core/testing';
import { StudentsApi } from './students-api';
import { StudentsStore } from './students-store';
import { FakeStudentsApi, aStudent } from './testing/fake-students-api';

describe('StudentsStore', () => {
  let store: StudentsStore;
  let api: FakeStudentsApi;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [{ provide: StudentsApi, useClass: FakeStudentsApi }],
    });
    api = TestBed.inject(StudentsApi) as unknown as FakeStudentsApi;
    api.students = [aStudent({ id: 's1', name: 'Ana' }), aStudent({ id: 's2', name: 'Bruno' })];
    store = TestBed.inject(StudentsStore);
  });

  it('load → sets students; loading true during, false after', async () => {
    const gate = api.responder.hold('list');
    const pending = store.load('');
    expect(store.loading()).toBe(true);
    gate.next();
    await pending;
    expect(store.loading()).toBe(false);
    expect(store.students().map((student) => student.name)).toEqual(['Ana', 'Bruno']);
  });

  it('load failure → keeps previous list, returns problem', async () => {
    await store.load('');
    api.responder.fail('list', 500);
    const outcome = await store.load('');
    expect(outcome.ok).toBe(false);
    expect(store.students()).toHaveLength(2);
  });

  it('two loads, older answers last → the newer result stays', async () => {
    const slow = api.responder.hold('list');
    const older = store.load('');
    const newer = store.load('bru');
    await newer;
    slow.next();
    await older;
    expect(store.students().map((student) => student.name)).toEqual(['Bruno']);
  });

  it('create → saving true during, false after, returns created', async () => {
    const gate = api.responder.hold('create');
    const pending = store.create({
      name: 'Carla',
      email: null,
      englishLevel: 'Advanced',
      notes: null,
    });
    expect(store.saving()).toBe(true);
    gate.next();
    const outcome = await pending;
    expect(store.saving()).toBe(false);
    expect(outcome.ok && outcome.value.name).toBe('Carla');
  });

  it('remove 409 → outcome with detail, list unchanged', async () => {
    api.students[0].lessonPlanCount = 2;
    await store.load('');
    const outcome = await store.remove('s1');
    expect(outcome.ok ? null : outcome.problem.status).toBe(409);
    expect(store.students()).toHaveLength(2);
  });

  it('remove → student removed from the list', async () => {
    await store.load('');
    await store.remove('s2');
    expect(store.students().map((student) => student.id)).toEqual(['s1']);
  });

  it('loadOne → sets selected; update → selected replaced', async () => {
    await store.loadOne('s1');
    expect(store.selected()?.name).toBe('Ana');
    await store.update('s1', {
      name: 'Ana Maria',
      email: null,
      englishLevel: 'Advanced',
      notes: null,
    });
    expect(store.selected()?.name).toBe('Ana Maria');
  });

  it('loadOne unknown → 404 outcome and selected cleared', async () => {
    await store.loadOne('s1');
    const outcome = await store.loadOne('missing');
    expect(outcome.ok ? null : outcome.problem.status).toBe(404);
    expect(store.selected()).toBeNull();
  });
});
