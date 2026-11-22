'use client';

import { useTranslations } from 'next-intl';
import { ALL_STEPS, isLockedStep, readbackEnabled, stepEnabled, withStep, type ProfileConfig } from '../profile-form';

type WorkflowStepsProps = {
  config: ProfileConfig;
  onChange: (next: ProfileConfig) => void;
};

/** D6: every step of the run is shown; the six locked ones are disabled and preview is the only
 * adjustable one. Read-back appears here but is saved as `readback.enabled`. */
export function WorkflowSteps({ config, onChange }: WorkflowStepsProps) {
  const t = useTranslations('profiles');

  const rows = [
    ...ALL_STEPS.map((step) => ({
      step,
      checked: stepEnabled(config, step),
      locked: isLockedStep(step),
    })),
    { step: 'readback', checked: readbackEnabled(config), locked: false },
  ];

  return (
    <div className="space-y-2">
      {rows.map((row) => (
        <label key={row.step} className="flex items-center gap-3 text-sm">
          <input
            type="checkbox"
            className="size-4 accent-current"
            checked={row.checked}
            disabled={row.locked}
            onChange={(event) => onChange(withStep(config, row.step, event.target.checked))}
          />
          <span data-ltr>{row.step}</span>
          {row.locked ? <span className="text-xs text-muted-foreground">{t('locked')}</span> : null}
        </label>
      ))}
    </div>
  );
}
