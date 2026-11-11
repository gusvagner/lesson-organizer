import { TestBed } from '@angular/core/testing';
import { AppLogo } from './app-logo';

describe('AppLogo', () => {
  it('renders → svg with role img and label "Lesson Organizer"', async () => {
    const fixture = TestBed.createComponent(AppLogo);
    await fixture.whenStable();
    const svg = (fixture.nativeElement as HTMLElement).querySelector('svg');
    expect(svg?.getAttribute('role')).toBe('img');
    expect(svg?.getAttribute('aria-label')).toBe('Lesson Organizer');
  });

  it('size 48 → width and height 48', async () => {
    const fixture = TestBed.createComponent(AppLogo);
    fixture.componentRef.setInput('size', 48);
    await fixture.whenStable();
    const svg = (fixture.nativeElement as HTMLElement).querySelector('svg');
    expect([svg?.getAttribute('width'), svg?.getAttribute('height')]).toEqual(['48', '48']);
  });
});
