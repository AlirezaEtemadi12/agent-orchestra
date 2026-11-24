import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { NextIntlClientProvider } from 'next-intl';
import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { SetupStep } from './components/setup-step';
import { projectsEn } from './i18n/en';
import type { SetupPlan } from './types';

vi.mock('next/navigation', () => ({
  useSearchParams: () => new URLSearchParams('id=shop'),
}));

vi.mock('@/i18n/navigation', () => ({
  Link: ({ children }: { children: React.ReactNode }) => <a>{children}</a>,
  useRouter: () => ({ push: () => undefined }),
}));

const messages = { projects: projectsEn, common: { errors: { unexpected: 'Something went wrong.' } } };

function project() {
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
    repos: ['App'],
    lastEventAt: null,
  };
}

function plan(engineFound = true): SetupPlan {
  return {
    engineFound,
    isSingleRepo: false,
    items: [
      { id: 'config', path: '.orchestra/orchestrator.json', action: 'create', optional: false, detail: null },
      { id: 'knowledge', path: '.ai/knowledge', action: 'create', optional: true, detail: null },
    ],
    verifyCommands: { App: ['npm run build', 'npm test'] },
  };
}

function respond(body: unknown): Promise<Response> {
  return Promise.resolve(
    new Response(JSON.stringify(body), {
      status: 200,
      headers: { 'Content-Type': 'application/json' },
    }),
  );
}

type Seen = { url: string; body: unknown };

function renderStep(engineFound = true, applied = false): Seen[] {
  const seen: Seen[] = [];

  vi.stubGlobal(
    'fetch',
    vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);
      const body = typeof init?.body === 'string' ? JSON.parse(init.body) : null;

      seen.push({ url, body });

      if (url.includes('/api/projects/setup/plan')) {
        return respond(plan(engineFound));
      }

      if (url.includes('/api/projects/setup/apply')) {
        return respond({
          items: [{ id: 'config', ok: !applied, error: applied ? 'Setup.Item.WriteFailed' : null, detail: null }],
          project: { id: 'shop', name: 'MyShop', rootPath: 'F:\\Projects\\MyShop', runsPath: '', repos: [], profile: 'default' },
          knowledgePrompt: 'Nothing to create: .ai/ and AGENTS.md are already there.',
        });
      }

      return respond([project()]);
    }),
  );

  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });

  render(
    <QueryClientProvider client={client}>
      <NextIntlClientProvider locale="en" messages={messages}>
        <SetupStep />
      </NextIntlClientProvider>
    </QueryClientProvider>,
  );

  return seen;
}

describe('the setup step', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('lists the rows with their action and the commands to edit', async () => {
    renderStep();

    expect(await screen.findByText('.orchestra/orchestrator.json')).toBeInTheDocument();
    expect(screen.getAllByText('will be created')).toHaveLength(2);
    expect(await screen.findByRole('textbox')).toHaveValue('npm run build\nnpm test');
    expect(screen.getByRole('button', { name: 'Set up' })).toBeEnabled();
  });

  it('sends a removed tick as a skip and keeps the rest', async () => {
    const seen = renderStep();

    fireEvent.click(await screen.findByRole('checkbox', { name: '.ai/knowledge' }));

    await waitFor(() => {
      expect(seen.some((call) => JSON.stringify(call.body).includes('"skip":["knowledge"]'))).toBe(true);
    });

    // The row is still in the list; only the plan changed.
    expect(screen.getByText('.ai/knowledge')).toBeInTheDocument();
  });

  it('sends the commands as one list per repo, never as the textarea text', async () => {
    const seen = renderStep();

    const box = await screen.findByRole('textbox');

    fireEvent.change(box, { target: { value: 'dotnet build\n\n  dotnet test  \n' } });
    fireEvent.click(screen.getByRole('button', { name: 'Set up' }));

    await waitFor(() => {
      const apply = seen.find((call) => call.url.includes('/api/projects/setup/apply'));

      expect(apply?.body).toMatchObject({ verifyCommands: { App: ['dotnet build', 'dotnet test'] } });
    });

    // Every plan call after the first carries lists too: the API answers 400 to a string.
    const plans = seen.filter((call) => call.url.includes('/api/projects/setup/plan'));

    for (const call of plans.slice(1)) {
      const commands = (call.body as { verifyCommands: Record<string, unknown> }).verifyCommands;

      expect(Object.values(commands).every(Array.isArray)).toBe(true);
    }
  });

  it('keeps the button off and says why when the engine is missing', async () => {
    renderStep(false);

    expect(await screen.findByText('The engine folder was not found. Set Orchestra:EnginePath.')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Set up' })).toBeDisabled();
  });

  it('shows the failure of a row and waits for the first event', async () => {
    renderStep(true, true);

    fireEvent.click(await screen.findByRole('button', { name: 'Set up' }));

    expect(await screen.findByText('Could not write this file.')).toBeInTheDocument();
    expect(screen.getByText('Waiting for the first event')).toBeInTheDocument();
    expect(screen.queryByText(messages.projects.setup.knowledge)).not.toBeInTheDocument();
  });
});
