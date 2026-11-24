'use client';

import { useTranslations } from 'next-intl';
import { useSearchParams } from 'next/navigation';
import { useEffect, useMemo, useState } from 'react';
import { EmptyState } from '@/components/app/empty-state';
import { PageHeader } from '@/components/app/page-header';
import { TableSkeleton } from '@/components/app/table-skeleton';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useRouter } from '@/i18n/navigation';
import { useProjects } from '@/lib/projects/queries';
import { parseRepos } from '../project-schema';
import { errorText } from '../projects-errors';
import { useSetupApply, useSetupPlan } from '../queries';
import type { SetupAction, SetupPlan, SetupRequest, SetupResult } from '../types';
import { SetupResultView } from './setup-result';

/** Plan 0012 §6.5: the checklist the setup step shows, and the one button that writes it. */
export function SetupStep() {
  const t = useTranslations('projects');
  const common = useTranslations('common');
  const params = useSearchParams();
  const router = useRouter();
  const projects = useProjects();
  const plan = useSetupPlan();
  const apply = useSetupApply();
  const { mutateAsync: askPlan } = plan;
  const { mutateAsync: applySetup } = apply;

  const idParam = params.get('id');
  const nameParam = params.get('name') ?? '';
  const rootParam = params.get('rootPath') ?? '';
  const reposParam = params.get('repos') ?? '';
  const existing = idParam ? projects.data?.find((project) => project.id === idParam) : undefined;

  const target = useMemo(() => {
    if (existing) {
      return {
        id: existing.id,
        name: existing.name,
        rootPath: existing.rootPath ?? '',
        repos: existing.repos.length > 0 ? existing.repos : ['.'],
      };
    }

    const repos = parseRepos(reposParam);

    return {
      id: idParam ?? '',
      name: nameParam,
      rootPath: rootParam,
      repos: repos.length > 0 ? repos : ['.'],
    };
  }, [existing, idParam, nameParam, reposParam, rootParam]);

  const [state, setState] = useState<SetupPlan | null>(null);
  const [skip, setSkip] = useState<string[]>([]);
  const [commands, setCommands] = useState<Record<string, string> | null>(null);
  const [result, setResult] = useState<SetupResult | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    if (target.rootPath.length === 0 || (idParam !== null && projects.isPending)) {
      return;
    }

    let cancelled = false;

    const run = async (): Promise<void> => {
      const body: SetupRequest = {
        rootPath: target.rootPath,
        name: target.name,
        id: target.id,
        repos: target.repos,
        verifyCommands: toLists(commands),
        skip: skip.length > 0 ? skip : null,
      };

      try {
        const next = await askPlan(body);

        if (cancelled) {
          return;
        }

        setState(next);
        setMessage(null);

        if (commands === null) {
          setCommands(
            Object.fromEntries(
              Object.entries(next.verifyCommands).map(([repo, list]) => [repo, list.join('\n')]),
            ),
          );
        }
      } catch (error) {
        if (!cancelled) {
          setMessage(errorText(error, common('errors.unexpected')));
        }
      }
    };

    // Plan 0012 D11: a tick redraws at once; a command waits 400 ms so typing does not plan on every key.
    const timer = window.setTimeout(() => void run(), commands === null ? 0 : 400);

    return () => {
      cancelled = true;
      window.clearTimeout(timer);
    };
  }, [askPlan, commands, common, idParam, projects.isPending, skip, target]);

  const runApply = async (): Promise<void> => {
    setMessage(null);

    try {
      setResult(
        await applySetup({
          rootPath: target.rootPath,
          name: target.name,
          id: target.id,
          repos: target.repos,
          verifyCommands: toLists(commands),
          skip: skip.length > 0 ? skip : null,
        }),
      );
    } catch (error) {
      setMessage(errorText(error, common('errors.unexpected')));
    }
  };

  if (idParam !== null && projects.isPending) {
    return <TableSkeleton rows={5} />;
  }

  if (target.rootPath.length === 0) {
    return <EmptyState title={common('errors.unexpected')} />;
  }

  if (result) {
    return <SetupResultView result={result} plan={state} />;
  }

  const remaining = (state?.items ?? []).filter(
    (item) => item.action !== 'exists' && !skip.includes(item.id),
  ).length;

  const actionLabel = (action: SetupAction): string => {
    switch (action) {
      case 'create':
        return t('setup.action.create');
      case 'add':
        return t('setup.action.add');
      case 'update':
        return t('setup.action.update');
      default:
        return t('setup.action.exists');
    }
  };

  return (
    <div className="space-y-6">
      <PageHeader title={t('setup.title', { name: target.name || target.id })} />

      {state === null ? <TableSkeleton rows={5} /> : null}

      {state ? (
        <div className="space-y-4">
          <ul className="divide-y overflow-hidden rounded-xl border bg-card/40">
            {state.items.map((item) => {
              const checked = !skip.includes(item.id);

              return (
                <li key={item.id} className="flex items-start gap-3 p-3 text-sm">
                  {item.optional ? (
                    <input
                      type="checkbox"
                      className="mt-1"
                      aria-label={item.path}
                      checked={checked}
                      onChange={() =>
                        setSkip(
                          checked ? [...skip, item.id] : skip.filter((id) => id !== item.id),
                        )
                      }
                    />
                  ) : (
                    <span className="mt-0.5 w-4 text-muted-foreground">•</span>
                  )}

                  <div className="min-w-0 flex-1 space-y-1">
                    <span data-ltr className="block truncate font-mono text-xs">
                      {item.path}
                    </span>
                    {item.detail ? (
                      <p data-ltr className="text-xs text-muted-foreground">
                        {item.detail}
                      </p>
                    ) : null}
                  </div>

                  <Badge variant={item.action === 'exists' ? 'secondary' : 'outline'}>
                    {actionLabel(item.action)}
                  </Badge>
                </li>
              );
            })}
          </ul>

          {Object.keys(state.verifyCommands).length > 0 ? (
            <div className="space-y-2 rounded-xl border bg-card/40 p-4">
              <h2 className="text-sm font-medium">{t('setup.commands')}</h2>
              {Object.keys(state.verifyCommands).map((repo) => (
                <label key={repo} className="block space-y-1 text-sm">
                  <span data-ltr className="text-xs text-muted-foreground">
                    {repo}
                  </span>
                  <textarea
                    data-ltr
                    className="min-h-20 w-full rounded-md border bg-background p-2 font-mono text-xs"
                    value={commands?.[repo] ?? ''}
                    onChange={(event) =>
                      setCommands({ ...(commands ?? {}), [repo]: event.target.value })
                    }
                  />
                </label>
              ))}
            </div>
          ) : null}

          <div className="flex flex-wrap items-center gap-2">
            <Button
              type="button"
              disabled={apply.isPending || !state.engineFound || remaining === 0}
              onClick={() => void runApply()}
            >
              {t('setup.apply')}
            </Button>
            <Button
              type="button"
              variant="ghost"
              onClick={() => router.push('/settings/projects/' as never)}
            >
              {t('setup.back')}
            </Button>
            {!state.engineFound ? (
              <span className="text-sm text-destructive">{t('setup.noEngine')}</span>
            ) : null}
            {state.engineFound && remaining === 0 ? (
              <span className="text-sm text-muted-foreground">{t('setup.nothing')}</span>
            ) : null}
          </div>

          {message ? (
            <p role="alert" className="text-sm text-destructive">
              {message}
            </p>
          ) : null}
        </div>
      ) : null}
    </div>
  );
}

/** The textareas hold one command per line; the API takes one list per repo, blank lines dropped. */
export function toLists(commands: Record<string, string> | null): Record<string, string[]> | null {
  if (commands === null) {
    return null;
  }

  return Object.fromEntries(
    Object.entries(commands).map(([repo, text]) => [
      repo,
      text
        .split('\n')
        .map((line) => line.trim())
        .filter((line) => line.length > 0),
    ]),
  );
}
