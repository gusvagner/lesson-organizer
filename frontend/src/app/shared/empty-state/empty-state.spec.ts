import { TestBed } from '@angular/core/testing';
import { click, textOf } from '../../testing/dom';
import { EmptyState } from './empty-state';

describe('EmptyState', () => {
  it('message and action → action emitted on click', async () => {
    const fixture = TestBed.createComponent(EmptyState);
    fixture.componentRef.setInput('icon', 'search_off');
    fixture.componentRef.setInput('message', 'No results');
    fixture.componentRef.setInput('actionLabel', 'Clear filters');
    let clicked = 0;
    fixture.componentInstance.action.subscribe(() => clicked++);
    await fixture.whenStable();
    expect(textOf(fixture.nativeElement)).toContain('No results');
    click(fixture.nativeElement, 'button');
    expect(clicked).toBe(1);
  });

  it('no action label → no button', async () => {
    const fixture = TestBed.createComponent(EmptyState);
    fixture.componentRef.setInput('icon', 'school');
    fixture.componentRef.setInput('message', 'Nothing here');
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('button')).toBeNull();
  });
});
