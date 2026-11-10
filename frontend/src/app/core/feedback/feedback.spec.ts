import { ApiProblem } from '../http/api-problem';
import { failureMessage } from './feedback';

const problem = (status: number, detail: string | null): ApiProblem => ({
  status,
  type: null,
  detail,
  fieldErrors: {},
});

describe('failureMessage', () => {
  it('409 → server detail', () => {
    const detail =
      'This student has lesson plans. Delete them first or move them to another student.';
    expect(failureMessage(problem(409, detail))).toBe(detail);
  });

  it('404 → "Not found"', () => {
    expect(failureMessage(problem(404, 'Student not found.'))).toBe('Not found');
  });

  it('500 and 0 → generic message', () => {
    expect(failureMessage(problem(500, null))).toBe('Something went wrong. Try again.');
    expect(failureMessage(problem(0, null))).toBe('Something went wrong. Try again.');
  });

  it('400 without detail → "Please check the form."', () => {
    expect(failureMessage(problem(400, null))).toBe('Please check the form.');
  });
});
