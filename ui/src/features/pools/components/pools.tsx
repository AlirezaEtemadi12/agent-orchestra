'use client';

import { useTranslations } from 'next-intl';
import { EmptyState } from '@/components/app/empty-state';
import { ErrorState } from '@/components/app/error-state';
import { PageHeader } from '@/components/app/page-header';
import { TableSkeleton } from '@/components/app/table-skeleton';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { useAppFormatter } from '@/lib/i18n/formatter';
import { usePools } from '../queries';
import type { Pool, PoolSlot } from '../types';

export function Pools() {
  const t = useTranslations('pools');
  const common = useTranslations('common');
  const pools = usePools();

  return (
    <div className="space-y-6">
      <PageHeader title={t('title')} />

      {pools.isPending ? <TableSkeleton /> : null}

      {pools.isError ? (
        <ErrorState
          title={common('errors.network')}
          retryLabel={common('actions.retry')}
          onRetry={() => void pools.refetch()}
        />
      ) : null}

      {pools.data && pools.data.length === 0 ? <EmptyState title={common('states.empty')} /> : null}

      {pools.data ? <PoolCards pools={pools.data} /> : null}
    </div>
  );
}

/// Props only, so a test can render the cards without a server.
export function PoolCards({ pools }: { pools: Pool[] }) {
  const t = useTranslations('pools');
  const formatter = useAppFormatter();

  return (
    <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
      {pools.map((pool) => (
        <Card key={pool.pool}>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <span data-ltr>{pool.pool}</span>
              {pool.tier ? (
                <Badge variant="outline">
                  <span data-ltr>{pool.tier}</span>
                </Badge>
              ) : null}
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            <div className="flex flex-wrap gap-2">
              {pool.slots.length === 0 ? (
                <span className="text-xs text-muted-foreground">{t('free')}</span>
              ) : (
                pool.slots.map((slot) => <SlotChip key={slot.n} slot={slot} />)
              )}
            </div>

            {pool.waiting.length > 0 ? (
              <div className="space-y-1">
                <p className="text-xs font-medium">{t('waiting')}</p>
                <ul className="space-y-1 text-xs text-muted-foreground">
                  {pool.waiting.map((entry) => (
                    <li key={`${entry.taskId}.${entry.since}`} className="flex justify-between gap-2">
                      <span data-ltr>{entry.taskId}</span>
                      <span className="tabular-nums">{formatter.dateTime(entry.since)}</span>
                    </li>
                  ))}
                </ul>
              </div>
            ) : null}

            {pool.cooldownUntil ? (
              <p className="text-xs text-warning">
                {t('cooldownUntil', { time: formatter.dateTime(pool.cooldownUntil) })}
              </p>
            ) : null}

            {pool.exhaustedUntil ? (
              <p className="text-xs text-destructive">
                {t('exhaustedUntil', { time: formatter.dateTime(pool.exhaustedUntil) })}
              </p>
            ) : null}
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

function SlotChip({ slot }: { slot: PoolSlot }) {
  const t = useTranslations('pools');

  return (
    <Badge variant="secondary">
      <span>{t('slot', { n: slot.n })}</span>
      <span className="text-muted-foreground" data-ltr>
        {slot.unknown ? t('unknown') : (slot.heldBy ?? t('free'))}
      </span>
    </Badge>
  );
}
