import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { NextIntlClientProvider } from 'next-intl';
import { render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ConnectPage } from './components/connect-page';
import { projectsEn } from './i18n/en';

vi.mock('next/navigation', () => ({
  useSearchParams: () => new URLSearchParams('id=shop'),
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ children }: { children: React.ReactNode }) => <a>{children}</a>,
  useRouter: () => ({ push: () => undefined }),
}));

const messages = { projects: projectsEn, common: { errors: { unexpected: 'Something went wrong.' } } };

const token = 'a'.repeat(64);

function project(lastEventAt: string | null) {
  return {
    id: 'shop',
    name: 'MyShop',
    runCount: 0,
    openRunCount: 0,
    sessions: 0,
    calls: 0,
    requests: 0,
    costUsd: null,
    lastImport: null,
    origin: 'dashboard',
    rootPath: 'F:\\Projects\\MyShop',
    runsPath: 'F:\\Projects\\MyShop\\.orchestra\\runs',
    repos: [],
    lastEventAt,
  };
}

function respond(body: unknown): Promise<Response> {
  return Promise.resolve(
    new Response(JSON.stringify(body), { status: 200, headers: { 'Content-Type': 'application/json' } }),
  );
}

function renderPage(lastEventAt: string | null) {
  vi.stubGlobal(
    'fetch',
    vi.fn((input: RequestInfo | URL) => {
      const url = String(input);

      if (url.includes('/api/projects/shop/connect')) {
        return respond({
          reportBlock: `{\n  "report": {\n    "project": "shop"\n  }\n}`,
          localKeys: `{\n  "reportToken": "${token}"\n}`,
          agentPrompt: 'Add this "report" block ...',
          hasProfilesPath: false,
        });
      }

      return respond([project(lastEventAt)]);
    }),
  );

  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });

  return render(
    <QueryClientProvider client={client}>
      <NextIntlClientProvider locale="en" messages={messages}>
        <ConnectPage />
      </NextIntlClientProvider>
    </QueryClientProvider>,
  );
}

describe('the connect page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('shows the three pieces, keeps the token out of the agent prompt and waits', async () => {
    renderPage(null);

    expect(await screen.findByText('Connect MyShop')).toBeInTheDocument();
    expect(await screen.findByText('1. Add this block to .orchestra/orchestrator.json')).toBeInTheDocument();
    expect(screen.getByText('2. Add these keys to .orchestra/orchestrator.local.json yourself. They hold the token, so they never go to an agent. Keep enginePath as it is.')).toBeInTheDocument();
    expect(screen.getByText('3. Waiting for the first event — start any run of this project.')).toBeInTheDocument();
    expect(screen.getByText(/reportToken/)).toBeInTheDocument();
    expect(screen.queryByText(new RegExp(token))).toBeTruthy();
    expect(screen.getByText('Add this "report" block ...')).toBeInTheDocument();
  });

  it('says connected once the project has an event', async () => {
    renderPage('2026-11-23T08:00:00Z');

    expect(await screen.findByText(/3\. Connected — last event/)).toBeInTheDocument();
  });
});
