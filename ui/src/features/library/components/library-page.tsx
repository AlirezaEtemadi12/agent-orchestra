'use client';

import { useSearchParams } from 'next/navigation';
import { usePathname, useRouter } from 'next/navigation';
import { useTranslations } from 'next-intl';
import {
  DataTable,
  Pagination,
  Toolbar,
  type DataTableColumn,
} from '@/components/app/data-table';
import { DateTimeText } from '@/components/app/data-text';
import { EmptyState } from '@/components/app/empty-state';
import { ErrorState } from '@/components/app/error-state';
import { PageHeader } from '@/components/app/page-header';
import { ProjectChips } from '@/components/app/project-chips';
import { TableSkeleton } from '@/components/app/table-skeleton';
import { Link } from '@/i18n/navigation';
import { ApiError } from '@/lib/api/api-error';
import { useTableQuery } from '@/lib/gridify/use-table-query';
import { useProjects } from '@/lib/projects/queries';
import { cn } from '@/lib/utils';
import {
  LIBRARY_TABS,
  detailQuery,
  statusColumnVisible,
  tabFrom,
  type LibraryTab,
} from '../library-params';
import { useLibraryTable } from '../queries';
import type { LibraryItem } from '../types';
import { EvidenceGallery } from './evidence-gallery';
import { LibraryStatusBadge } from './status';

const PAGE_SIZE = 20;
const EVIDENCE_PAGE_SIZE = 100;

