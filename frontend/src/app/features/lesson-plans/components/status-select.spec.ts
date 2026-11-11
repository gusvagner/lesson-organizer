import { TestBed } from '@angular/core/testing';
import { chooseOption, textOf } from '../../../testing/dom';
import { LessonPlanStatus } from '../lesson-plan.model';
import { StatusSelect } from './status-select';

describe('StatusSelect', () => {
  async function render() {
    const fixture = TestBed.createComponent(StatusSelect);
    fixture.componentRef.setInput('status', 'ToPrepare');
    fixture.componentRef.setInput('label', 'Status of Review');
    const emitted: LessonPlanStatus[] = [];
    fixture.componentInstance.statusChange.subscribe((status) => emitted.push(status));
    await fixture.whenStable();
    return { fixture, root: fixture.nativeElement as HTMLElement, emitted };
  }

  it('option chosen → emitted, but shows the saved status until the input changes', async () => {
    const { fixture, root, emitted } = await render();
    await chooseOption(root, 'mat-select', 'Delivered');
    await fixture.whenStable();
    expect(emitted).toEqual(['Delivered']);
    expect(textOf(root)).toContain('To prepare');
  });

  it('status input changed after a successful save → shows the new status', async () => {
    const { fixture, root } = await render();
    await chooseOption(root, 'mat-select', 'Delivered');
    fixture.componentRef.setInput('status', 'Delivered');
    await fixture.whenStable();
    expect(textOf(root)).toContain('Delivered');
  });

  it('label input → aria-label names the plan', async () => {
    const { root } = await render();
    expect(root.querySelector('mat-select')?.getAttribute('aria-label')).toBe('Status of Review');
  });
});
