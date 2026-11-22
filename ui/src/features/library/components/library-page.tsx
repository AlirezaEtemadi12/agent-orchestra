'use client';

import { useTranslations } from 'next-intl';
import { useSearchParams } from 'next/navigation';
import { useState } from 'react';
import { DateTimeText } from '@/components/app/data-text';
import { EmptyState } from '@/components/app/empty-state';
import { ErrorState } from '@/components/app/error-state';
import { PageHeader } from '@/components/app/page-header';
import { ProjectChips } from '@/components/app/project-chips';
import { TableSkeleton } from '@/components/app/table-skeleton';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Link, useRouter } from '@/i18n/navigation';
import { useProjects } from '@/lib/projects/queries';
import { cn } from '@/lib/utils';
import {
  LIBRARY_TABS,
  SORTS,
  detailQuery,
  dirFrom,
  libraryHref,
  sortFrom,
  statusColumnVisible,
  tabFrom,
  tabQuery,
} from '../library-params';
import { useLibrary } from '../queries';
import type { LibraryItem } from '../types';
import { EvidenceGallery } from './evidence-gallery';
import { LibraryStatusBadge } from './status';

export function LibraryPage() {
  const t = useTranslations('library');
  const common = useTranslations('common');
  const columns = t as unknown as (key: string) => string;
  const params = useSearchParams();
  const router = useRouter();

  const tab = tabFrom(params.get('tab'));
  const project = params.get('project');
  const sort = sortFrom(params.get('sort'));
  const dir = dirFrom(params.get('dir'));
  const [status, setStatus] = useState<string | null>(null);

  const showStatus = statusColumnVisible(tab.category);
  const items = useLibrary({
    project,
    category: tab.category,
    status: showStatus ? status : null,
    sort,
    dir,
  });
  const projects = useProjects();
  const rows = items.data ?? [];

  const go = (query: Parameters<typeof libraryHref>[0]): void => {
    router.replace(libraryHref(query) as never);
  };

  const isPlan = tab.category === 'Plan';
  const isIdea = tab.category === 'Idea';

  return (
    <div className="space-y-6">
      <PageHeader title={t('title')} />

      <div className="flex flex-wrap gap-1 border-b pb-2">
        {LIBRARY_TABS.map((item) => {
          const active = item.tab === tab.tab;

          return (
            <button
              key={item.tab}
              type="button"
              aria-current={active ? 'page' : undefined}
              onClick={() => go(tabQuery(item, project, sort, dir))}
              className={cn(
                'rounded-md px-3 py-1.5 text-sm transition-colors',
                active ? 'bg-muted font-medium' : 'text-muted-foreground hover:bg-muted/50',
              )}
            >
              {columns(`tabs.${item.tab}`)}
            </button>
          );
        })}
      </div>

      <ProjectChips
        projects={projects.data ?? []}
        selected={project}
        allLabel={common('filters.allProjects')}
        onSelect={(id) => go(tabQuery(tab, id, sort, dir))}
      />

      <div className="flex flex-wrap items-center gap-2 text-sm">
        {showStatus ? (
          <label className="flex items-center gap-2">
            <span className="text-muted-foreground">{t('columns.status')}</span>
            <select
              className="h-8 rounded-md border bg-background px-2"
              value={status ?? ''}
              onChange={(event) => setStatus(event.target.value || null)}
            >
              <option value="">{t('all')}</option>
              {(isIdea
                ? ['raw', 'ready', 'planned', 'dropped']
                : ['Draft', 'Approved', 'Running', 'Done', 'Dropped', 'Unknown']
              ).map((value) => (
                <option key={value} value={value}>
                  {columns(isIdea ? `ideaStatus.${value}` : `status.${value}`)}
                </option>
              ))}
            </select>
          </label>
        ) : null}

        <label className="flex items-center gap-2">
          <span className="text-muted-foreground">{t('sort.label')}</span>
          <select
            className="h-8 rounded-md border bg-background px-2"
            value={sort}
            onChange={(event) => go(tabQuery(tab, project, sortFrom(event.target.value), dir))}
          >
            {SORTS.map((key) => (
              <option key={key} value={key}>
                {columns(`columns.${key}`)}
              </option>
            ))}
          </select>
        </label>

        <label className="flex items-center gap-2">
          <span className="sr-only">{t('sort.label')}</span>
          <select
            className="h-8 rounded-md border bg-background px-2"
            value={dir}
            onChange={(event) => go(tabQuery(tab, project, sort, event.target.value))}
          >
            <option value="desc">↓</option>
            <option value="asc">↑</option>
          </select>
        </label>
      </div>

      {items.isPending ? <TableSkeleton rows={8} /> : null}

      {items.isError ? (
        <ErrorState
          title={common('errors.network')}
          retryLabel={common('actions.retry')}
          onRetry={() => void items.refetch()}
        />
      ) : null}

      {!items.isPending && !items.isError && rows.length === 0 ? <EmptyState title={t('empty')} /> : null}

      {rows.length > 0 && tab.category === 'Evidence' ? <EvidenceGallery items={rows} /> : null}

      {rows.length > 0 && tab.category !== 'Evidence' ? (
        <Table>
          <TableHeader>
            <TableRow>
              {isPlan ? <TableHead>{t('columns.number')}</TableHead> : null}
              <TableHead>{t('columns.title')}</TableHead>
              <TableHead>{t('columns.project')}</TableHead>
              {showStatus ? <TableHead>{t('columns.status')}</TableHead> : null}
              {isIdea ? <TableHead>{t('columns.linkedPlan')}</TableHead> : null}
              {isPlan ? (
                <>
                  <TableHead>{t('columns.created')}</TableHead>
                  <TableHead>{t('columns.updated')}</TableHead>
                  <TableHead>{t('columns.done')}</TableHead>
                  <TableHead>{t('columns.run')}</TableHead>
                </>
              ) : (
                <TableHead>{t('columns.updated')}</TableHead>
              )}
            </TableRow>
          </TableHeader>
          <TableBody>
            {rows.map((item) => (
              <LibraryRow key={`${item.projectId}.${item.category}.${item.relPath}`} item={item} kind={isIdea ? 'idea' : 'plan'} />
            ))}
          </TableBody>
        </Table>
      ) : null}
    </div>
  );
}

