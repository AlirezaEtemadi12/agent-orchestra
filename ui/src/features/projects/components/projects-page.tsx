'use client';

import { useTranslations } from 'next-intl';
import { useState } from 'react';
import { toast } from 'sonner';
import { EmptyState } from '@/components/app/empty-state';
import { ErrorState } from '@/components/app/error-state';
import { PageHeader } from '@/components/app/page-header';
import { TableSkeleton } from '@/components/app/table-skeleton';
import { Button } from '@/components/ui/button';
import { Link } from '@/i18n/navigation';
import { useProjects, type ProjectDto } from '@/lib/projects/queries';
import { ConfirmDialog } from './confirm-dialog';
import { ProjectsTable } from './projects-table';
import { useRemoveProject, useStoreState } from '../queries';

export function ProjectsPage() {
  const t = useTranslations('projects');
  const common = useTranslations('common');
  const projects = useProjects();
  const store = useStoreState();
  const remove = useRemoveProject();
  const [pending, setPending] = useState<ProjectDto | null>(null);

  const rows = projects.data ?? [];

  const confirm = async (): Promise<void> => {
    if (!pending) {
      return;
    }

    await remove.mutateAsync(pending.id);

    toast.success(t('removed'));
    setPending(null);
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('title')}
        actions={
          <Button asChild size="sm">
            <Link href={'/settings/projects/edit/' as never}>{t('new')}</Link>
          </Button>
        }
      />

      {store.data && !store.data.readable ? (
        <ErrorState
          title={t('storeUnreadable')}
          retryLabel={common('actions.retry')}
          onRetry={() => void store.refetch()}
        />
      ) : null}

      {projects.isPending ? <TableSkeleton rows={4} /> : null}

      {projects.isError ? (
        <ErrorState
          title={common('errors.network')}
          retryLabel={common('actions.retry')}
          onRetry={() => void projects.refetch()}
        />
      ) : null}

      {!projects.isPending && !projects.isError && rows.length === 0 ? (
        <EmptyState title={common('states.noProjects')} />
      ) : null}

      {rows.length > 0 ? <ProjectsTable rows={rows} onRemove={setPending} /> : null}

      <ConfirmDialog
        open={pending !== null}
        title={pending ? t('removeTitle', { name: pending.name }) : ''}
        body={t('removeBody')}
        confirmLabel={t('remove')}
        cancelLabel={common('actions.back')}
        onOpenChange={(open) => {
          if (!open) {
            setPending(null);
          }
        }}
        onConfirm={() => void confirm()}
      />
    </div>
  );
}
