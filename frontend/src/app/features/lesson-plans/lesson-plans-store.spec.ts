import { TestBed } from '@angular/core/testing';
import { LessonPlansApi } from './lesson-plans-api';
import { LessonPlansStore } from './lesson-plans-store';
import { FakeLessonPlansApi, aLessonPlan } from './testing/fake-lesson-plans-api';

describe('LessonPlansStore', () => {
  let store: LessonPlansStore;
  let api: FakeLessonPlansApi;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [{ provide: LessonPlansApi, useClass: FakeLessonPlansApi }],
    });
    api = TestBed.inject(LessonPlansApi) as unknown as FakeLessonPlansApi;
    api.plans = [
      aLessonPlan({ id: 'p1', title: 'Review', studentId: 's1' }),
      aLessonPlan({ id: 'p2', title: 'Phrasal verbs', studentId: 's2', status: 'Ready' }),
    ];
    store = TestBed.inject(LessonPlansStore);
  });

  it('load with filter → only matching plans; loading toggles', async () => {
    const gate = api.responder.hold('list');
    const pending = store.load({ studentId: 's2', status: null });
    expect(store.loading()).toBe(true);
    gate.next();
    await pending;
    expect(store.loading()).toBe(false);
    expect(store.plans().map((plan) => plan.id)).toEqual(['p2']);
  });

  it('changeStatus → replaces only that item, no reload', async () => {
    await store.load({ studentId: null, status: null });
    api.responder.calls.length = 0;
    await store.changeStatus('p1', 'Delivered');
    expect(store.plans().map((plan) => plan.status)).toEqual(['Delivered', 'Ready']);
    expect(api.responder.calls).toEqual(['changeStatus']);
  });

  it('changeStatus → saving toggles', async () => {
    await store.load({ studentId: null, status: null });
    const gate = api.responder.hold('changeStatus');
    const pending = store.changeStatus('p1', 'Ready');
    expect(store.saving()).toBe(true);
    gate.next();
    await pending;
    expect(store.saving()).toBe(false);
  });

  it('changeStatus failure → list unchanged, problem returned', async () => {
    await store.load({ studentId: null, status: null });
    api.responder.fail('changeStatus', 404);
    const outcome = await store.changeStatus('p1', 'Ready');
    expect(outcome.ok).toBe(false);
    expect(store.plans()[0].status).toBe('ToPrepare');
  });

  it('remove → removes the item from plans', async () => {
    await store.load({ studentId: null, status: null });
    await store.remove('p1');
    expect(store.plans().map((plan) => plan.id)).toEqual(['p2']);
  });

  it('loadOne → selected; unknown → 404 and selected cleared', async () => {
    await store.loadOne('p2');
    expect(store.selected()?.title).toBe('Phrasal verbs');
    const outcome = await store.loadOne('missing');
    expect(outcome.ok ? null : outcome.problem.status).toBe(404);
    expect(store.selected()).toBeNull();
  });

  it('create and update → saving toggles and values returned', async () => {
    const request = { studentId: 's1', title: 'New', description: null, lessonDate: '2026-11-12' };
    const created = await store.create(request);
    expect(created.ok && created.value.status).toBe('ToPrepare');
    const updated = await store.update('p1', { ...request, title: 'Renamed' });
    expect(updated.ok && updated.value.title).toBe('Renamed');
    expect(store.saving()).toBe(false);
  });
});
