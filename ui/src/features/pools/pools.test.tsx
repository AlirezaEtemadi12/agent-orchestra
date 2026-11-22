import { NextIntlClientProvider } from 'next-intl';
import { render, screen } from '@testing-library/react';
import type { ReactElement } from 'react';
import { describe, expect, it } from 'vitest';
import { PoolCards } from './components/pools';
import { poolsEn } from './i18n/en';
import type { Pool } from './types';

const messages = { pools: poolsEn, common: { states: { empty: 'Nothing here yet.' } } };

function withProviders(ui: ReactElement) {
  return render(
    <NextIntlClientProvider locale="en" messages={messages}>
      {ui}
    </NextIntlClientProvider>,
  );
}

function pool(overrides: Partial<Pool> = {}): Pool {
  return {
    pool: 'agent',
    tier: null,
    slots: [],
    waiting: [],
    cooldownUntil: null,
    exhaustedUntil: null,
    ...overrides,
  };
}

describe('the pools screen', () => {
  it('shows who holds a slot and what waits', () => {
    withProviders(
      <PoolCards
        pools={[
          pool({
            slots: [{ n: 1, heldBy: 'T-AGENT-031', since: '2026-11-20T06:00:00Z', unknown: false }],
            waiting: [{ taskId: 'T-AGENT-032', since: '2026-11-20T06:30:00Z' }],
          }),
        ]}
      />,
    );

    expect(screen.getByText('Slot 1')).toBeInTheDocument();
    expect(screen.getByText('T-AGENT-031')).toBeInTheDocument();
    expect(screen.getByText('Waiting')).toBeInTheDocument();
    expect(screen.getByText('T-AGENT-032')).toBeInTheDocument();
  });

  it('never shows a stale slot as held', () => {
    withProviders(
      <PoolCards pools={[pool({ slots: [{ n: 2, heldBy: null, since: null, unknown: true }] })]} />,
    );

    expect(screen.getByText('Slot 2')).toBeInTheDocument();
    expect(screen.getByText('unknown')).toBeInTheDocument();
  });

  it('shows the cooldown and the quota window', () => {
    withProviders(
      <PoolCards
        pools={[
          pool({
            cooldownUntil: '2026-11-20T08:30:00Z',
            exhaustedUntil: '2026-11-21T00:00:00Z',
          }),
        ]}
      />,
    );

    expect(screen.getByText(/Cooling down until/)).toBeInTheDocument();
    expect(screen.getByText(/Quota exhausted until/)).toBeInTheDocument();
  });
});
