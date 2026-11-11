import { safeReturnUrl } from './safe-return-url';

describe('safeReturnUrl', () => {
  it.each([
    ['/students', '/students'],
    ['/students/1?tab=plans', '/students/1?tab=plans'],
    ['//evil.example', '/lesson-plans'],
    ['https://evil.example', '/lesson-plans'],
    ['/\\evil.example', '/lesson-plans'],
    ['javascript:alert(1)', '/lesson-plans'],
    ['', '/lesson-plans'],
    [null, '/lesson-plans'],
  ])('%s → %s', (input, expected) => {
    expect(safeReturnUrl(input)).toBe(expected);
  });
});
