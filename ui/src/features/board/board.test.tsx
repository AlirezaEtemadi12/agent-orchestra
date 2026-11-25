import { NextIntlClientProvider } from 'next-intl';
import { render, screen } from '@testing-library/react';
import type { ReactElement } from 'react';
import { describe, expect, it } from 'vitest';
import { BoardCardView, BoardLanes } from './components/board';
import { boardEn } from './i18n/en';
import type { BoardCard, BoardLanes as Lanes } from './types';

const messages = { board: boardEn, common: { states: { empty: 'Nothing here yet.' } } };

function withProviders(ui: ReactElement) {
  return render(
    <NextIntlClientProvider locale="en" messages={messages}>
      {ui}
    </NextIntlClientProvider>,
  );
}

function card(overrides: Partial<BoardCard> = {}): BoardCard {
  return {
    projectId: 'project-b',
    runKey: 'T-AGENT-031.1',
    taskId: 'T-AGENT-031',
    status: 'Implementing',
    planNumber: '0159',
    planTitle: 'The live plan',
    stage: 'review',
    model: 'opencode:glm-5.3',
    startedAt: '2026-11-20T06:00:00Z',
    elapsedSec: 3720,
    lastLine: 'reading src/app.tsx',
    waitingFor: null,
    pool: null,
    heldBy: null,
    repos: [],
    ...overrides,
  };
}

function lanes(overrides: Partial<Lanes> = {}): Lanes {
  return { running: [], needsYou: [], done: [], failed: [], ...overrides };
}

describe('the runs board', () => {
  it('renders the four lanes and a card inside its lane', () => {
    withProviders(<BoardLanes lanes={lanes({ running: [card()] })} />);

    expect(screen.getByRole('region', { name: 'Running' })).toBeInTheDocument();
    expect(screen.getByRole('region', { name: 'Needs you' })).toBeInTheDocument();
    expect(screen.getByText('0159 · The live plan')).toBeInTheDocument();
    expect(screen.getByText('reading src/app.tsx')).toBeInTheDocument();
    expect(screen.getByText('1:02:00')).toBeInTheDocument();
  });

  it('turns the waiting state into its own badge', () => {
    withProviders(<BoardCardView card={card({ status: 'AwaitingApproval', waitingFor: 'approval' })} />);

    expect(screen.getByText('Waiting for your approval')).toBeInTheDocument();
  });

  it('shows one chip per repo of the plan', () => {
    withProviders(
      <BoardCardView
        card={card({
          repos: [
            { name: 'Web', status: 'Implemented' },
            { name: 'Ui', status: null },
          ],
        })}
      />,
    );

    expect(screen.getByText('Web · Implemented')).toBeInTheDocument();
    expect(screen.getByText('Ui')).toBeInTheDocument();
  });

  it('names the pool it waits for', () => {
    withProviders(
      <BoardCardView card={card({ waitingFor: 'pool', pool: 'agent', heldBy: 'T-AGENT-040' })} />,
    );

    expect(screen.getByText('Waiting for agent, held by T-AGENT-040')).toBeInTheDocument();
  });

  it('says so when no run reported in a day', () => {
    withProviders(<BoardLanes lanes={lanes()} />);

    expect(screen.getByText('No run had an event in the last 24 hours.')).toBeInTheDocument();
  });
});
