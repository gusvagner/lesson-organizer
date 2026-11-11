import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class PageReloader {
  load(path: string): void {
    window.location.assign(path);
  }
}
