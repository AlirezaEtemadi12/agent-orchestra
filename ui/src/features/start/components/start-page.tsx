'use client';

import { useTranslations } from 'next-intl';
import type { ReactNode } from 'react';
import { PageHeader } from '@/components/app/page-header';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Link } from '@/i18n/navigation';
import { startSteps } from '../start-steps';
import { useProjects } from '../queries';

/** Plan 0012 D16: the dashboard runs, then the first project is set up; no hosting question. */
export function StartPage() {
  const t = useTranslations('start');
  const projects = useProjects();

  const state = startSteps({ projects: projects.data ?? [] });

  return (
    <div className="space-y-6">
      <PageHeader title={t('title')} />

      <ol className="grid gap-4">
        <Step index={1} title={t('running')} done labels={{ done: t('done'), todo: t('todo') }} />

        <Step
          index={2}
          title={t('firstProject')}
          done={state.connected}
          labels={{ done: t('done'), todo: t('todo') }}
          action={
            <Button asChild size="sm" variant="outline">
              <Link href={'/settings/projects/edit/' as never}>{t('action')}</Link>
            </Button>
          }
        >
          <p className="text-xs text-muted-foreground">{t('body')}</p>
          {state.pending.length > 0 ? (
            <ul className="space-y-1 text-sm">
              {state.pending.map((project) => (
                <li key={project.id}>
                  <Link
                    className="underline-offset-4 hover:underline"
                    href={{ pathname: '/settings/projects/setup/', query: { id: project.id } } as never}
                  >
                    {project.id}
                  </Link>
                </li>
              ))}
            </ul>
          ) : null}
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
