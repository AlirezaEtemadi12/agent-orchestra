'use client';

import { useTranslations } from 'next-intl';
import { EmptyState } from '@/components/app/empty-state';
import { ErrorState } from '@/components/app/error-state';
import { PageHeader } from '@/components/app/page-header';
import { TableSkeleton } from '@/components/app/table-skeleton';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Link } from '@/i18n/navigation';
import { useProfiles } from '../queries';

export function ProfilesPage() {
  const t = useTranslations('profiles');
  const common = useTranslations('common');
  const profiles = useProfiles();
  const rows = profiles.data ?? [];

  return (
    <div className="space-y-6">
      <PageHeader title={t('title')} />

      {profiles.isPending ? <TableSkeleton rows={3} /> : null}

      {profiles.isError ? (
        <ErrorState
          title={common('errors.network')}
          retryLabel={common('actions.retry')}
          onRetry={() => void profiles.refetch()}
        />
      ) : null}

      {!profiles.isPending && !profiles.isError && rows.length === 0 ? (
        <EmptyState title={common('states.empty')} />
      ) : null}

      {rows.length > 0 ? (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>id</TableHead>
              <TableHead>{t('history')}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {rows.map((profile) => (
              <TableRow key={profile.id}>
                <TableCell className="font-medium">
                  <Link
                    className="underline underline-offset-4"
                    data-ltr
                    href={{ pathname: '/settings/profiles/edit/', query: { id: profile.id } } as never}
                  >
                    {profile.id}
                  </Link>
                </TableCell>
                <TableCell data-ltr>{t('version', { n: profile.currentVersion })}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      ) : null}
    </div>
  );
}
