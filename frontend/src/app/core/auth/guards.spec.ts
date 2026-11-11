import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { authGuard } from './auth-guard';
import { AuthStore } from './auth-store';
import { guestGuard } from './guest-guard';
import { provideFakeAuth } from './testing/auth-testing';

@Component({ template: '' })
class Blank {}

describe('authGuard and guestGuard', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideFakeAuth(),
        provideRouter([
          { path: 'login', component: Blank, canActivate: [guestGuard] },
          { path: 'lesson-plans', component: Blank, canActivate: [authGuard] },
          { path: 'students', component: Blank, canActivate: [authGuard] },
        ]),
      ],
    });
  });

  it('guest on /students → /login?returnUrl=%2Fstudents', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/students');
    expect(TestBed.inject(Router).url).toBe('/login?returnUrl=%2Fstudents');
  });

  it('logged in on /students → route activates', async () => {
    await TestBed.inject(AuthStore).login('maria@example.com', 'Demo1234!');
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/students');
    expect(TestBed.inject(Router).url).toBe('/students');
  });

  it('logged in on /login → /lesson-plans', async () => {
    await TestBed.inject(AuthStore).login('maria@example.com', 'Demo1234!');
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/login');
    expect(TestBed.inject(Router).url).toBe('/lesson-plans');
  });

  it('guest on /login → stays', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/login');
    expect(TestBed.inject(Router).url).toBe('/login');
  });
});
