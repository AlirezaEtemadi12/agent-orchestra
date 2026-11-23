'use client';

import { useTranslations } from 'next-intl';
import { Badge } from '@/components/ui/badge';
import type { FolderCheck } from '../types';

export function CheckResult({ check }: { check: FolderCheck }) {
  const t = useTranslations('projects');

  return (
    <div className="space-y-3 rounded-xl border bg-card/40 p-4">
      {check.isReady ? <p className="text-sm font-medium text-emerald-600 dark:text-emerald-400">{t('form.ready')}</p> : null}

      <ul className="grid gap-2 sm:grid-cols-2">
        {check.items.map((item) => (
          <li key={item.name} className="flex items-center justify-between gap-2 text-sm">
            <span data-ltr>
              {item.name}
              {item.inEachRepo ? <span className="ms-2 text-xs text-muted-foreground">{t('form.inEachRepo')}</span> : null}
            </span>
            <Badge variant={item.found ? 'secondary' : 'outline'}>
              {item.found ? t('found') : t('missing')}
            </Badge>
          </li>
        ))}
      </ul>

      {check.repos.length > 0 ? (
        <p className="text-xs text-muted-foreground" data-ltr>
          {check.repos.join(', ')}
        </p>
      ) : null}
    </div>
  );
}
