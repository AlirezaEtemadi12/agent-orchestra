'use client';

import { useTranslations } from 'next-intl';
import { Badge } from '@/components/ui/badge';

const PLAN_TONES: Record<string, string> = {
  Done: 'bg-success/15 text-success',
  Approved: 'bg-info/15 text-info',
  Running: 'bg-info/15 text-info',
  Draft: 'bg-warning/15 text-warning',
  Dropped: 'bg-muted text-muted-foreground',
  Unknown: 'bg-muted text-muted-foreground',
};

const IDEA_TONES: Record<string, string> = {
  planned: 'bg-success/15 text-success',
  ready: 'bg-info/15 text-info',
  raw: 'bg-warning/15 text-warning',
  dropped: 'bg-muted text-muted-foreground',
};

export function statusTone(status: string, kind: 'plan' | 'idea'): string {
  return (kind === 'idea' ? IDEA_TONES : PLAN_TONES)[status] ?? 'bg-muted text-muted-foreground';
}

/** An idea with no status shows no badge at all (plan 0002 §6.6). */
export function LibraryStatusBadge({
  status,
  kind,
}: {
  status: string | null;
  kind: 'plan' | 'idea';
}) {
  const t = useTranslations('library');
  const label = t as unknown as (key: string) => string;

  if (!status) {
    return null;
  }

  return (
    <Badge variant="secondary" className={statusTone(status, kind)}>
      {kind === 'idea' ? label(`ideaStatus.${status}`) : label(`status.${status}`)}
    </Badge>
  );
}