function LibraryRow({ item, kind }: { item: LibraryItem; kind: 'plan' | 'idea' }) {
  const t = useTranslations('library');
  const isPlan = item.category === 'Plan';

  return (
    <TableRow>
      {isPlan ? (
        <TableCell data-ltr className="tabular-nums">
          {item.number ?? '—'}
        </TableCell>
      ) : null}

      <TableCell className="whitespace-normal">
        <Link
          href={{ pathname: '/library/view/', query: detailQuery(item.category, item.projectId, item.relPath) }}
          className="font-medium underline-offset-4 hover:underline"
        >
          {item.title}
        </Link>
        <p className="text-xs text-muted-foreground" data-ltr>
          {item.relPath}
        </p>
      </TableCell>

      <TableCell data-ltr>{item.projectName}</TableCell>

      {item.category === 'Plan' || item.category === 'Idea' ? (
        <TableCell>
          <LibraryStatusBadge status={item.status} kind={kind} />
        </TableCell>
      ) : null}

      {item.category === 'Idea' ? (
        <TableCell data-ltr>
          {item.linkedPlanNumber ? `${item.linkedPlanNumber} · ${item.linkedPlanTitle ?? ''}` : '—'}
        </TableCell>
      ) : null}

      {isPlan ? (
        <>
          <TableCell>
            <DateTimeText value={item.createdOn} />
          </TableCell>
          <TableCell>
            <DateTimeText value={item.updatedOn} />
          </TableCell>
          <TableCell>
            <DateTimeText value={item.doneOn} />
          </TableCell>
          <TableCell>
            {item.runKey ? (
              <Link
                href={{ pathname: '/runs/view/', query: { project: item.projectId, run: item.runKey } }}
                className="underline-offset-4 hover:underline"
              >
                <span data-ltr>{item.runKey}</span>
              </Link>
            ) : (
              <span className="text-muted-foreground">—</span>
            )}
          </TableCell>
        </>
      ) : (
        <TableCell title={item.updatedOn ? undefined : t('modifiedHint')}>
          <DateTimeText value={item.updatedOn ?? item.modifiedAt} />
        </TableCell>
      )}
    </TableRow>
  );
}
