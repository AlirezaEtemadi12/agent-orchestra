'use client';

import { useTranslations } from 'next-intl';
import { useMemo, useState } from 'react';
import { ErrorState } from '@/components/app/error-state';
import { KpiCard } from '@/components/app/kpi-card';
import { MoneyText, DateTimeText } from '@/components/app/data-text';
import { EmptyState } from '@/components/app/empty-state';
import { PageHeader } from '@/components/app/page-header';
import { ProjectChips } from '@/components/app/project-chips';
import { StatusBadge } from '@/components/app/status-badge';
import { Button } from '@/components/ui/button';
import { TableSkeleton } from '@/components/app/table-skeleton';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Link } from '@/i18n/navigation';
import { useProjects, useRuns } from '../queries';

export function RunsList({ title }: { title?: string }) {
  const t = useTranslations('runs');
  const common = useTranslations('common');
  const [project, setProject] = useState<string | null>(null);
  const [status, setStatus] = useState<string | null>(null);
  const [repo, setRepo] = useState<string | null>(null);

  const projects = useProjects();
  const runs = useRuns(project, status, repo);

  const loaded = useMemo(() => runs.data ?? [], [runs.data]);

  const statuses = useMemo(
    () => Array.from(new Set(loaded.map((run) => run.status))).sort(),
    [loaded],
  );

  const repos = useMemo(
    () => Array.from(new Set(loaded.map((run) => run.repo).filter((value): value is string => Boolean(value)))).sort(),
    [loaded],
  );

  const totals = useMemo(
    () => ({
      runs: loaded.length,
      sessions: loaded.reduce((total, run) => total + run.sessions, 0),
      calls: loaded.reduce((total, run) => total + run.calls, 0),
      cost: loaded.reduce((total, run) => total + (run.costUsd ?? 0), 0),
    }),
    [loaded],
  );

  return (
    <div className="space-y-6">
      <PageHeader title={title ?? t('title')} />

      <ProjectChips
        projects={projects.data ?? []}
        selected={project}
        allLabel={common('filters.allProjects')}
        onSelect={setProject}
      />

      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <KpiCard label={t('kpi.runs')} value={totals.runs} />
        <KpiCard label={t('kpi.sessions')} value={totals.sessions} />
        <KpiCard label={t('kpi.calls')} value={totals.calls} />
        <KpiCard label={t('kpi.cost')} value={<MoneyText usd={totals.cost} />} />
      </div>

      <div className="flex flex-wrap items-center gap-2 text-sm">
        <label className="flex items-center gap-2">
          <span className="text-muted-foreground">{t('filters.status')}</span>
          <select
            className="h-8 rounded-md border bg-background px-2"
            value={status ?? ''}
            onChange={(event) => setStatus(event.target.value || null)}
          >
            <option value="">{t('filters.all')}</option>
            {statuses.map((value) => (
              <option key={value} value={value}>
                {value}
              </option>
            ))}
          </select>
        </label>

        <label className="flex items-center gap-2">
          <span className="text-muted-foreground">{t('filters.repo')}</span>
          <select
            className="h-8 rounded-md border bg-background px-2"
            value={repo ?? ''}
            onChange={(event) => setRepo(event.target.value || null)}
          >
            <option value="">{t('filters.all')}</option>
            {repos.map((value) => (
              <option key={value} value={value}>
                {value}
              </option>
            ))}
          </select>
        </label>
      </div>

      {runs.isPending ? <TableSkeleton /> : null}

      {runs.isError ? (
        <ErrorState
          title={common('errors.network')}
          retryLabel={common('actions.retry')}
          onRetry={() => void runs.refetch()}
        />
      ) : null}

      {/* Plan 0009 D15: with no projects the empty state leads to the start page. */}
      {!runs.isPending && !runs.isError && loaded.length === 0 ? (
        <EmptyState
          title={t('empty')}
          action={
            (projects.data?.length ?? 0) === 0 ? (
              <Button asChild size="sm" variant="outline">
                <Link href={'/start/' as never}>{t('noProjects')}</Link>
              </Button>
            ) : undefined
          }
        />
      ) : null}

      {loaded.length > 0 ? (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>{t('columns.plan')}</TableHead>
              <TableHead>{t('columns.repo')}</TableHead>
              <TableHead>{t('columns.status')}</TableHead>
              <TableHead>{t('columns.sessions')}</TableHead>
              <TableHead>{t('columns.calls')}</TableHead>
              <TableHead>{t('columns.cost')}</TableHead>
              <TableHead>{t('columns.updated')}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {loaded.map((run) => (
              <TableRow key={`${run.projectId}.${run.runKey}`}>
                <TableCell className="whitespace-normal">
                  <Link
                    className="font-medium underline-offset-4 hover:underline"
                    href={{
                      pathname: '/runs/view/',
                      query: { project: run.projectId, run: run.runKey },
                    }}
                  >
                    <span data-ltr>{run.runKey}</span>
                  </Link>
                  <p className="text-xs text-muted-foreground">
                    {run.planNumber ? <span data-ltr>{`v${run.planNumber}`}</span> : null}{' '}
                    {run.planTitle ?? run.task ?? ''}
                  </p>
                </TableCell>
                <TableCell data-ltr>{run.repo ?? '—'}</TableCell>
                <TableCell>
                  <StatusBadge status={run.status} />
                </TableCell>
                <TableCell className="tabular-nums" data-ltr>
                  {run.sessions}
                </TableCell>
                <TableCell className="tabular-nums" data-ltr>
                  {run.calls}
                </TableCell>
                <TableCell>
                  <MoneyText usd={run.costUsd} />
                </TableCell>
                <TableCell>
                  <DateTimeText value={run.updatedAt} />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      ) : null}
    </div>
  );
}
