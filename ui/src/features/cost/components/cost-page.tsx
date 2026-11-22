'use client';

import { useTranslations } from 'next-intl';
import { useMemo, useState } from 'react';
import { BarList } from '@/components/app/bar-list';
import { DayColumns } from '@/components/app/day-columns';
import { DateTimeText, MoneyText, TokensText } from '@/components/app/data-text';
import { EmptyState } from '@/components/app/empty-state';
import { ErrorState } from '@/components/app/error-state';
import { PageHeader } from '@/components/app/page-header';
import { ProjectChips } from '@/components/app/project-chips';
import { TableSkeleton } from '@/components/app/table-skeleton';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { useAppFormatter } from '@/lib/i18n/formatter';
import { useProjects } from '@/lib/projects/queries';
import { useCostSummary } from '../queries';

function monthBounds(month: string): { from: string; to: string } {
  const [year, monthNumber] = month.split('-').map(Number);
  const last = new Date(Date.UTC(year, monthNumber, 0)).getUTCDate();

  return { from: `${month}-01`, to: `${month}-${String(last).padStart(2, '0')}` };
}

function currentMonth(): string {
  const now = new Date();

  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
}

export function CostPage() {
  const t = useTranslations('cost');
  const runs = useTranslations('runs');
  const common = useTranslations('common');
  const formatter = useAppFormatter();
  const projects = useProjects();
  const [project, setProject] = useState<string | null>(null);
  const [month, setMonth] = useState(currentMonth);

  const zone = useMemo(() => Intl.DateTimeFormat().resolvedOptions().timeZone, []);
  const filter = useMemo(() => ({ project, ...monthBounds(month) }), [project, month]);

  const byModel = useCostSummary(filter, 'model', zone);
  const byProject = useCostSummary(filter, 'project', zone);
  const byDay = useCostSummary(filter, 'day', zone);

  const empty =
    !byModel.isPending &&
    !byModel.isError &&
    (byModel.data ?? []).length === 0 &&
    (byProject.data ?? []).length === 0;

  return (
    <div className="space-y-6">
      <PageHeader title={t('title')} description={t('note')} />

      <ProjectChips
        projects={projects.data ?? []}
        selected={project}
        allLabel={common('filters.allProjects')}
        onSelect={setProject}
      />

      <label className="flex w-fit items-center gap-2 text-sm">
        <span className="text-muted-foreground">{t('range')}</span>
        <input
          type="month"
          className="h-8 rounded-md border bg-background px-2"
          value={month}
          onChange={(event) => setMonth(event.target.value || currentMonth())}
          data-ltr
        />
      </label>

      {byModel.isPending ? <TableSkeleton /> : null}

      {byModel.isError ? (
        <ErrorState
          title={common('errors.network')}
          retryLabel={common('actions.retry')}
          onRetry={() => void byModel.refetch()}
        />
      ) : null}

      {empty ? <EmptyState title={common('states.empty')} /> : null}

      {(byModel.data ?? []).length > 0 ? (
        <Card>
          <CardHeader>
            <CardTitle>{t('byModel')}</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <BarList
              items={(byModel.data ?? []).map((row) => ({
                label: row.key,
                value: row.costUsd ?? 0,
                display: <MoneyText usd={row.costUsd} />,
              }))}
            />
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{runs('columns.model')}</TableHead>
                  <TableHead>{runs('columns.sessions')}</TableHead>
                  <TableHead>{runs('columns.calls')}</TableHead>
                  <TableHead>{runs('columns.input')}</TableHead>
                  <TableHead>{runs('columns.output')}</TableHead>
                  <TableHead>{runs('columns.cacheRead')}</TableHead>
                  <TableHead>{runs('columns.cost')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {(byModel.data ?? []).map((row) => (
                  <TableRow key={row.key}>
                    <TableCell data-ltr>{row.key}</TableCell>
                    <TableCell data-ltr>{row.sessions}</TableCell>
                    <TableCell data-ltr>{row.calls}</TableCell>
                    <TableCell>
                      <TokensText value={row.input} />
                    </TableCell>
                    <TableCell>
                      <TokensText value={row.output} />
                    </TableCell>
                    <TableCell>
                      <TokensText value={row.cacheRead} />
                    </TableCell>
                    <TableCell>
                      <MoneyText usd={row.costUsd} />
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      ) : null}

      {(byProject.data ?? []).length > 0 ? (
        <Card>
          <CardHeader>
            <CardTitle>{t('byProject')}</CardTitle>
          </CardHeader>
          <CardContent>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{common('filters.project')}</TableHead>
                  <TableHead>{runs('kpi.runs')}</TableHead>
                  <TableHead>{runs('columns.cost')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {(byProject.data ?? []).map((row) => (
                  <TableRow key={row.key}>
                    <TableCell data-ltr>{row.key}</TableCell>
                    <TableCell data-ltr>{row.runs}</TableCell>
                    <TableCell>
                      <MoneyText usd={row.costUsd} />
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      ) : null}

      {(byDay.data ?? []).length > 0 ? (
        <Card>
          <CardHeader>
            <CardTitle>{t('byDay')}</CardTitle>
          </CardHeader>
          <CardContent>
            <DayColumns
              items={(byDay.data ?? []).map((row) => ({
                label: row.key,
                value: row.costUsd ?? 0,
                display: formatter.money(row.costUsd),
              }))}
            />
            <p className="mt-2 text-xs text-muted-foreground">
              <DateTimeText value={`${month}-01`} />
            </p>
          </CardContent>
        </Card>
      ) : null}
    </div>
  );
}
