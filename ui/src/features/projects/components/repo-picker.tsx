'use client';

import { useTranslations } from 'next-intl';
import { useState } from 'react';
import { Button } from '@/components/ui/button';

type Props = {
  detected: string[];
  value: string[];
  disabled: boolean;
  onChange: (repos: string[]) => void;
};

/** Plan 0009 §6.6 step 5: the detected repos as checkboxes, plus one added by its folder name. */
export function RepoPicker({ detected, value, disabled, onChange }: Props) {
  const t = useTranslations('projects');
  const [draft, setDraft] = useState('');

  const all = Array.from(new Set([...detected, ...value]));

  const toggle = (repo: string): void => {
    onChange(value.includes(repo) ? value.filter((item) => item !== repo) : [...value, repo]);
  };

  const add = (): void => {
    const repo = draft.trim();

    if (repo.length === 0 || value.includes(repo)) {
      return;
    }

    onChange([...value, repo]);
    setDraft('');
  };

  return (
    <div className="space-y-2">
      <ul className="grid gap-1 sm:grid-cols-2">
        {all.map((repo) => (
          <li key={repo}>
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                className="size-4"
                checked={value.includes(repo)}
                disabled={disabled}
                onChange={() => toggle(repo)}
              />
              <span data-ltr>{repo}</span>
            </label>
          </li>
        ))}
      </ul>

      {!disabled ? (
        <div className="flex items-center gap-2">
          <input
            value={draft}
            onChange={(event) => setDraft(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === 'Enter') {
                event.preventDefault();
                add();
              }
            }}
            placeholder={t('form.addRepo')}
            className="h-9 w-full rounded-md border bg-background px-2 text-sm"
            data-ltr
          />
          <Button type="button" size="sm" variant="outline" onClick={add}>
            {t('form.add')}
          </Button>
        </div>
      ) : null}
    </div>
  );
}
