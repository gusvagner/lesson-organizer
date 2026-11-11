import { Provider } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { AuthApi } from '../auth-api';
import { PageReloader } from '../page-reloader';
import { FakeAuthApi } from './fake-auth-api';
import { FakePageReloader } from './fake-page-reloader';

export function provideFakeAuth(): Provider[] {
  return [
    { provide: AuthApi, useClass: FakeAuthApi },
    { provide: PageReloader, useClass: FakePageReloader },
  ];
}

export function fakeAuthApi(): FakeAuthApi {
  return TestBed.inject(AuthApi) as unknown as FakeAuthApi;
}

export function fakePageReloader(): FakePageReloader {
  return TestBed.inject(PageReloader) as unknown as FakePageReloader;
}
