import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import { NextIntlClientProvider } from 'next-intl';
import type { ReactElement } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { commonEn } from '@/lib/i18n/messages/common.en';
import { importsApi } from './api';
import { ImportLog } from './components/import-log';
import { importsEn } from './i18n/en';
import type { ImportLogDto } from './types';

const navigation = vi.hoisted(() => ({
  push: vi.fn(),
  replace: vi.fn(),
}));

vi.mock('next/navigation', () => ({
  useSearchParams: () => new URLSearchParams(),
  useRouter: () => ({ push: navigation.push, replace: navigation.replace }),
  usePathname: () => '/imports/',
}));

vi.mock('./api', () => ({
  importsApi: {
    table: vi.fn(),
    reimport: vi.fn(),
  },
}));

const messages = { imports: importsEn, common: commonEn };

function withProviders(ui: ReactElement) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });

  return render(
    <NextIntlClientProvider locale="en" messages={messages}>
      <QueryClientProvider client={client}>{ui}</QueryClientProvider>
    </NextIntlClientProvider>,
  );
}

function log(overrides: Partial<ImportLogDto>): ImportLogDto {
  return {
    id: '00000000-0000-0000-0000-000000000001',
    projectId: 'demo',
    startedAt: '2026-11-25T10:00:00Z',
    finishedAt: '2026-11-25T10:05:00Z',
    filesRead: 1,
    blocksRead: 1,
    runsBuilt: 1,
    libraryItems: 1,
    errorCount: 0,
    errors: [],
    ...overrides,
  };
}

describe('ImportLog', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('reads the KPI block from its own single-row query', async () => {
    const last = log({ filesRead: 9, errorCount: 0 });
    const page = log({ filesRead: 2, errorCount: 1 });

    vi.mocked(importsApi.table).mockImplementation((_locale, query) =>
      Promise.resolve(
        query.pageSize === 1
          ? { count: 1, data: [last] }
          : { count: 2, data: [page] },
      ),
    );

    withProviders(<ImportLog />);

    expect(await screen.findByText('9')).toBeInTheDocument();
    expect(await screen.findByText('2')).toBeInTheDocument();
    expect(screen.getByText(importsEn.columns.finishedAt)).toBeInTheDocument();
  });
});
