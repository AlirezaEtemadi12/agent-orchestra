import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import { NextIntlClientProvider } from 'next-intl';
import type { ReactElement } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '@/lib/api/api-error';
import { commonEn } from '@/lib/i18n/messages/common.en';
import { runsApi } from './api';
import { RunsList } from './components/runs-list';
import { runsEn } from './i18n/en';
import type { RunListItem, RunsSummary } from './types';

const navigation = vi.hoisted(() => ({
  push: vi.fn(),
  replace: vi.fn(),
}));

const projects = vi.hoisted(() => ({
  data: [] as { id: string; name: string }[],
}));

vi.mock('next/navigation', () => ({
  useSearchParams: () => new URLSearchParams(),
  useRouter: () => ({ push: navigation.push, replace: navigation.replace }),
  usePathname: () => '/',
  redirect: vi.fn(),
  permanentRedirect: vi.fn(),
}));

vi.mock('@/lib/projects/queries', () => ({
  useProjects: () => ({ data: projects.data }),
}));

vi.mock('./api', () => ({
  runsApi: {
    table: vi.fn(),
    summary: vi.fn(),
    detail: vi.fn(),
  },
}));

const messages = { runs: runsEn, common: commonEn };

function withProviders(ui: ReactElement) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });

  return render(
    <NextIntlClientProvider locale="en" messages={messages}>
      <QueryClientProvider client={client}>{ui}</QueryClientProvider>
    </NextIntlClientProvider>,
  );
}

function run(overrides: Partial<RunListItem> = {}): RunListItem {
  return {
    projectId: 'demo',
    projectName: 'Demo',
    runKey: 'T-DEMO-001.1',
    taskId: 'T-DEMO-001',
    repo: 'Ui',
    status: 'Verified',
    planNumber: '0001',
    planTitle: 'Plan',
    task: 'Task',
    branch: 'plan-0001',
    sessions: 1,
    calls: 2,
    requests: 3,
    costUsd: 0.5,
    rounds: 1,
    models: ['model'],
    updatedAt: '2026-11-25T00:00:00Z',
    closedAt: null,
    source: 'Ledger',
    ...overrides,
  };
}

function summary(overrides: Partial<RunsSummary> = {}): RunsSummary {
  return {
    runs: 0,
    sessions: 0,
    calls: 0,
    costUsd: 0,
    statuses: [],
    repos: [],
    ...overrides,
  };
}

describe('RunsList', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    projects.data = [];
  });

  it('reads the cards and the dropdown options from the summary, not the page rows', async () => {
    projects.data = [{ id: 'demo', name: 'Demo' }];
    vi.mocked(runsApi.summary).mockResolvedValue(
      summary({
        runs: 9,
        sessions: 12,
        calls: 15,
        costUsd: 1.25,
        statuses: ['SummaryStatus'],
        repos: ['SummaryRepo'],
      }),
    );
    vi.mocked(runsApi.table).mockResolvedValue({
      count: 1,
      data: [run({ status: 'RowStatus', repo: 'RowRepo' })],
    });

    withProviders(<RunsList />);

    expect(await screen.findByText('9')).toBeInTheDocument();
    expect(await screen.findByRole('option', { name: 'SummaryStatus' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'SummaryRepo' })).toBeInTheDocument();
    expect(screen.queryByRole('option', { name: 'RowStatus' })).toBeNull();
    expect(screen.queryByRole('option', { name: 'RowRepo' })).toBeNull();
  });

  it('shows one error state when either query fails', async () => {
    projects.data = [{ id: 'demo', name: 'Demo' }];
    vi.mocked(runsApi.summary).mockResolvedValue(summary());
    vi.mocked(runsApi.table).mockRejectedValue(
      new ApiError(400, 'Table.Query.Invalid', 'This sort or filter is not valid.'),
    );

    withProviders(<RunsList />);

    expect(await screen.findByText('This sort or filter is not valid.')).toBeInTheDocument();
    expect(
      screen.getByRole('button', { name: commonEn.actions.retry }),
    ).toBeInTheDocument();
  });

  it('keeps the start link in the empty state when there are no projects', async () => {
    vi.mocked(runsApi.summary).mockResolvedValue(summary());
    vi.mocked(runsApi.table).mockResolvedValue({ count: 0, data: [] });

    withProviders(<RunsList />);

    expect(await screen.findByText(runsEn.empty)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: runsEn.noProjects })).toBeInTheDocument();
  });
});
