import { OneSubmission } from './one-submission';

describe('OneSubmission', () => {
  it('second run while the first is pending → ignored', async () => {
    const submission = new OneSubmission();
    let runs = 0;
    let release = (_done: boolean) => {};
    const first = submission.run(() => {
      runs++;
      return new Promise<boolean>((resolve) => (release = resolve));
    });
    await submission.run(async () => {
      runs++;
      return true;
    });
    release(false);
    await first;
    expect(runs).toBe(1);
  });

  it('work finished → stays locked; work failed → unlocked', async () => {
    const finished = new OneSubmission();
    await finished.run(async () => true);
    expect(finished.active()).toBe(true);
    const failed = new OneSubmission();
    await failed.run(async () => false);
    expect(failed.active()).toBe(false);
  });
});
