import { PublicApi } from '../../../testing/public-api';
import { ConfirmDialogData } from '../confirm-dialog';
import { Confirmation } from '../confirmation';

export class FakeConfirmation implements PublicApi<Confirmation> {
  readonly asked: ConfirmDialogData[] = [];
  answer = true;

  ask(data: ConfirmDialogData): Promise<boolean> {
    this.asked.push(data);
    return Promise.resolve(this.answer);
  }
}
