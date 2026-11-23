'use client';

import { useTranslations } from 'next-intl';
import type { ReactNode } from 'react';
import { PageHeader } from '@/components/app/page-header';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Link } from '@/i18n/navigation';
import { startSteps } from '../start-steps';
import { useHosting, useProjects, useStartRuns } from '../queries';

/** Plan 0009 D15: five steps, each with a mark computed from what the dashboard can see right now. */
export function StartPage() {
  const t = useTranslations('start');
  const projects = useProjects();
  const hosting = useHosting();
  const runs = useStartRuns();

  const state = startSteps({
    hosting: hosting.data,
    projects: projects.data ?? [],
    runCount: runs.data?.length ?? 0,
  });

  const docker = state.docker;

  return (
    <div className="space-y-6">
      <PageHeader title={t('title')} />

      <ol className="grid gap-4">
        <Step index={1} title={t('s1.title')} done={state.dashboard} labels={{ done: t('done'), todo: t('todo') }}>
          <p className="text-xs text-muted-foreground" data-ltr>
            {docker ? t('s1.docker') : t('s1.iis')}
          </p>
        </Step>

        <Step
          index={2}
          title={t('s2.title')}
          done={state.project}
          labels={{ done: t('done'), todo: t('todo') }}
          action={
            <Button asChild size="sm" variant="outline">
              <Link href={'/settings/projects/edit/' as never}>{t('s2.action')}</Link>
            </Button>
          }
        >
          <p className="text-xs text-muted-foreground">{t('s2.body')}</p>
        </Step>

        <Step index={3} title={t('s3.title')} done={state.connected} labels={{ done: t('done'), todo: t('todo') }}>
          <p className="text-xs text-muted-foreground">{t('s3.body')}</p>
          {state.pending.length > 0 ? (
            <ul className="space-y-1 text-sm">
              {state.pending.map((project) => (
                <li key={project.id}>
                  <Link
                    className="underline-offset-4 hover:underline"
                    href={{ pathname: '/settings/projects/connect/', query: { id: project.id } } as never}
                  >
                    {project.id}
                  </Link>
                </li>
              ))}
            </ul>
          ) : null}
        </Step>

        <Step index={4} title={t('s4.title')} done={state.run} labels={{ done: t('done'), todo: t('todo') }}>
          <p className="text-xs text-muted-foreground">{t('s4.body')}</p>
        </Step>

        <Step index={5} title={t('s5.title')} labels={{ done: t('done'), todo: t('todo') }}>
          <p className="text-xs text-muted-foreground">{t('s5.body')}</p>
        </Step>
      </ol>
    </div>
  );
}

function Step({
  index,
  title,
  done,
  labels,
  action,
  children,
}: {
  index: number;
  title: string;
  done?: boolean;
  labels: { done: string; todo: string };
  action?: ReactNode;
  children?: ReactNode;
}) {
  return (
    <li className="space-y-2 rounded-xl border bg-card/40 p-4">
      <div className="flex items-center justify-between gap-2">
        <h2 className="text-sm font-medium">
          <span data-ltr className="me-2 text-muted-foreground">
            {index}
          </span>
          {title}
        </h2>
        {done === undefined ? null : (
          <Badge variant={done ? 'secondary' : 'outline'}>{done ? labels.done : labels.todo}</Badge>
        )}
        {action}
      </div>
      {children}
    </li>
  );
}
