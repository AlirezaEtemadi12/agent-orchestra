'use client';

import { useTranslations } from 'next-intl';
import { DurationText } from '@/components/app/data-text';
import { EmptyState } from '@/components/app/empty-state';
import { ErrorState } from '@/components/app/error-state';
import { PageHeader } from '@/components/app/page-header';
import { StatusBadge } from '@/components/app/status-badge';
import { TableSkeleton } from '@/components/app/table-skeleton';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent } from '@/components/ui/card';
import { useBoard } from '../queries';
import type { BoardCard, BoardLanes as Lanes } from '../types';

const WAITING_KEYS = {
  approval: 'waiting.approval',
  manual: 'waiting.manual',
  needInput: 'waiting.needInput',
  verifyFail: 'waiting.verifyFail',
} as const;

export function Board() {
  const t = useTranslations('board');
  const common = useTranslations('common');
  const board = useBoard();

  return (
    <div className="space-y-6">
      <PageHeader title={t('title')} />

      {board.isPending ? <TableSkeleton /> : null}

      {board.isError ? (
        <ErrorState
          title={common('errors.network')}
          retryLabel={common('actions.retry')}
          onRetry={() => void board.refetch()}
        />
      ) : null}

      {board.data?.reporter === 'off' ? (
        <p className="rounded-lg border border-warning/40 bg-warning/10 p-3 text-sm text-warning">
          {t('reporterOff')}
        </p>
      ) : null}

      {board.data ? <BoardLanes lanes={board.data.lanes} /> : null}
    </div>
  );
}

/// Props only, so a test can render the four lanes without a server (plan 0004 §7).
export function BoardLanes({ lanes }: { lanes: Lanes }) {
  const t = useTranslations('board');
  const common = useTranslations('common');

  const columns = [
    { key: 'running', title: t('lanes.running'), cards: lanes.running },
    { key: 'needsYou', title: t('lanes.needsYou'), cards: lanes.needsYou },
    { key: 'done', title: t('lanes.done'), cards: lanes.done },
    { key: 'failed', title: t('lanes.failed'), cards: lanes.failed },
  ];

  if (columns.every((column) => column.cards.length === 0)) {
    return <EmptyState title={t('empty')} />;
  }

  return (
    <div className="grid gap-4 lg:grid-cols-4">
      {columns.map((column) => (
        <section key={column.key} className="space-y-3" aria-label={column.title}>
          <h2 className="flex items-center gap-2 text-sm font-medium">
            {column.title}
            <span className="tabular-nums text-xs text-muted-foreground" data-ltr>
              {column.cards.length}
            </span>
          </h2>

          {column.cards.length === 0 ? (
            <p className="rounded-lg border border-dashed p-3 text-xs text-muted-foreground">
              {common('states.empty')}
            </p>
          ) : (
            column.cards.map((card) => <BoardCardView key={`${card.projectId}.${card.taskId}`} card={card} />)
          )}
        </section>
      ))}
    </div>
  );
}

export function BoardCardView({ card }: { card: BoardCard }) {
  const t = useTranslations('board');

  return (
    <Card size="sm">
      <CardContent className="space-y-2">
        <div className="flex items-start justify-between gap-2">
          <div className="min-w-0 space-y-1">
            <p className="truncate text-sm font-medium" data-ltr>
              {card.planNumber ? `${card.planNumber} · ${card.planTitle ?? card.taskId}` : card.taskId}
            </p>
            <p className="text-xs text-muted-foreground" data-ltr>
              {card.projectId} · {card.taskId}
            </p>
          </div>
          <StatusBadge status={card.status} />
        </div>

        {card.waitingFor ? <WaitingBadge card={card} /> : null}

        <div className="flex flex-wrap gap-x-3 gap-y-1 text-xs text-muted-foreground">
          {card.stage ? <span data-ltr>{card.stage}</span> : null}
          {card.model ? (
            <span className="truncate" data-ltr>
              {card.model}
            </span>
          ) : null}
          <span>
            {t('elapsed')}: <DurationText seconds={card.elapsedSec} />
          </span>
        </div>

        {card.lastLine ? (
          <p className="line-clamp-2 font-mono text-xs text-muted-foreground" data-ltr>
            {card.lastLine}
          </p>
        ) : null}
      </CardContent>
    </Card>
  );
}

function WaitingBadge({ card }: { card: BoardCard }) {
  const t = useTranslations('board');
  const waiting = card.waitingFor;

  if (!waiting) {
    return null;
  }

  const text =
    waiting === 'pool'
      ? t('waiting.pool', { pool: card.pool ?? '—', heldBy: card.heldBy ?? '—' })
      : t(WAITING_KEYS[waiting]);

  return (
    <Badge variant="secondary" className="bg-warning/15 text-warning">
      {text}
    </Badge>
  );
}
