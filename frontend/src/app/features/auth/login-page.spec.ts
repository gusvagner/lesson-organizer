import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { fakeAuthApi, provideFakeAuth } from '../../core/auth/testing/auth-testing';
import { click, query, settle, textOf, typeInto } from '../../testing/dom';
import { LoginPage } from './login-page';

@Component({ template: '' })
class Blank {}

describe('LoginPage', () => {
  let harness: RouterTestingHarness;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideFakeAuth(),
        provideRouter(
          [
            { path: 'login', component: LoginPage },
            { path: 'students/:id', component: Blank },
            { path: 'lesson-plans', component: Blank },
          ],
          withComponentInputBinding(),
        ),
      ],
    });
    harness = await RouterTestingHarness.create();
  });

  async function logIn(url: string): Promise<HTMLElement> {
    await harness.navigateByUrl(url);
    const root = harness.routeNativeElement!;
    typeInto(root, 'input[formcontrolname=email]', 'maria@example.com');
    typeInto(root, 'input[formcontrolname=password]', 'Demo1234!');
    click(root, 'button[type=submit]');
    await settle();
    await harness.fixture.whenStable();
    return root;
  }

  it('valid login with returnUrl=/students/1 → navigates to /students/1', async () => {
    await logIn('/login?returnUrl=%2Fstudents%2F1');
    expect(TestBed.inject(Router).url).toBe('/students/1');
  });

  it('valid login with returnUrl=//evil.example → navigates to /lesson-plans', async () => {
    await logIn('/login?returnUrl=%2F%2Fevil.example');
    expect(TestBed.inject(Router).url).toBe('/lesson-plans');
  });

  it('401 → shows "Invalid email or password." and stays', async () => {
    fakeAuthApi().fail('login', 401, 'Invalid email or password.');
    const root = await logIn('/login');
    expect(textOf(root)).toContain('Invalid email or password.');
    expect(TestBed.inject(Router).url).toBe('/login');
  });

  it('429 → shows "Too many attempts. Try again in a minute."', async () => {
    fakeAuthApi().fail('login', 429);
    const root = await logIn('/login');
    expect(textOf(root)).toContain('Too many attempts. Try again in a minute.');
  });

  it('empty form submitted → field messages, no request', async () => {
    await harness.navigateByUrl('/login');
    const root = harness.routeNativeElement!;
    click(root, 'button[type=submit]');
    await harness.fixture.whenStable();
    expect(textOf(root)).toContain('Email is required.');
    expect(fakeAuthApi().calls).toEqual([]);
  });

  it('login in flight → submit disabled', async () => {
    fakeAuthApi().hold('login');
    const root = await logIn('/login');
    expect(query<HTMLButtonElement>(root, 'button[type=submit]').disabled).toBe(true);
  });

  it('registered=1 → shows "Account created. Log in to continue."', async () => {
    await harness.navigateByUrl('/login?registered=1');
    expect(textOf(harness.routeNativeElement!)).toContain('Account created. Log in to continue.');
  });
});
