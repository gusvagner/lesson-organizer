import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthStore } from '../auth/auth-store';
import { fakePageReloader, provideFakeAuth } from '../auth/testing/auth-testing';
import { click, settle, textOf } from '../../testing/dom';
import { Shell } from './shell';

describe('Shell', () => {
  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [provideFakeAuth(), provideRouter([]), provideHttpClient()],
    });
    await TestBed.inject(AuthStore).login('maria@example.com', 'Demo1234!');
  });

  it('shows the app links and the teacher name', async () => {
    const fixture = TestBed.createComponent(Shell);
    await fixture.whenStable();
    const text = textOf(fixture.nativeElement);
    expect(text).toContain('Lesson plans');
    expect(text).toContain('Students');
    expect(text).toContain('Maria');
  });

  it('theme button "Switch to dark mode" → click → "Switch to light mode"', async () => {
    const fixture = TestBed.createComponent(Shell);
    await fixture.whenStable();
    const root = fixture.nativeElement as HTMLElement;
    const before = root.querySelector('button.theme-toggle')?.getAttribute('aria-label');
    click(root, 'button.theme-toggle');
    await fixture.whenStable();
    const after = root.querySelector('button.theme-toggle')?.getAttribute('aria-label');
    expect(new Set([before, after])).toEqual(
      new Set(['Switch to dark mode', 'Switch to light mode']),
    );
    expect(after).not.toBe(before);
  });

  it('shows the app logo', async () => {
    const fixture = TestBed.createComponent(Shell);
    await fixture.whenStable();
    expect((fixture.nativeElement as HTMLElement).querySelector('lo-app-logo')).not.toBeNull();
  });

  it('Log out → full page load of /login', async () => {
    const fixture = TestBed.createComponent(Shell);
    await fixture.whenStable();
    click(fixture.nativeElement, 'button.user-menu');
    await fixture.whenStable();
    click(document.body, 'button.logout');
    await settle();
    expect(fakePageReloader().loaded).toEqual(['/login']);
  });
});
