'use client';

import { useTranslations } from 'next-intl';
import { useSearchParams } from 'next/navigation';
import { useRouter } from '@/i18n/navigation';
import { DateTimeText, DurationText, MoneyText, TokensText } from '@/components/app/data-text';
import { EmptyState } from '@/components/app/empty-state';
import { ErrorState } from '@/components/app/error-state';
import { KpiCard } from '@/components/app/kpi-card';
import { Markdown } from '@/components/app/markdown';
import { PageHeader } from '@/components/app/page-header';
import { StatusBadge } from '@/components/app/status-badge';
import { TableSkeleton } from '@/components/app/table-skeleton';
import { VerdictBadge } from '@/components/app/verdict-badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Link } from '@/i18n/navigation';
import { useRun } from '../queries';
import type { StageResult } from '../types';

type RunsTranslator = ReturnType<typeof useTranslations<'runs'>>;

function stageLabel(t: RunsTranslator, stage: StageResult): string {
  if (stage.stage === 'Review') {
    return t('stage.review');
  }

  if (stage.stage === 'ReadBack') {
    return t('stage.readBack');
  }

  if (stage.stage === 'Implement') {
    return t('stage.implement', { phase: stage.phase ?? 0 });
  }

  return t('stage.verify');
}

export function RunDetail() {
  const t = useTranslations('runs');
  const common = useTranslations('common');
  const params = useSearchParams();
  const router = useRouter();
  const project = params.get('project');
  const runKey = params.get('run');

  const run = useRun(project, runKey);

  if (!project || !runKey) {
    return (
      <div className="space-y-6">
        <Button type="button" variant="outline" size="sm" onClick={() => router.push('/')}>
          {common('actions.back')}
        </Button>
        <EmptyState title={t('detail.notFound')} />
      </div>
    );
  }

  if (run.isPending) {
    return <TableSkeleton rows={8} />;
  }

  if (run.isError || !run.data) {
    return (
      <div className="space-y-4">
        <ErrorState
          title={t('detail.notFound')}
          retryLabel={common('actions.retry')}
          onRetry={() => void run.refetch()}
        />
        <Button type="button" variant="outline" size="sm" onClick={() => router.push('/')}>
          {common('actions.back')}
        </Button>
      </div>
    );
  }

  const detail = run.data;

  return (
    <div className="space-y-6">
      <div className="space-y-2">
        <Button type="button" variant="outline" size="sm" onClick={() => router.push('/')}>
          {common('actions.back')}
        </Button>
        <PageHeader
          title={detail.runKey}
          description={[detail.projectName, detail.planNumber ? `v${detail.planNumber}` : null, detail.planTitle ?? detail.task]
            .filter(Boolean)
            .join(' · ')}
          actions={
            <div className="flex items-center gap-2">
              {detail.plan ? (
                <Link
                  href={{
                    pathname: '/library/view/',
                    query: {
                      project: detail.projectId,
                      category: 'Plan',
                      path: detail.plan.relPath,
                    },
                  }}
                  className="text-sm underline underline-offset-4"
                >
                  {t('detail.plan')}
                </Link>
              ) : null}
              <StatusBadge status={detail.status} />
            </div>
          }
        />
        <div className="flex flex-wrap gap-4 text-xs text-muted-foreground">
          <span>
            {t('detail.branch')}: <span data-ltr>{detail.branch ?? '—'}</span>
          </span>
          <span>
            {t('detail.planVersion')}: <span data-ltr>{detail.planVersion ?? '—'}</span>
          </span>
          <span>
            {t('columns.repo')}: <span data-ltr>{detail.repo ?? '—'}</span>
          </span>
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <KpiCard label={t('kpi.sessions')} value={detail.sessions} />
        <KpiCard label={t('kpi.calls')} value={detail.calls} />
        <KpiCard label={t('detail.totalCost')} value={<MoneyText usd={detail.totalCostUsd} />} />
        <KpiCard label={t('columns.updated')} value={<DateTimeText value={detail.updatedAt} />} />
      </div>

      {detail.live ? (
        <Card>
          <CardHeader>
            <CardTitle>{t('detail.live')}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-2">
            <div className="flex flex-wrap gap-x-4 gap-y-1 text-xs text-muted-foreground">
              {detail.live.stage ? <span data-ltr>{detail.live.stage}</span> : null}
              {detail.live.model ? <span data-ltr>{detail.live.model}</span> : null}
              {detail.live.pool ? (
                <span data-ltr>
                  {detail.live.pool} · {detail.live.heldBy ?? '—'}
                </span>
              ) : null}
              <DateTimeText value={detail.live.at} />
            </div>
            <p className="text-xs text-muted-foreground">{t('detail.liveLine')}</p>
            <p className="line-clamp-3 font-mono text-xs" data-ltr>
              {detail.live.lastLine ?? '—'}
            </p>
          </CardContent>
        </Card>
      ) : null}

      <Card>
        <CardHeader>
          <CardTitle>{t('detail.stages')}</CardTitle>
        </CardHeader>
        <CardContent>
          {detail.stages.length === 0 ? (
            <p className="text-sm text-muted-foreground">{common('states.empty')}</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('columns.stage')}</TableHead>
                  <TableHead>{t('columns.model')}</TableHead>
                  <TableHead>{t('columns.verdict')}</TableHead>
                  <TableHead>{t('columns.findings')}</TableHead>
                  <TableHead>{t('columns.at')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {detail.stages.map((stage) => (
                  <TableRow key={`${stage.stage}.${stage.phase}.${stage.round}`}>
                    <TableCell>{stageLabel(t, stage)}</TableCell>
                    <TableCell data-ltr>{stage.model ?? '—'}</TableCell>
                    <TableCell>
                      <VerdictBadge verdict={stage.verdict} />
                    </TableCell>
                    <TableCell className="tabular-nums" data-ltr>
                      {[stage.blockers, stage.should, stage.nits].some((value) => value !== null)
                        ? `${stage.blockers ?? 0}/${stage.should ?? 0}/${stage.nits ?? 0}`
                        : [stage.changed, stage.outsidePlan, stage.violations].some((value) => value !== null)
                          ? `${stage.changed ?? 0}/${stage.outsidePlan ?? 0}/${stage.violations ?? 0}`
                          : stage.gatesGreen === null
                            ? '—'
                            : stage.gatesGreen
                              ? 'OK'
                              : 'FAIL'}
                    </TableCell>
                    <TableCell>
                      <DateTimeText value={stage.at} />
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {detail.roundRows.length > 0 ? (
        <Card>
          <CardHeader>
            <CardTitle>{t('detail.rounds')}</CardTitle>
          </CardHeader>
          <CardContent>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('columns.stage')}</TableHead>
                  <TableHead>{t('columns.model')}</TableHead>
                  <TableHead>{t('columns.verdict')}</TableHead>
                  <TableHead>{t('columns.findings')}</TableHead>
                  <TableHead>{t('columns.at')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {detail.roundRows.map((stage) => (
                  <TableRow key={`${stage.stage}.${stage.round}`}>
                    <TableCell>
                      {stageLabel(t, stage)}{' '}
                      {stage.round ? <span className="text-xs text-muted-foreground">{t('detail.textRound', { round: stage.round })}</span> : null}
                    </TableCell>
                    <TableCell data-ltr>{stage.model ?? '—'}</TableCell>
                    <TableCell>
                      <VerdictBadge verdict={stage.verdict} />
                    </TableCell>
                    <TableCell className="tabular-nums" data-ltr>
                      {`${stage.blockers ?? 0}/${stage.should ?? 0}/${stage.nits ?? 0}`}
                    </TableCell>
                    <TableCell>
                      <DateTimeText value={stage.at} />
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      ) : null}

      <Card>
        <CardHeader>
          <CardTitle>{t('detail.sessions')}</CardTitle>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t('columns.session')}</TableHead>
                <TableHead>{t('columns.firstRole')}</TableHead>
                <TableHead>{t('columns.model')}</TableHead>
                <TableHead>{t('columns.calls')}</TableHead>
                <TableHead>{t('columns.input')}</TableHead>
                <TableHead>{t('columns.output')}</TableHead>
                <TableHead>{t('columns.cacheRead')}</TableHead>
                <TableHead>{t('columns.cost')}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {detail.sessionRows.map((session) => (
                <TableRow key={session.sessionId}>
                  <TableCell data-ltr className="text-xs">
                    {session.sessionId}
                  </TableCell>
                  <TableCell data-ltr>{session.firstRole ?? '—'}</TableCell>
                  <TableCell data-ltr>{session.model ?? '—'}</TableCell>
                  <TableCell data-ltr>{session.calls ?? '—'}</TableCell>
                  <TableCell>
                    <TokensText value={session.input} />
                  </TableCell>
                  <TableCell>
                    <TokensText value={session.output} />
                  </TableCell>
                  <TableCell>
                    <TokensText value={session.cacheRead} />
                  </TableCell>
                  <TableCell>
                    <MoneyText usd={session.costUsd} />
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('detail.stageUsage')}</CardTitle>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>{t('columns.stage')}</TableHead>
                <TableHead>{t('columns.model')}</TableHead>
                <TableHead>{t('columns.input')}</TableHead>
                <TableHead>{t('columns.output')}</TableHead>
                <TableHead>{t('columns.time')}</TableHead>
                <TableHead>{t('columns.cost')}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {detail.stageUsage.map((usage) => (
                <TableRow key={`${usage.stage}.${usage.model}`}>
                  <TableCell data-ltr>{usage.stage}</TableCell>
                  <TableCell data-ltr>{usage.model ?? '—'}</TableCell>
                  <TableCell>
                    <TokensText value={usage.input} />
                  </TableCell>
                  <TableCell>
                    <TokensText value={usage.output} />
                  </TableCell>
                  <TableCell>
                    <DurationText seconds={usage.seconds} />
                  </TableCell>
                  <TableCell>
                    <MoneyText usd={usage.costUsd} />
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>{t('detail.texts')}</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          {detail.texts.length === 0 ? (
            <p className="text-sm text-muted-foreground">{t('detail.noTexts')}</p>
          ) : (
            detail.texts.map((text) => (
              <details key={`${text.stage}.${text.round}.${text.name}`} className="rounded-md border p-3">
                <summary className="cursor-pointer text-sm font-medium">
                  <span data-ltr>
                    {text.stage} · {text.name}
                  </span>{' '}
                  {text.round ? (
                    <span className="text-xs text-muted-foreground">{t('detail.textRound', { round: text.round })}</span>
                  ) : null}
                </summary>
                <div className="mt-3 max-h-96 overflow-auto">
                  <Markdown source={text.text} />
                </div>
              </details>
            ))
          )}
        </CardContent>
      </Card>
    </div>
  );
}
