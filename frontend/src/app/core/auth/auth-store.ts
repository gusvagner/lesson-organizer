import { Injectable, computed, inject, signal } from '@angular/core';
import { Outcome, toOutcome } from '../http/outcome';
import { AuthApi } from './auth-api';
import { PageReloader } from './page-reloader';
import { User } from './user.model';

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly api = inject(AuthApi);
  private readonly reloader = inject(PageReloader);
  private readonly user = signal<User | null>(null);
  private readonly pending = signal(false);

  readonly currentUser = this.user.asReadonly();
  readonly isLoggedIn = computed(() => this.user() !== null);
  readonly busy = this.pending.asReadonly();

  async restoreSession(): Promise<void> {
    await toOutcome(this.api.csrf());
    const me = await toOutcome(this.api.me());
    this.user.set(me.ok ? me.value : null);
  }

  login(email: string, password: string): Promise<Outcome<User>> {
    return this.whileBusy(async () => {
      await toOutcome(this.api.csrf());
      const outcome = await toOutcome(this.api.login({ email, password }));
      if (outcome.ok) {
        this.user.set(outcome.value);
        await toOutcome(this.api.csrf());
      }
      return outcome;
    });
  }

  register(name: string, email: string, password: string): Promise<Outcome<User>> {
    return this.whileBusy(async () => {
      await toOutcome(this.api.csrf());
      return toOutcome(this.api.register({ name, email, password }));
    });
  }

  async logout(): Promise<void> {
    await toOutcome(this.api.logout());
    this.user.set(null);
    this.reloader.load('/login');
  }

  clear(): void {
    this.user.set(null);
  }

  private async whileBusy<T>(work: () => Promise<T>): Promise<T> {
    this.pending.set(true);
    try {
      return await work();
    } finally {
      this.pending.set(false);
    }
  }
}
