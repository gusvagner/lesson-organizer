import { TestBed } from '@angular/core/testing';
import { click, settle, textOf } from '../../testing/dom';
import { Confirmation } from './confirmation';

describe('Confirmation', () => {
  const data = { title: 'Delete student?', message: 'Delete Ana?', confirmLabel: 'Delete' };

  it('confirm clicked → true', async () => {
    const answer = TestBed.inject(Confirmation).ask(data);
    await settle();
    expect(textOf(document.body)).toContain('Delete Ana?');
    click(document.body, 'button.confirm');
    expect(await answer).toBe(true);
  });

  it('cancel clicked → false', async () => {
    const answer = TestBed.inject(Confirmation).ask(data);
    await settle();
    click(document.body, 'button.cancel');
    expect(await answer).toBe(false);
  });
});
