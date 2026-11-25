import { describe, expect, it } from 'vitest';
import { buildGridifyQuery } from './build-query';

describe('buildGridifyQuery', () => {
  it('carries the page, the size and one order key', () => {
    expect(
      buildGridifyQuery({ page: 2, pageSize: 50, orderBy: 'costUsd desc' }),
    ).toEqual({ page: 2, pageSize: 50, orderBy: 'costUsd desc', filter: '' });
  });

  it('turns the search into one case-insensitive group', () => {
    expect(
      buildGridifyQuery({
        page: 1,
        pageSize: 20,
        orderBy: '',
        search: 'T-WEB',
        searchableFields: ['taskId', 'planTitle'],
      }),
    ).toEqual({
      page: 1,
      pageSize: 20,
      orderBy: '',
      filter: '(taskId=*T-WEB/i|planTitle=*T-WEB/i)',
    });
  });

  it('joins exact filters with AND and keeps them case-sensitive', () => {
    expect(
      buildGridifyQuery({
        page: 1,
        pageSize: 20,
        orderBy: 'updatedAt desc',
        search: 'web',
        searchableFields: ['taskId'],
        filters: [
          { field: 'status', value: 'Verified' },
          { field: 'repo', value: 'Web' },
        ],
      }).filter,
    ).toBe('(taskId=*web/i),status=Verified,repo=Web');
  });

  it('escapes the characters the Gridify syntax uses', () => {
    expect(
      buildGridifyQuery({
        page: 1,
        pageSize: 20,
        orderBy: '',
        search: 'a(b),c|d',
        searchableFields: ['task'],
      }).filter,
    ).toBe('(task=*a\\(b\\)\\,c\\|d/i)');
  });

  it('leaves the filter empty when there is no search and no filter', () => {
    expect(buildGridifyQuery({ page: 1, pageSize: 20, orderBy: '' }).filter).toBe('');
  });
});
