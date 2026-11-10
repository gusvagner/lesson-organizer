import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter, withComponentInputBinding } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { fakeAuthApi, provideFakeAuth } from '../../core/auth/testing/auth-testing';
import { click, settle, textOf, typeInto } from '../../testing/dom';
import { RegisterPage } from './register-page';

@Component({ template: '' })
class Blank {}

describe('RegisterPage', () => {
  let harness: RouterTestingHarness;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideFakeAuth(),
        provideRouter(
          [
            { path: 'register', component: RegisterPage },
            { path: 'login', component: Blank },
          ],
          withComponentInputBinding(),
        ),
      ],
    });
    harness = await RouterTestingHarness.create();
  });

  async function register(password: string): Promise<HTMLElement> {
    await harness.navigateByUrl('/register');
    const root = harness.routeNativeElement!;
    typeInto(root, 'input[formcontrolname=name]', 'Maria');
    typeInto(root, 'input[formcontrolname=email]', 'maria@example.com');
    typeInto(root, 'input[formcontrolname=password]', password);
    click(root, 'button[type=submit]');
    await settle();
    await harness.fixture.whenStable();
    return root;
  }

  it('valid → navigates to /login?registered=1', async () => {
    await register('Demo1234!');
    expect(TestBed.inject(Router).url).toBe('/login?registered=1');
  });

  it('409 → "An account with this email already exists." under email', async () => {
    fakeAuthApi().fail('register', 409, 'An account with this email already exists.');
    const root = await register('Demo1234!');
    expect(textOf(root.querySelector('mat-form-field.email-field')!)).toContain(
      'An account with this email already exists.',
    );
    expect(TestBed.inject(Router).url).toBe('/register');
  });

  it('password "short" → "Password must be at least 8 characters." and no request', async () => {
    const root = await register('short');
    expect(textOf(root)).toContain('Password must be at least 8 characters.');
    expect(fakeAuthApi().calls).toEqual([]);
  });

  it('name "   " → "Name is required."', async () => {
    await harness.navigateByUrl('/register');
    const root = harness.routeNativeElement!;
    typeInto(root, 'input[formcontrolname=name]', '   ');
    await harness.fixture.whenStable();
    expect(textOf(root)).toContain('Name is required.');
  });
});
