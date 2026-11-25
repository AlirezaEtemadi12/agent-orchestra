'use client';

import { useTranslations } from 'next-intl';
import { usePathname, useRouter } from 'next/navigation';
import { DataTable, Toolbar, type DataTableColumn } from '@/components/app/data-table';
import { DateTimeText, MoneyText } from '@/components/app/data-text';
import { EmptyState } from '@/components/app/empty-state';
import { ErrorState } from '@/components/app/error-state';
import { KpiCard } from '@/components/app/kpi-card';
import { PageHeader } from '@/components/app/page-header';
import { ProjectChips } from '@/components/app/project-chips';
import { StatusBadge } from '@/components/app/status-badge';
import { Button } from '@/components/ui/button';
import { Link } from '@/i18n/navigation';
import { ApiError } from '@/lib/api/api-error';
import { useTableQuery } from '@/lib/gridify/use-table-query';
import { useAppFormatter } from '@/lib/i18n/formatter';
import { useProjects, useRunsSummary, useRunsTable } from '../queries';
import type { RunListItem } from '../types';

type FilterKey = 'project' | 'status' | 'repo';

export function RunsList({ title }: { title?: string }) {
  const t = useTranslations('runs');
  const common = useTranslations('common');
  const format = useAppFormatter();
  const router = useRouter();
  const pathname = usePathname();

  const tableQuery = useTableQuery({ defaultOrderBy: 'updatedAt desc', defaultPageSize: 20 });
  const project = tableQuery.params.project ?? null;
  const status = tableQuery.params.status ?? null;
  const repo = tableQuery.params.repo ?? null;

  const query = {
    project,
    status,
    repo,
    page: tableQuery.state.page,
    pageSize: tableQuery.state.pageSize,
    orderBy: tableQuery.state.orderBy,
    search: tableQuery.state.q,
  };

  const projects = useProjects();
  const table = useRunsTable(query);
  const summary = useRunsSummary(query);
  const summaryValue = summary.data;

  const error = table.error ?? summary.error;
  const retry = () => {
    void table.refetch();
    void summary.refetch();
  };

  // A page-owned filter change resets the page and keeps the hook's own keys.
  const setFilter = (key: FilterKey, value: string | null) => {
    const next = { ...tableQuery.params };

    if (value) {
      next[key] = value;
    } else {
      delete next[key];
    }

    delete next.page;

    const search = new URLSearchParams(next).toString();
    router.push(search ? `${pathname}?${search}` : pathname);
  };

  const empty = (
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
  );

  const columns: readonly DataTableColumn<RunListItem>[] = [
    {
      id: 'runKey',
      header: t('columns.plan'),
      sortable: true,
      cell: (run) => (
        <div className="whitespace-normal">
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
        </div>
      ),
    },
    {
      id: 'repo',
      header: t('columns.repo'),
      sortable: true,
      cell: (run) => <span data-ltr>{run.repo ?? common('notAvailable')}</span>,
    },
    {
      id: 'status',
      header: t('columns.status'),
      sortable: true,
      cell: (run) => <StatusBadge status={run.status} />,
    },
    {
      id: 'sessions',
      header: t('columns.sessions'),
      cell: (run) => (
        <span className="tabular-nums" data-ltr>
          {format.number(run.sessions)}
        </span>
      ),
    },
    {
      id: 'calls',
      header: t('columns.calls'),
      cell: (run) => (
        <span className="tabular-nums" data-ltr>
          {format.number(run.calls)}
        </span>
      ),
    },
    {
      id: 'costUsd',
      header: t('columns.cost'),
      sortable: true,
      cell: (run) => <MoneyText usd={run.costUsd} />,
    },
    {
      id: 'updatedAt',
      header: t('columns.updated'),
      sortable: true,
      cell: (run) => <DateTimeText value={run.updatedAt} />,
    },
  ];

  return (
    <div className="space-y-6">
      <PageHeader title={title ?? t('title')} />

      <ProjectChips
        projects={projects.data ?? []}
        selected={project}
        allLabel={common('filters.allProjects')}
        onSelect={(value) => setFilter('project', value)}
      />

      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
        <KpiCard
          label={t('kpi.runs')}
          value={summaryValue ? format.number(summaryValue.runs) : common('notAvailable')}
        />
        <KpiCard
          label={t('kpi.sessions')}
          value={summaryValue ? format.number(summaryValue.sessions) : common('notAvailable')}
        />
        <KpiCard
          label={t('kpi.calls')}
          value={summaryValue ? format.number(summaryValue.calls) : common('notAvailable')}
        />
        <KpiCard
          label={t('kpi.cost')}
          value={summaryValue ? <MoneyText usd={summaryValue.costUsd} /> : common('notAvailable')}
        />
      </div>

      <Toolbar search={tableQuery.state.q} onSearch={tableQuery.setSearch}>
        <label className="flex items-center gap-2">
          <span className="text-muted-foreground">{t('filters.status')}</span>
          <select
            className="h-8 rounded-md border bg-background px-2"
            value={status ?? ''}
            onChange={(event) => setFilter('status', event.target.value || null)}
          >
            <option value="">{t('filters.all')}</option>
            {(summaryValue?.statuses ?? []).map((value) => (
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
            onChange={(event) => setFilter('repo', event.target.value || null)}
          >
            <option value="">{t('filters.all')}</option>
            {(summaryValue?.repos ?? []).map((value) => (
              <option key={value} value={value}>
                {value}
              </option>
            ))}
          </select>
        </label>
      </Toolbar>

      {error ? (
        <ErrorState
          title={error instanceof ApiError ? error.message : common('errors.network')}
          retryLabel={common('actions.retry')}
          onRetry={retry}
        />
      ) : (
        <DataTable
          columns={columns}
          rows={table.data?.data ?? []}
          count={table.data?.count ?? 0}
          page={tableQuery.state.page}
          pageSize={tableQuery.state.pageSize}
          orderBy={tableQuery.state.orderBy}
          onPageChange={tableQuery.setPage}
          onPageSizeChange={tableQuery.setPageSize}
          onOrderChange={tableQuery.setOrderBy}
          onRetry={retry}
          loading={table.isPending}
          error={null}
          empty={empty}
        />
      )}
    </div>
  );
}
