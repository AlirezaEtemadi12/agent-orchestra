'use client';

import { useTranslations } from 'next-intl';
import { useEffect, useState } from 'react';
import { PageHeader } from '@/components/app/page-header';
import { Button } from '@/components/ui/button';
import { Link } from '@/i18n/navigation';
import { useAppFormatter } from '@/lib/i18n/formatter';
import { useProjects } from '@/lib/projects/queries';
import { copyPrompt } from '../clipboard';
import type { SetupPlan, SetupResult } from '../types';

type Props = {
  result: SetupResult;
  plan: SetupPlan | null;
};

/** Plan 0012 §6.6: the same rows with their mark, the first-event wait, and the knowledge prompt. */
export function SetupResultView({ result, plan }: Props) {
  const t = useTranslations('projects');
  const format = useAppFormatter();
  const projects = useProjects();
  const [copied, setCopied] = useState(false);

  const project = projects.data?.find((item) => item.id === result.project.id);
  const waiting = !project?.lastEventAt;
  const refetch = projects.refetch;

  // Plan 0012 D11: the project list carries lastEventAt, so that is what is polled until the first event.
  useEffect(() => {
    if (!waiting) {
      return;
    }

    const timer = window.setInterval(() => void refetch(), 10_000);

    return () => window.clearInterval(timer);
  }, [waiting, refetch]);

  const paths = new Map((plan?.items ?? []).map((item) => [item.id, item.path]));

  const errorLabel = (code: string | null): string | null => {
    switch (code?.split('.').pop()) {
      case 'WriteFailed':
        return t('setup.errors.WriteFailed');
      case 'GitInitFailed':
        return t('setup.errors.GitInitFailed');
      case 'InvalidJson':
        return t('setup.errors.InvalidJson');
      case 'Skipped':
        return t('setup.errors.Skipped');
      default:
        return null;
    }
  };

  const copy = async (): Promise<void> => {
    setCopied(await copyPrompt(result.knowledgePrompt));
  };

  // D14: the server answers plain text when there is nothing to fill, and that box is not shown.
  const showKnowledge =
    result.knowledgePrompt.trim().length > 0 && !result.knowledgePrompt.startsWith('Nothing to create');

  return (
    <div className="space-y-6">
      <PageHeader title={t('setup.title', { name: result.project.name })} />

      <ul className="divide-y overflow-hidden rounded-xl border bg-card/40">
        {result.items.map((item) => (
          <li key={item.id} className="space-y-1 p-3 text-sm">
            <div className="flex items-start justify-between gap-2">
              <span data-ltr className="truncate font-mono text-xs">
                {paths.get(item.id) ?? item.id}
              </span>
              <span
                className={item.ok ? 'text-emerald-600 dark:text-emerald-400' : 'text-destructive'}
              >
                {item.ok ? '✓' : (errorLabel(item.error) ?? item.error ?? '')}
              </span>
            </div>
            {item.detail ? (
              <p data-ltr className="text-xs text-muted-foreground">
                {item.detail}
              </p>
            ) : null}
          </li>
        ))}
      </ul>

      <p
        className={
          waiting
            ? 'text-sm text-muted-foreground'
            : 'text-sm font-medium text-emerald-600 dark:text-emerald-400'
        }
      >
        {waiting
          ? t('setup.waiting')
          : t('setup.connected', {
              time: project?.lastEventAt ? format.dateTime(project.lastEventAt) : '',
            })}
      </p>

      {showKnowledge ? (
        <details className="rounded-xl border bg-card/40 p-4">
          <summary className="cursor-pointer text-sm font-medium">{t('setup.knowledge')}</summary>
          <pre
            data-ltr
            className="mt-3 max-h-80 overflow-auto rounded-lg border bg-muted/40 p-3 text-xs whitespace-pre-wrap"
          >
            {result.knowledgePrompt}
          </pre>
          <div className="mt-2 flex justify-end">
            <Button type="button" size="sm" variant="outline" onClick={() => void copy()}>
              {copied ? t('copied') : t('copy')}
            </Button>
          </div>
        </details>
      ) : null}

      <div className="flex flex-wrap gap-2">
        <Button asChild size="sm">
          <Link href={'/settings/projects/' as never}>{t('setup.back')}</Link>
        </Button>
      </div>
    </div>
  );
}
