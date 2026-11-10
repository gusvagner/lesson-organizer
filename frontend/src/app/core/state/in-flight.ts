import { computed, signal } from '@angular/core';

export class InFlight {
  private readonly count = signal(0);
  readonly active = computed(() => this.count() > 0);

  async run<T>(work: () => Promise<T>): Promise<T> {
    this.count.update((count) => count + 1);
    try {
      return await work();
    } finally {
      this.count.update((count) => count - 1);
    }
  }
}

export class LatestOnly {
  private sequence = 0;

  next(): () => boolean {
    const ticket = ++this.sequence;
    return () => ticket === this.sequence;
  }
}
