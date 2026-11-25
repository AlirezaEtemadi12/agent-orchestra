import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { useTableQuery } from './use-table-query';

const navigation = vi.hoisted(() => {
  const push = vi.fn();
  const replace = vi.fn();
  let params = new URLSearchParams();

  return {
    push,
    replace,
    get params() {
      return params;
    },
    set params(value: URLSearchParams) {
      params = value;
    },
  };
});

vi.mock('next/navigation', () => ({
  useSearchParams: () => navigation.params,
  useRouter: () => ({ push: navigation.push, replace: navigation.replace }),
  usePathname: () => '/runs/',
}));

function Harness() {
  const query = useTableQuery({ defaultOrderBy: 'updatedAt desc', defaultPageSize: 20 });

  return (
    <div>
      <output data-testid="state">{JSON.stringify(query.state)}</output>
      <button type="button" onClick={() => query.setPage(3)}>
        set-page
      </button>
      <button type="button" onClick={() => query.setPageSize(100)}>
        set-size
      </button>
      <button type="button" onClick={() => query.setOrderBy('runKey')}>
        set-order
      </button>
      <button type="button" onClick={() => query.setSearch('t-web')}>
        set-search
      </button>
    </div>
  );
}

function state(): unknown {
  return JSON.parse(screen.getByTestId('state').textContent ?? '{}');
}

describe('useTableQuery', () => {
  beforeEach(() => {
    navigation.push.mockClear();
    navigation.replace.mockClear();
    navigation.params = new URLSearchParams();
  });

  it('reads the four keys out of the URL', () => {
    navigation.params = new URLSearchParams(
      'page=2&pageSize=50&orderBy=costUsd%20desc&q=web&project=demo',
    );

    render(<Harness />);

    expect(state()).toEqual({ page: 2, pageSize: 50, orderBy: 'costUsd desc', q: 'web' });
  });

  it('falls back on an invalid page or size and on a missing order', () => {
    navigation.params = new URLSearchParams('page=0&pageSize=7');

    render(<Harness />);

    expect(state()).toEqual({ page: 1, pageSize: 20, orderBy: 'updatedAt desc', q: '' });
  });

  it('pushes a new page and keeps the page filters', async () => {
    navigation.params = new URLSearchParams(
      'page=2&pageSize=50&orderBy=costUsd%20desc&q=web&project=demo',
    );

    render(<Harness />);
    await userEvent.click(screen.getByRole('button', { name: 'set-page' }));

    expect(navigation.push).toHaveBeenCalledWith(
      '/runs/?page=3&pageSize=50&orderBy=costUsd+desc&q=web&project=demo',
      { scroll: false },
    );
  });

  it('resets the page when the size, the order or the search changes', async () => {
    navigation.params = new URLSearchParams('page=3&pageSize=20&project=demo');

    render(<Harness />);
    await userEvent.click(screen.getByRole('button', { name: 'set-size' }));

    expect(navigation.push).toHaveBeenCalledWith('/runs/?pageSize=100&project=demo', {
      scroll: false,
    });
  });

  it('replaces instead of pushing for the search', async () => {
    navigation.params = new URLSearchParams('page=3&project=demo');

    render(<Harness />);
    await userEvent.click(screen.getByRole('button', { name: 'set-search' }));

    expect(navigation.replace).toHaveBeenCalledWith('/runs/?project=demo&q=t-web', {
      scroll: false,
    });
    expect(navigation.push).not.toHaveBeenCalled();
  });
});
