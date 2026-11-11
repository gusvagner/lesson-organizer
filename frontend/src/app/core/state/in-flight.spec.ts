import { InFlight, LatestOnly } from './in-flight';

describe('InFlight', () => {
  it('two overlapping runs → active until both finish', async () => {
    const inFlight = new InFlight();
    let releaseFirst = () => {};
    let releaseSecond = () => {};
    const first = inFlight.run(() => new Promise<void>((resolve) => (releaseFirst = resolve)));
    const second = inFlight.run(() => new Promise<void>((resolve) => (releaseSecond = resolve)));
    expect(inFlight.active()).toBe(true);
    releaseFirst();
    await first;
    expect(inFlight.active()).toBe(true);
    releaseSecond();
    await second;
    expect(inFlight.active()).toBe(false);
  });

  it('work throws → inactive again and error propagates', async () => {
    const inFlight = new InFlight();
    await expect(inFlight.run(() => Promise.reject(new Error('boom')))).rejects.toThrow('boom');
    expect(inFlight.active()).toBe(false);
  });
});

describe('LatestOnly', () => {
  it('newer ticket issued → older ticket is stale', () => {
    const latest = new LatestOnly();
    const older = latest.next();
    const newer = latest.next();
    expect([older(), newer()]).toEqual([false, true]);
  });
});
