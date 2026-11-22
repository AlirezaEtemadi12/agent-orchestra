import { NextIntlClientProvider } from 'next-intl';
import { render, screen } from '@testing-library/react';
import type { ReactElement } from 'react';
import { describe, expect, it, vi } from 'vitest';
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

function withProviders(ui: ReactElement) {
  return render(
    <NextIntlClientProvider locale="en" messages={messages}>
      {ui}
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
