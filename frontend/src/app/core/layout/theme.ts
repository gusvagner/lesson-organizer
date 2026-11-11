import { DOCUMENT, Injectable, InjectionToken, computed, inject, signal } from '@angular/core';

export type ThemeMode = 'light' | 'dark';

const STORAGE_KEY = 'lo-theme';

interface KeyValueStorage {
  getItem(key: string): string | null;
  setItem(key: string, value: string): void;
}

export const BROWSER_STORAGE = new InjectionToken<KeyValueStorage | null>('BROWSER_STORAGE', {
  providedIn: 'root',
  factory: () => {
    try {
      return inject(DOCUMENT).defaultView?.localStorage ?? null;
    } catch {
      return null;
    }
  },
});

@Injectable({ providedIn: 'root' })
export class ThemeStorage {
  private readonly storage = inject(BROWSER_STORAGE);

  read(): ThemeMode | null {
    try {
      const value = this.storage?.getItem(STORAGE_KEY) ?? null;
      return value === 'light' || value === 'dark' ? value : null;
    } catch {
      return null;
    }
  }

  write(mode: ThemeMode): void {
    try {
      this.storage?.setItem(STORAGE_KEY, mode);
    } catch {
      return;
    }
  }
}

@Injectable({ providedIn: 'root' })
export class SystemColorScheme {
  private readonly window = inject(DOCUMENT).defaultView;

  prefersDark(): boolean {
    return this.window?.matchMedia?.('(prefers-color-scheme: dark)').matches ?? false;
  }
}

@Injectable({ providedIn: 'root' })
export class Theme {
  private readonly storage = inject(ThemeStorage);
  private readonly system = inject(SystemColorScheme);
  private readonly document = inject(DOCUMENT);
  private readonly chosen = signal<ThemeMode | null>(this.storage.read());

  readonly mode = computed<ThemeMode>(
    () => this.chosen() ?? (this.system.prefersDark() ? 'dark' : 'light'),
  );

  apply(): void {
    this.document.documentElement.style.colorScheme = this.chosen() ?? '';
  }

  toggle(): void {
    const next: ThemeMode = this.mode() === 'dark' ? 'light' : 'dark';
    this.chosen.set(next);
    this.storage.write(next);
    this.apply();
  }
}
