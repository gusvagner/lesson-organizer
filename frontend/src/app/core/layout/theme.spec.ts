import { DOCUMENT } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { BROWSER_STORAGE, SystemColorScheme, Theme, ThemeStorage } from './theme';

class FakeSystemColorScheme {
  dark = false;
  prefersDark(): boolean {
    return this.dark;
  }
}

class MemoryStorage {
  readonly items = new Map<string, string>();
  getItem(key: string): string | null {
    return this.items.get(key) ?? null;
  }
  setItem(key: string, value: string): void {
    this.items.set(key, value);
  }
}

class BlockedStorage {
  getItem(): string | null {
    throw new Error('SecurityError: storage is blocked');
  }
  setItem(): void {
    throw new Error('SecurityError: storage is blocked');
  }
}

describe('Theme', () => {
  function setUp(storage: object, systemDark = false) {
    const system = new FakeSystemColorScheme();
    system.dark = systemDark;
    TestBed.configureTestingModule({
      providers: [
        { provide: BROWSER_STORAGE, useValue: storage },
        { provide: SystemColorScheme, useValue: system },
      ],
    });
    TestBed.inject(DOCUMENT).documentElement.style.colorScheme = '';
    return TestBed.inject(Theme);
  }

  it('no stored mode, system dark → mode dark and page left on the system setting', () => {
    const theme = setUp(new MemoryStorage(), true);
    theme.apply();
    expect(theme.mode()).toBe('dark');
    expect(TestBed.inject(DOCUMENT).documentElement.style.colorScheme).toBe('');
  });

  it('stored "dark" → applied on start', () => {
    const storage = new MemoryStorage();
    storage.setItem('lo-theme', 'dark');
    const theme = setUp(storage);
    theme.apply();
    expect(TestBed.inject(DOCUMENT).documentElement.style.colorScheme).toBe('dark');
  });

  it('toggle from light → dark, color-scheme "dark", stored "dark"', () => {
    const storage = new MemoryStorage();
    const theme = setUp(storage);
    theme.toggle();
    expect(theme.mode()).toBe('dark');
    expect(TestBed.inject(DOCUMENT).documentElement.style.colorScheme).toBe('dark');
    expect(storage.getItem('lo-theme')).toBe('dark');
  });

  it('stored value not a mode → ignored', () => {
    const storage = new MemoryStorage();
    storage.setItem('lo-theme', 'purple');
    expect(setUp(storage, true).mode()).toBe('dark');
  });

  it('storage blocked → toggle still switches for the session', () => {
    const theme = setUp(new BlockedStorage());
    theme.toggle();
    expect(theme.mode()).toBe('dark');
    expect(TestBed.inject(ThemeStorage).read()).toBeNull();
  });
});
