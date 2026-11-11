import { CdkDrag, CdkDragDrop, CdkDropList } from '@angular/cdk/drag-drop';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { textOf } from '../../../testing/dom';
import { LessonPlanResponse, LessonPlanStatus } from '../lesson-plan.model';
import { aLessonPlan } from '../testing/fake-lesson-plans-api';
import { LessonPlanBoard } from './lesson-plan-board';
import { StatusChange } from './lesson-plan-events';
import { StatusSelect } from './status-select';

const plans = [
  aLessonPlan({ id: 'p1', title: 'Later', lessonDate: '2026-11-20', status: 'ToPrepare' }),
  aLessonPlan({ id: 'p2', title: 'Earlier', lessonDate: '2026-11-10', status: 'ToPrepare' }),
  aLessonPlan({ id: 'p3', title: 'Ready one', lessonDate: '2026-11-12', status: 'Ready' }),
];

describe('LessonPlanBoard', () => {
  beforeEach(() => TestBed.configureTestingModule({ providers: [provideRouter([])] }));

  async function render(statuses?: LessonPlanStatus[], saving = false) {
    const fixture = TestBed.createComponent(LessonPlanBoard);
    fixture.componentRef.setInput('plans', plans);
    fixture.componentRef.setInput('saving', saving);
    if (statuses) {
      fixture.componentRef.setInput('statuses', statuses);
    }
    const changes: StatusChange[] = [];
    fixture.componentInstance.statusChange.subscribe((change) => changes.push(change));
    await fixture.whenStable();
    return { fixture, root: fixture.nativeElement as HTMLElement, changes };
  }

  function column(root: HTMLElement, status: LessonPlanStatus): HTMLElement {
    return root.querySelector<HTMLElement>(`section[data-status="${status}"]`)!;
  }

  function drop(
    lists: CdkDropList<LessonPlanStatus>[],
    from: LessonPlanStatus,
    to: LessonPlanStatus,
    plan: LessonPlanResponse,
  ): void {
    const source = lists.find((list) => list.data === from)!;
    const target = lists.find((list) => list.data === to)!;
    target.dropped.emit({
      container: target,
      previousContainer: source,
      item: { data: plan },
    } as unknown as CdkDragDrop<LessonPlanStatus, LessonPlanStatus, LessonPlanResponse>);
  }

  it('plans → each in its status column, sorted by date, with counts', async () => {
    const { root } = await render();
    expect(textOf(column(root, 'ToPrepare').querySelector('h2')!)).toBe('To prepare');
    const titles = Array.from(column(root, 'ToPrepare').querySelectorAll('.card-title')).map(
      textOf,
    );
    expect(titles).toEqual(['Earlier', 'Later']);
    expect(textOf(column(root, 'ToPrepare').querySelector('.count')!)).toBe('2');
    expect(textOf(column(root, 'Ready'))).toContain('Ready one');
  });

  it('drop from To prepare to Ready → statusChange { id, status: "Ready" }', async () => {
    const { fixture, changes } = await render();
    const lists = fixture.debugElement
      .queryAll(By.directive(CdkDropList))
      .map((d) => d.injector.get(CdkDropList));
    drop(lists, 'ToPrepare', 'Ready', plans[1]);
    expect(changes).toEqual([{ id: 'p2', status: 'Ready' }]);
  });

  it('drop in the same column → nothing emitted', async () => {
    const { fixture, changes } = await render();
    const lists = fixture.debugElement
      .queryAll(By.directive(CdkDropList))
      .map((d) => d.injector.get(CdkDropList));
    drop(lists, 'Ready', 'Ready', plans[2]);
    expect(changes).toEqual([]);
  });

  it('saving → drag disabled on every card', async () => {
    const { fixture } = await render(undefined, true);
    const drags = fixture.debugElement
      .queryAll(By.directive(CdkDrag))
      .map((d) => d.injector.get(CdkDrag));
    expect(drags.length).toBe(3);
    expect(drags.every((drag) => drag.disabled)).toBe(true);
  });

  it('statuses ["Ready"] → one column', async () => {
    const { root } = await render(['Ready']);
    expect(root.querySelectorAll('section[data-status]').length).toBe(1);
    expect(column(root, 'Ready')).not.toBeNull();
  });

  it('empty column → "No lesson plans"', async () => {
    const { root } = await render();
    expect(textOf(column(root, 'Delivered'))).toContain('No lesson plans');
  });

  it('card status select → statusChange', async () => {
    const { fixture, changes } = await render();
    const select = fixture.debugElement.query(By.directive(StatusSelect))
      .componentInstance as StatusSelect;
    select.statusChange.emit('Delivered');
    expect(changes).toEqual([{ id: 'p2', status: 'Delivered' }]);
  });
});
