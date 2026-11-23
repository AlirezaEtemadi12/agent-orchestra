'use client';

import { useTranslations } from 'next-intl';
import { useHosting } from '../queries';

/** Plan 0009 §6.6 step 1: one line, from `GET api/system/hosting`. */
export function HostingLine() {
  const t = useTranslations('projects');
  const hosting = useHosting();

  if (!hosting.data) {
    return null;
  }

  const { mode, roots, mapped } = hosting.data;

  const text =
    mode === 'container'
      ? mapped
        ? t('hosting.docker', { roots: roots.join(', ') })
        : t('hosting.dockerNone')
      : t('hosting.direct');

  return (
    <p className={mode === 'container' && !mapped ? 'text-sm text-destructive' : 'text-sm text-muted-foreground'}>
      {text}
    </p>
  );
}
