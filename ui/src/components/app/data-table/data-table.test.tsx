import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { NextIntlClientProvider } from 'next-intl';
import { useState, type ReactElement } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { ApiError } from '@/lib/api/api-error';
import { commonEn } from '@/lib/i18n/messages/common.en';
import { DataTable, type DataTableColumn } from './data-table';

type Row = { id: string; name: string };

const columns: readonly DataTableColumn<Row>[] = [
  { id: 'name', header: 'Name', sortable: true, cell: (row) => row.name },
  { id: 'id', header: 'Id', cell: (row) => row.id },
];

const messages = { common: commonEn };

function withProviders(ui: ReactElement) {
  return render(
    <NextIntlClientProvider locale="en" messages={messages}>
      {ui}
    </NextIntlClientProvider>,
  );
}

function Paged() {
  const [page, setPage] = useState(1);
  const pageSize = 10;
  const rows = Array.from({ length: 25 }, (_, index) => ({
    id: String(index + 1),
    name: `row-${index + 1}`,
  }));
  const start = (page - 1) * pageSize;

  return (
    <DataTable
      columns={columns}
      rows={rows.slice(start, start + pageSize)}
      count={rows.length}
      page={page}
      pageSize={pageSize}
      orderBy=""
      onPageChange={setPage}
      onPageSizeChange={() => undefined}
      onOrderChange={() => undefined}
      onRetry={() => undefined}
      loading={false}
      error={null}
      empty={<p>Nothing here</p>}
    />
  );
}

function Sorted() {
  const [orderBy, setOrderBy] = useState('');
  const rows: Row[] = [
    { id: '1', name: 'B' },
    { id: '2', name: 'A' },
  ];
  const sorted = [...rows].sort((left, right) => {
    if (!orderBy) {
      return 0;
    }

    const [field, direction] = orderBy.split(' ');
    const result = left[field as 'name'].localeCompare(right[field as 'name']);

    return direction === 'desc' ? -result : result;
  });

  return (
    <DataTable
      columns={columns}
      rows={sorted}
      count={rows.length}
      page={1}
      pageSize={10}
      orderBy={orderBy}
      onPageChange={() => undefined}
      onPageSizeChange={() => undefined}
      onOrderChange={setOrderBy}
      onRetry={() => undefined}
      loading={false}
      error={null}
      empty={<p>Nothing here</p>}
    />
  );
}

describe('DataTable', () => {
  it('pages with Next and Last and moves the range', async () => {
    withProviders(<Paged />);

    expect(screen.getByText('row-1')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: commonEn.table.next }));

    expect(screen.getByText('row-11')).toBeInTheDocument();
    expect(screen.getByText('11–20 of 25')).toBeInTheDocument();

    await userEvent.click(screen.getByRole('button', { name: commonEn.table.last }));

    expect(screen.getByText('row-21')).toBeInTheDocument();
    expect(screen.getByText('21–25 of 25')).toBeInTheDocument();
  });

  it('cycles the sort header from ascending to descending to none', async () => {
    withProviders(<Sorted />);

    const header = screen.getAllByRole('columnheader')[0];
    expect(header).toHaveAttribute('aria-sort', 'none');
    expect(screen.getAllByRole('row')[1]).toHaveTextContent('B');

    await userEvent.click(within(header).getByRole('button', { name: commonEn.table.sortAsc }));

    expect(header).toHaveAttribute('aria-sort', 'ascending');
    expect(screen.getAllByRole('row')[1]).toHaveTextContent('A');

    await userEvent.click(within(header).getByRole('button', { name: commonEn.table.sortDesc }));

    expect(header).toHaveAttribute('aria-sort', 'descending');
    expect(screen.getAllByRole('row')[1]).toHaveTextContent('B');

    await userEvent.click(within(header).getByRole('button', { name: commonEn.table.sortNone }));

    expect(header).toHaveAttribute('aria-sort', 'none');
  });

  it('leaves a non-sortable header without a control', () => {
    withProviders(<Paged />);

    const header = screen.getAllByRole('columnheader')[1];
    expect(header).toHaveTextContent('Id');
    expect(within(header).queryByRole('button')).toBeNull();
  });

  it('shows the empty node at zero', () => {
    withProviders(
      <DataTable
        columns={columns}
        rows={[]}
        count={0}
        page={1}
        pageSize={20}
        orderBy=""
        onPageChange={() => undefined}
        onPageSizeChange={() => undefined}
        onOrderChange={() => undefined}
        onRetry={() => undefined}
        loading={false}
        error={null}
        empty={<p>Nothing here</p>}
      />,
    );

    expect(screen.getByText('Nothing here')).toBeInTheDocument();
  });

  it('shows the server message of an ApiError and retries', async () => {
    const retry = vi.fn();

    withProviders(
      <DataTable
        columns={columns}
        rows={[]}
        count={0}
        page={1}
        pageSize={20}
        orderBy=""
        onPageChange={() => undefined}
        onPageSizeChange={() => undefined}
        onOrderChange={() => undefined}
        onRetry={retry}
        loading={false}
        error={new ApiError(400, 'Table.Query.Invalid', 'This sort or filter is not valid.')}
        empty={<p>Nothing here</p>}
      />,
    );

    expect(screen.getByText('This sort or filter is not valid.')).toBeInTheDocument();
    await userEvent.click(screen.getByRole('button', { name: commonEn.actions.retry }));
    expect(retry).toHaveBeenCalled();
  });
});
