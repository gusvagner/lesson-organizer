import { PublicApi } from '../../../testing/public-api';
import { PageReloader } from '../page-reloader';

export class FakePageReloader implements PublicApi<PageReloader> {
  readonly loaded: string[] = [];

  load(path: string): void {
    this.loaded.push(path);
  }
}
