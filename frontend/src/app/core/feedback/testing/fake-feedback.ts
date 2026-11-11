import { PublicApi } from '../../../testing/public-api';
import { ApiProblem } from '../../http/api-problem';
import { Feedback, failureMessage } from '../feedback';

export class FakeFeedback implements PublicApi<Feedback> {
  readonly messages: string[] = [];

  success(message: string): void {
    this.messages.push(message);
  }

  failure(problem: ApiProblem): void {
    this.messages.push(failureMessage(problem));
  }
}
