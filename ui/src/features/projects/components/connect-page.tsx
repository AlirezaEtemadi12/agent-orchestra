'use client';

import { useTranslations } from 'next-intl';
import { useSearchParams } from 'next/navigation';
import { useEffect, useState } from 'react';
import { EmptyState } from '@/components/app/empty-state';
import { PageHeader } from '@/components/app/page-header';
import { TableSkeleton } from '@/components/app/table-skeleton';
import { Button } from '@/components/ui/button';
import { Link } from '@/i18n/navigation';
import { useAppFormatter } from '@/lib/i18n/formatter';
import { useProjects } from '@/lib/projects/queries';
import { copyPrompt } from '../clipboard';
import { useConnect } from '../queries';

/** Plan 0009 §6.6: two pastes and a status that turns green on the first event of this project. */
export function ConnectPage() {
  const t = useTranslations('projects');
  const common = useTranslations('common');
  const format = useAppFormatter();
  const params = useSearchParams();
  const projects = useProjects();
  const id = params.get('id');
  const project = id ? projects.data?.find((item) => item.id === id) : undefined;
  const waiting = project ? !project.lastEventAt : true;
  const connect = useConnect(id);
  const [copied, setCopied] = useState<string | null>(null);
  const refetchProjects = projects.refetch;

  // Plan 0009 D11: lastEventAt lives in the project list, so that is what is polled until the first event.
  useEffect(() => {
    if (!id || !waiting) {
      return;
    }

    const timer = window.setInterval(() => void refetchProjects(), 10_000);

    return () => window.clearInterval(timer);
  }, [id, waiting, refetchProjects]);

  if (id && projects.isPending) {
    return <TableSkeleton rows={4} />;
  }

  if (!id || (projects.data && !project)) {
    return <EmptyState title={common('errors.unexpected')} />;
  }

  const copy = async (key: string, text: string): Promise<void> => {
    const done = await copyPrompt(text);

    setCopied(done ? key : null);
  };

  const data = connect.data;

  return (
    <div className="space-y-6">
      <PageHeader title={t('connect.title', { name: project?.name ?? id })} />

      {connect.isPending || !data ? <TableSkeleton rows={4} /> : null}

      {data ? (
        <div className="space-y-4">
          <Card index={t('connect.block')} copyLabel={copied === 'block' ? t('connect.copied') : t('connect.copy')} onCopy={() => void copy('block', data.reportBlock)}>
            <pre data-ltr className="overflow-auto text-xs whitespace-pre-wrap">
              {data.reportBlock}
            </pre>
            <p className="text-xs text-muted-foreground">{t('connect.orPrompt')}</p>
            <div className="flex justify-end">
              <Button type="button" size="sm" variant="outline" onClick={() => void copy('prompt', data.agentPrompt)}>
                {copied === 'prompt' ? t('connect.copied') : t('connect.copy')}
              </Button>
            </div>
            <pre data-ltr className="overflow-auto text-xs whitespace-pre-wrap">
              {data.agentPrompt}
            </pre>
          </Card>

          <Card index={t('connect.local')} copyLabel={copied === 'keys' ? t('connect.copied') : t('connect.copy')} onCopy={() => void copy('keys', data.localKeys)}>
            <pre data-ltr className="overflow-auto text-xs whitespace-pre-wrap">
              {data.localKeys}
            </pre>
            {!data.hasProfilesPath ? <p className="text-xs text-muted-foreground">{t('connect.noProfilesPath')}</p> : null}
          </Card>

          <div className="space-y-2 rounded-xl border bg-card/40 p-4 text-sm">
            <p className={waiting ? 'text-muted-foreground' : 'font-medium text-emerald-600 dark:text-emerald-400'}>
              {waiting ? t('connect.waiting') : t('connect.connected', { time: project?.lastEventAt ? format.dateTime(project.lastEventAt) : '' })}
            </p>
            <div className="flex flex-wrap gap-2">
              <Button asChild size="sm" variant="outline">
                <Link href={{ pathname: '/board/' } as never}>{t('connect.board')}</Link>
              </Button>
              <Button asChild size="sm" variant="ghost">
                <Link href={'/settings/projects/' as never}>{t('connect.back')}</Link>
              </Button>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}

function Card({
  index,
  copyLabel,
  onCopy,
  children,
}: {
  index: string;
  copyLabel: string;
  onCopy: () => void;
  children: React.ReactNode;
}) {
  return (
    <section className="space-y-2 rounded-xl border bg-card/40 p-4">
      <div className="flex items-start justify-between gap-2">
        <h2 className="text-sm font-medium">{index}</h2>
        <Button type="button" size="sm" variant="outline" onClick={onCopy}>
          {copyLabel}
        </Button>
      </div>
      {children}
    </section>
  );
}
