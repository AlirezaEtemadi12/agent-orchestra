/** Plan 0006 D6: always on; the editor shows them disabled and the engine rejects them off. */
export const LOCKED_STEPS = ['understanding', 'plan', 'review', 'approve', 'implement', 'verify'] as const;

export const ALL_STEPS = [
  'understanding',
  'preview',
  'plan',
  'review',
  'approve',
  'implement',
  'verify',
] as const;

/** Keys a profile owns (D1). Anything else in a project's file stays there, untouched by the merge. */
export const PROFILE_KEYS = [
  'roles',
  'tiers',
  'adapters',
  'modelRatesSource',
  'modelRates',
  'quota',
  'sessions',
  'revise',
  'readback',
  'preflight',
  'defaults',
  'rules',
  'workflow',
] as const;

export type ProfileConfig = Record<string, unknown>;

export function isLockedStep(step: string): boolean {
  return (LOCKED_STEPS as readonly string[]).includes(step);
}

export function parseProfile(json: string): { value: ProfileConfig | null; error: string | null } {
  try {
    const value: unknown = JSON.parse(json);

    if (value === null || typeof value !== 'object' || Array.isArray(value)) {
      return { value: null, error: 'The profile is not a JSON object.' };
    }

    return { value: value as ProfileConfig, error: null };
  } catch (error) {
    return { value: null, error: error instanceof Error ? error.message : 'Invalid JSON.' };
  }
}

export function section(config: ProfileConfig, key: string): Record<string, unknown> {
  const value = config[key];

  return value !== null && typeof value === 'object' && !Array.isArray(value)
    ? (value as Record<string, unknown>)
    : {};
}

export function stepEnabled(config: ProfileConfig, step: string): boolean {
  const workflow = section(config, 'workflow');
  const steps = section(workflow, 'steps');
  const spec = steps[step];

  if (spec !== null && typeof spec === 'object' && 'enabled' in spec) {
    return Boolean((spec as { enabled: unknown }).enabled);
  }

  return true;
}

export function readbackEnabled(config: ProfileConfig): boolean {
  const readback = section(config, 'readback');

  return 'enabled' in readback ? Boolean(readback.enabled) : true;
}

/** D6: read-back is not a workflow step; the editor shows it among them and writes `readback.enabled`. */
export function withStep(config: ProfileConfig, step: string, enabled: boolean): ProfileConfig {
  const copy = structuredClone(config);

  if (step === 'readback') {
    copy.readback = { ...section(copy, 'readback'), enabled };

    return copy;
  }

  const workflow = { ...section(copy, 'workflow') };
  const steps = { ...section(workflow, 'steps') };
  const spec = steps[step];

  steps[step] = {
    ...(spec !== null && typeof spec === 'object' && !Array.isArray(spec) ? spec : {}),
    enabled,
  };
  workflow.steps = steps;
  copy.workflow = workflow;

  return copy;
}

/** The value at a dotted source path, for the effective-config table. Arrays stop the walk. */
export function valueAt(config: unknown, path: string): unknown {
  let current: unknown = config;

  for (const part of path.split('.')) {
    if (current === null || typeof current !== 'object' || Array.isArray(current)) {
      return undefined;
    }

    current = (current as Record<string, unknown>)[part];
  }

  return current;
}