export function LibraryPage() {
  const t = useTranslations('library');
  const common = useTranslations('common');
  const labels = t as unknown as (key: string) => string;
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  const tab = tabFrom(searchParams.get('tab'));
  const tableQuery = useTableQuery({
    defaultOrderBy: 'updatedOn desc',
    defaultPageSize: tab.category === 'Evidence' ? EVIDENCE_PAGE_SIZE : PAGE_SIZE,
  });

  const project = tableQuery.params.project ?? null;
  const showStatus = statusColumnVisible(tab.category);
  const status = showStatus ? (tableQuery.params.status ?? null) : null;

  const library = useLibraryTable({
    project,
    category: tab.category,
    status,
    page: tableQuery.state.page,
    pageSize: tableQuery.state.pageSize,
    orderBy: tableQuery.state.orderBy,
    search: tableQuery.state.q,
  });

  const projects = useProjects();
  const rows = library.data?.data ?? [];
  const count = library.data?.count ?? 0;
  const isPlan = tab.category === 'Plan';
  const isIdea = tab.category === 'Idea';

  const go = (changes: Record<string, string | null>) => {
    const next: Record<string, string> = { ...tableQuery.params };

    for (const [key, value] of Object.entries(changes)) {
      if (value) {
        next[key] = value;
      } else {
        delete next[key];
      }
    }

    delete next.page;

    const search = new URLSearchParams(next).toString();
    router.push(search ? `${pathname}?${search}` : pathname);
  };

  const setTab = (value: LibraryTab) => {
    const changes: Record<string, string | null> = { tab: value.tab };

    if (!statusColumnVisible(value.category)) {
      changes.status = null;
    }

    go(changes);
  };

  const titleCell = (item: LibraryItem) => (
    <div className="whitespace-normal">
      <Link
        href={{ pathname: '/library/view/', query: detailQuery(item.category, item.projectId, item.relPath) }}
        className="font-medium underline-offset-4 hover:underline"
      >
        {item.title}
      </Link>
      <p className="text-xs text-muted-foreground" data-ltr>
        {item.relPath}
      </p>
    </div>
  );

  const columns: DataTableColumn<LibraryItem>[] = [];

  if (isPlan) {
    columns.push(
      {
        id: 'number',
        header: t('columns.number'),
        sortable: true,
        className: 'tabular-nums',
        cell: (item) => <span data-ltr>{item.number ?? common('notAvailable')}</span>,
      },
      { id: 'title', header: t('columns.title'), sortable: true, cell: titleCell },
      {
        id: 'projectName',
        header: t('columns.project'),
        sortable: true,
        cell: (item) => <span data-ltr>{item.projectName}</span>,
      },
      {
        id: 'status',
        header: t('columns.status'),
        sortable: true,
        cell: (item) => <LibraryStatusBadge status={item.status} kind="plan" />,
      },
      {
        id: 'createdOn',
        header: t('columns.created'),
        sortable: true,
        cell: (item) => <DateTimeText value={item.createdOn} />,
      },
      {
        id: 'updatedOn',
        header: t('columns.updated'),
        sortable: true,
        cell: (item) => <DateTimeText value={item.updatedOn} />,
      },
      {
        id: 'doneOn',
        header: t('columns.done'),
        sortable: true,
        cell: (item) => <DateTimeText value={item.doneOn} />,
      },
      {
        id: 'runKey',
        header: t('columns.run'),
        sortable: true,
        cell: (item) =>
          item.runKey ? (
            <Link
              href={{ pathname: '/runs/view/', query: { project: item.projectId, run: item.runKey } }}
              className="underline-offset-4 hover:underline"
            >
              <span data-ltr>{item.runKey}</span>
            </Link>
          ) : (
            <span className="text-muted-foreground">{common('notAvailable')}</span>
          ),
      },
    );
  } else if (isIdea) {
    columns.push(
      { id: 'title', header: t('columns.title'), sortable: true, cell: titleCell },
      {
        id: 'projectName',
        header: t('columns.project'),
        sortable: true,
        cell: (item) => <span data-ltr>{item.projectName}</span>,
      },
      {
        id: 'status',
        header: t('columns.status'),
        sortable: true,
        cell: (item) => <LibraryStatusBadge status={item.status} kind="idea" />,
      },
      {
        id: 'linkedPlan',
        header: t('columns.linkedPlan'),
        cell: (item) => (
          <span data-ltr>
            {item.linkedPlanNumber
              ? `${item.linkedPlanNumber} · ${item.linkedPlanTitle ?? ''}`
              : common('notAvailable')}
          </span>
        ),
      },
      {
        id: 'updatedOn',
        header: t('columns.updated'),
        sortable: true,
        cell: (item) => <DateTimeText value={item.updatedOn ?? item.modifiedAt} />,
      },
    );
  } else {
    columns.push(
      { id: 'title', header: t('columns.title'), sortable: true, cell: titleCell },
      {
        id: 'projectName',
        header: t('columns.project'),
        sortable: true,
        cell: (item) => <span data-ltr>{item.projectName}</span>,
      },
      {
        id: 'updatedOn',
        header: t('columns.updated'),
        sortable: true,
        cell: (item) => (
          <span title={item.updatedOn ? undefined : t('modifiedHint')}>
            <DateTimeText value={item.updatedOn ?? item.modifiedAt} />
          </span>
        ),
      },
    );
  }

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
              onClick={() => setTab(item)}
              className={cn(
                'rounded-md px-3 py-1.5 text-sm transition-colors',
                active ? 'bg-muted font-medium' : 'text-muted-foreground hover:bg-muted/50',
              )}
            >
              {labels(`tabs.${item.tab}`)}
            </button>
          );
        })}
      </div>

      <ProjectChips
        projects={projects.data ?? []}
        selected={project}
        allLabel={common('filters.allProjects')}
        onSelect={(id) => go({ project: id })}
      />

      <Toolbar search={tableQuery.state.q} onSearch={tableQuery.setSearch}>
        {showStatus ? (
          <label className="flex items-center gap-2">
            <span className="text-muted-foreground">{t('columns.status')}</span>
            <select
              className="h-8 rounded-md border bg-background px-2"
              value={status ?? ''}
              onChange={(event) => go({ status: event.target.value || null })}
            >
              <option value="">{t('all')}</option>
              {(isIdea
                ? ['raw', 'ready', 'planned', 'dropped']
                : ['Draft', 'Approved', 'Running', 'Done', 'Dropped', 'Unknown']
              ).map((value) => (
                <option key={value} value={value}>
                  {labels(isIdea ? `ideaStatus.${value}` : `status.${value}`)}
                </option>
              ))}
            </select>
          </label>
        ) : null}
      </Toolbar>

      {tab.category === 'Evidence' ? (
        <>
          {library.isPending ? <TableSkeleton rows={8} /> : null}

          {library.isError ? (
            <ErrorState
              title={library.error instanceof ApiError ? library.error.message : common('errors.network')}
              retryLabel={common('actions.retry')}
              onRetry={() => void library.refetch()}
            />
          ) : null}

          {!library.isPending && !library.isError && rows.length === 0 ? (
            <EmptyState title={t('empty')} />
          ) : null}

          {rows.length > 0 ? <EvidenceGallery items={rows} /> : null}

          {count > EVIDENCE_PAGE_SIZE ? (
            <Pagination
              page={tableQuery.state.page}
              pageSize={tableQuery.state.pageSize}
              count={count}
              onPageChange={tableQuery.setPage}
              onPageSizeChange={tableQuery.setPageSize}
            />
          ) : null}
        </>
      ) : (
        <DataTable
          columns={columns}
          rows={rows}
          count={count}
          page={tableQuery.state.page}
          pageSize={tableQuery.state.pageSize}
          orderBy={tableQuery.state.orderBy}
          onPageChange={tableQuery.setPage}
          onPageSizeChange={tableQuery.setPageSize}
          onOrderChange={tableQuery.setOrderBy}
          onRetry={() => void library.refetch()}
          loading={library.isPending}
          error={library.error}
          empty={<EmptyState title={t('empty')} />}
        />
      )}
    </div>
  );
}
