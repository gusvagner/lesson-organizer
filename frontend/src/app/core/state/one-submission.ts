import { signal } from '@angular/core';

export class OneSubmission {
  private readonly locked = signal(false);
  readonly active = this.locked.asReadonly();

  async run(work: () => Promise<boolean>): Promise<void> {
    if (this.locked()) {
      return;
    }
    this.locked.set(true);
    let finished = false;
    try {
      finished = await work();
    } finally {
      if (!finished) {
        this.locked.set(false);
      }
    }
  }
}
