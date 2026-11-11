import { filterFromQuery, isFiltered } from './lesson-plan-query';

const GUID = '01a1c6e4-b0ef-7355-873b-c8c3fed7c18d';

describe('filterFromQuery', () => {
  it('status=Done and studentId=abc → both null', () => {
    expect(filterFromQuery('abc', 'Done')).toEqual({ studentId: null, status: null });
  });

  it('valid status and guid → kept', () => {
    expect(filterFromQuery(GUID, 'Ready')).toEqual({ studentId: GUID, status: 'Ready' });
  });

  it('missing → both null', () => {
    expect(filterFromQuery(undefined, undefined)).toEqual({ studentId: null, status: null });
  });

  it('isFiltered → true when any filter is set', () => {
    expect(isFiltered({ studentId: null, status: null })).toBe(false);
    expect(isFiltered({ studentId: null, status: 'Ready' })).toBe(true);
  });
});
