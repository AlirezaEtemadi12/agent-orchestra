import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { NextIntlClientProvider } from 'next-intl';
import { render, screen } from '@testing-library/react';
import type { ReactElement } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { commonEn } from '@/lib/i18n/messages/common.en';
import { profilesApi } from './api';
import { ProfileHistory } from './components/profile-history';
import { WorkflowSteps } from './components/workflow-steps';
import { profilesEn } from './i18n/en';
import {
  isLockedStep,
  parseProfile,
  readbackEnabled,
  section,
  stepEnabled,
  withStep,
  valueAt,
} from './profile-form';

const messages = { profiles: profilesEn };

const navigation = vi.hoisted(() => ({
  push: vi.fn(),
  replace: vi.fn(),
}));

vi.mock('next/navigation', () => ({
  useSearchParams: () => new URLSearchParams(),
  useRouter: () => ({ push: navigation.push, replace: navigation.replace }),
  usePathname: () => '/settings/profiles/edit/',
}));

vi.mock('./api', () => ({
  profilesApi: {
    list: vi.fn(),
    get: vi.fn(),
    save: vi.fn(),
    revert: vi.fn(),
    diff: vi.fn(),
    versionsTable: vi.fn(),
  },
}));

function withProviders(ui: ReactElement) {
  return render(
    <NextIntlClientProvider locale="en" messages={messages}>
      {ui}
    </NextIntlClientProvider>,
  );
}

function withHistoryProviders(ui: ReactElement) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });

  return render(
    <NextIntlClientProvider locale="en" messages={{ profiles: profilesEn, common: commonEn }}>
      <QueryClientProvider client={client}>{ui}</QueryClientProvider>
    </NextIntlClientProvider>,
  );
}

describe('the profile rules', () => {
  it('locks the six engine steps and leaves preview and read-back adjustable', () => {
    expect(isLockedStep('review')).toBe(true);
    expect(isLockedStep('approve')).toBe(true);
    expect(isLockedStep('preview')).toBe(false);
    expect(isLockedStep('readback')).toBe(false);
  });

  it('has every step on when the profile says nothing', () => {
    const config = {};

    expect(stepEnabled(config, 'preview')).toBe(true);
    expect(stepEnabled(config, 'review')).toBe(true);
    expect(readbackEnabled(config)).toBe(true);
  });

  it('turns preview off inside workflow.steps', () => {
    const next = withStep({ workflow: {} }, 'preview', false);

    expect(section(next, 'workflow').steps).toEqual({ preview: { enabled: false } });
    expect(stepEnabled(next, 'preview')).toBe(false);
    expect(stepEnabled(next, 'review')).toBe(true);
  });

  it('writes read-back to readback.enabled, never to workflow.steps', () => {
    const next = withStep({}, 'readback', false);

    expect(readbackEnabled(next)).toBe(false);
    expect(section(next, 'workflow').steps).toBeUndefined();
  });

  it('refuses a profile that is not a JSON object', () => {
    expect(parseProfile('[1,2]').error).not.toBeNull();
    expect(parseProfile('{ "roles": {} }').value).toEqual({ roles: {} });
  });

  it('reads a source value by path', () => {
    expect(valueAt({ tiers: { free: { maxConcurrent: 1 } } }, 'tiers.free.maxConcurrent')).toBe(1);
    expect(valueAt({}, 'tiers.free')).toBeUndefined();
  });
});

describe('the profile history', () => {
  it('pages the versions and has no search box', async () => {
    vi.mocked(profilesApi.versionsTable).mockResolvedValue({
      count: 2,
      data: [
        { version: 2, reason: 'second', createdAt: '2026-11-02T00:00:00Z' },
        { version: 1, reason: 'first', createdAt: '2026-11-01T00:00:00Z' },
      ],
    });

    withHistoryProviders(
      <ProfileHistory id="default" currentVersion={2} busy={false} onRevert={vi.fn()} />,
    );

    expect(await screen.findByText('Version 2')).toBeInTheDocument();
    expect(screen.getByText('second')).toBeInTheDocument();
    expect(screen.getByText('1–2 of 2')).toBeInTheDocument();
    expect(screen.queryByPlaceholderText(commonEn.table.searchPlaceholder)).toBeNull();
  });
});

describe('the workflow switches', () => {
  it('disables a locked step and says why', () => {
    withProviders(<WorkflowSteps config={{}} onChange={vi.fn()} />);

    expect(screen.getByRole('checkbox', { name: /^review/ })).toBeDisabled();
    expect(screen.getByRole('checkbox', { name: /^preview/ })).not.toBeDisabled();
    expect(screen.getAllByText('Always on').length).toBe(6);
  });

  it('shows read-back among the steps as its own switch', () => {
    withProviders(<WorkflowSteps config={{ readback: { enabled: false } }} onChange={vi.fn()} />);

    expect(screen.getByRole('checkbox', { name: /^readback/ })).not.toBeChecked();
  });
});
