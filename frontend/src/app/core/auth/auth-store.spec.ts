import { TestBed } from '@angular/core/testing';
import { AuthStore } from './auth-store';
import { fakeAuthApi, fakePageReloader, provideFakeAuth } from './testing/auth-testing';

describe('AuthStore', () => {
  let store: AuthStore;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: provideFakeAuth() });
    store = TestBed.inject(AuthStore);
  });

  it('login → csrf, login, csrf in that order and sets the user', async () => {
    const outcome = await store.login('maria@example.com', 'Demo1234!');
    expect(outcome.ok).toBe(true);
    expect(fakeAuthApi().calls).toEqual(['csrf', 'login', 'csrf']);
    expect(store.currentUser()?.email).toBe('maria@example.com');
  });

  it('login 401 → outcome with detail, user stays null', async () => {
    fakeAuthApi().fail('login', 401, 'Invalid email or password.');
    const outcome = await store.login('maria@example.com', 'wrong');
    expect(outcome.ok ? null : outcome.problem.detail).toBe('Invalid email or password.');
    expect(store.isLoggedIn()).toBe(false);
  });

  it('register → csrf then register, no session started', async () => {
    const outcome = await store.register('Maria', 'maria@example.com', 'Demo1234!');
    expect(outcome.ok).toBe(true);
    expect(fakeAuthApi().calls).toEqual(['csrf', 'register']);
    expect(store.isLoggedIn()).toBe(false);
  });

  it('restoreSession → csrf then me and sets the user', async () => {
    await store.restoreSession();
    expect(fakeAuthApi().calls).toEqual(['csrf', 'me']);
    expect(store.currentUser()?.name).toBe('Maria');
  });

  it('restoreSession 401 → null user, no throw', async () => {
    fakeAuthApi().fail('me', 401);
    await store.restoreSession();
    expect(store.currentUser()).toBeNull();
  });

  it('logout → api logout then full page load of /login', async () => {
    await store.login('maria@example.com', 'Demo1234!');
    await store.logout();
    expect(fakeAuthApi().calls).toContain('logout');
    expect(fakePageReloader().loaded).toEqual(['/login']);
    expect(store.currentUser()).toBeNull();
  });

  it('logout api failure → still loads /login', async () => {
    fakeAuthApi().fail('logout', 500);
    await store.logout();
    expect(fakePageReloader().loaded).toEqual(['/login']);
  });

  it('login in flight → busy true, then false', async () => {
    const gate = fakeAuthApi().hold('login');
    const pending = store.login('maria@example.com', 'Demo1234!');
    await new Promise((resolve) => setTimeout(resolve));
    expect(store.busy()).toBe(true);
    gate.next();
    await pending;
    expect(store.busy()).toBe(false);
  });

  it('clear → removes the user', async () => {
    await store.login('maria@example.com', 'Demo1234!');
    store.clear();
    expect(store.isLoggedIn()).toBe(false);
  });
});
