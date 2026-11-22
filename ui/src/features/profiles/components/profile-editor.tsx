'use client';

import { useTranslations } from 'next-intl';
import { useSearchParams } from 'next/navigation';
import { useState } from 'react';
import { toast } from 'sonner';
import { ErrorState } from '@/components/app/error-state';
import { PageHeader } from '@/components/app/page-header';
import { TableSkeleton } from '@/components/app/table-skeleton';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { useProfile, useRevertProfile, useSaveProfile } from '../queries';
import { parseProfile, section, type ProfileConfig } from '../profile-form';
import type { ProfileVersion } from '../types';
import { ProfileHistory } from './profile-history';
import { ReasonDialog } from './reason-dialog';
import { WorkflowSteps } from './workflow-steps';

type Tab = 'roles' | 'tiers' | 'adapters' | 'workflow' | 'json';

const TABS: Tab[] = ['roles', 'tiers', 'adapters', 'workflow', 'json'];

const INPUT = 'h-8 w-full rounded-md border bg-background px-2 text-xs disabled:opacity-60';

function withoutNotes(block: Record<string, unknown>): string[] {
  return Object.keys(block).filter((name) => name !== 'note' && name !== 'description');
}

export function ProfileEditor() {
  const common = useTranslations('common');
  const params = useSearchParams();
  const id = params.get('id') ?? 'default';
  const profile = useProfile(id);

  if (profile.isPending) {
    return <TableSkeleton rows={8} />;
  }

  if (profile.isError || !profile.data) {
    return (
      <ErrorState
        title={common('errors.network')}
        retryLabel={common('actions.retry')}
        onRetry={() => void profile.refetch()}
      />
    );
  }

  // One editor state per loaded version: a refetch of the same version keeps the draft, a save (a new
  // version) starts clean.
  return <EditorFields key={`${id}.${profile.data.version}`} id={id} loaded={profile.data} />;
}

type FieldsProps = {
  id: string;
  loaded: ProfileVersion;
};

function EditorFields({ id, loaded }: FieldsProps) {
  const t = useTranslations('profiles');
  const common = useTranslations('common');
  const save = useSaveProfile(id);
  const revert = useRevertProfile(id);

  const parsed = parseProfile(loaded.json);

  const [draft, setDraft] = useState<ProfileConfig>(parsed.value ?? {});
  const [raw, setRaw] = useState(JSON.stringify(parsed.value ?? {}, null, 2));
  const [rawError, setRawError] = useState<string | null>(parsed.error);
  const [tab, setTab] = useState<Tab>('roles');
  const [asking, setAsking] = useState(false);

  const roles = section(draft, 'roles');
  const tiers = section(draft, 'tiers');
  const adapters = section(draft, 'adapters');

  const setBlock = (key: string, value: Record<string, unknown>): void => {
    setDraft({ ...draft, [key]: value });
  };

  const chainOf = (role: string): Record<string, unknown>[] => {
    const spec = roles[role];
    const chain = spec !== null && typeof spec === 'object' ? (spec as Record<string, unknown>).chain : null;

    return Array.isArray(chain) ? (chain as Record<string, unknown>[]) : [];
  };

  const setChain = (role: string, chain: Record<string, unknown>[]): void => {
    const spec = roles[role];

    setBlock('roles', {
      ...roles,
      [role]: { ...(spec !== null && typeof spec === 'object' ? spec : {}), chain },
    });
  };

  const updateHop = (role: string, index: number, field: string, value: string): void => {
    const chain = chainOf(role).map((hop, position) =>
      position === index ? { ...hop, [field]: value } : hop,
    );

    setChain(role, chain);
  };

  const moveHop = (role: string, index: number, by: number): void => {
    const chain = [...chainOf(role)];
    const target = index + by;

    if (target < 0 || target >= chain.length) {
      return;
    }

    [chain[index], chain[target]] = [chain[target], chain[index]];
    setChain(role, chain);
  };

  const saveNow = async (reason: string): Promise<void> => {
    let payload = draft;

    if (tab === 'json') {
      const next = parseProfile(raw);

      if (!next.value) {
        setRawError(next.error);

        return;
      }

      payload = next.value;
      setDraft(next.value);
    }

    try {
      await save.mutateAsync({ json: JSON.stringify(payload), reason });
      setAsking(false);
      toast.success(t('exported'));
    } catch {
      toast.error(common('errors.unexpected'));
    }
  };

  const revertTo = (version: number): void => {
    revert.mutate(version, {
      onSuccess: () => toast.success(t('exported')),
      onError: () => toast.error(common('errors.unexpected')),
    });
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title={`${t('title')} · ${id}`}
        description={t('version', { n: loaded.version })}
        actions={
          <Button type="button" size="sm" onClick={() => setAsking(true)}>
            {t('save')}
          </Button>
        }
      />

      <div className="grid gap-6 xl:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle className="text-sm">profile</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex flex-wrap gap-1">
              {TABS.map((name) => (
                <Button
                  key={name}
                  type="button"
                  size="xs"
                  variant={tab === name ? 'secondary' : 'ghost'}
                  onClick={() => setTab(name)}
                >
                  <span data-ltr>{name}</span>
                </Button>
              ))}
            </div>

            {tab === 'roles' ? (
              <div className="space-y-4">
                {withoutNotes(roles).map((role) => (
                  <div key={role} className="space-y-2 rounded-lg border p-3">
                    <div className="flex items-center justify-between">
                      <span className="text-sm font-medium" data-ltr>
                        {role}
                      </span>
                      <Button
                        type="button"
                        size="xs"
                        variant="ghost"
                        onClick={() =>
                          setChain(role, [...chainOf(role), { adapter: '', model: '', family: '' }])
                        }
                      >
                        +
                      </Button>
                    </div>

                    {chainOf(role).map((hop, index) => (
                      <div key={index} className="grid gap-2 sm:grid-cols-2">
                        {['adapter', 'model', 'family', 'pool', 'tier'].map((field) => (
                          <input
                            key={field}
                            className={INPUT}
                            data-ltr
                            placeholder={field}
                            aria-label={field}
                            value={String(hop[field] ?? '')}
                            onChange={(event) => updateHop(role, index, field, event.target.value)}
                          />
                        ))}
                        <div className="flex gap-1 sm:col-span-2">
                          <Button
                            type="button"
                            size="xs"
                            variant="ghost"
                            onClick={() => moveHop(role, index, -1)}
                          >
                            ↑
                          </Button>
                          <Button
                            type="button"
                            size="xs"
                            variant="ghost"
                            onClick={() => moveHop(role, index, 1)}
                          >
                            ↓
                          </Button>
                          <Button
                            type="button"
                            size="xs"
                            variant="ghost"
                            onClick={() =>
                              setChain(
                                role,
                                chainOf(role).filter((_, position) => position !== index),
                              )
                            }
                          >
                            ×
                          </Button>
                        </div>
                      </div>
                    ))}
                  </div>
                ))}
              </div>
            ) : null}

            {tab === 'tiers' ? (
              <div className="space-y-4">
                {withoutNotes(tiers).map((tier) => {
                  const spec = (tiers[tier] ?? {}) as Record<string, unknown>;
                  const numeric = [
                    'maxConcurrent',
                    'minGapSec',
                    'rateLimitRetries',
                    'rateLimitBackoffSec',
                    'queueWaitSec',
                  ];

                  return (
                    <div key={tier} className="space-y-2 rounded-lg border p-3">
                      <span className="text-sm font-medium" data-ltr>
                        {tier}
                      </span>
                      <div className="grid gap-2 sm:grid-cols-2">
                        {numeric.map((field) => (
                          <label key={field} className="space-y-1">
                            <span className="text-xs text-muted-foreground" data-ltr>
                              {field}
                            </span>
                            <input
                              className={INPUT}
                              data-ltr
                              type="number"
                              value={String(spec[field] ?? '')}
                              onChange={(event) =>
                                setBlock('tiers', {
                                  ...tiers,
                                  [tier]: {
                                    ...spec,
                                    [field]:
                                      event.target.value === '' ? undefined : Number(event.target.value),
                                  },
                                })
                              }
                            />
                          </label>
                        ))}
                        <label className="space-y-1">
                          <span className="text-xs text-muted-foreground" data-ltr>
                            whenBusy
                          </span>
                          <input
                            className={INPUT}
                            data-ltr
                            value={String(spec.whenBusy ?? '')}
                            onChange={(event) =>
                              setBlock('tiers', {
                                ...tiers,
                                [tier]: { ...spec, whenBusy: event.target.value },
                              })
                            }
                          />
                        </label>
                      </div>
                    </div>
                  );
                })}
              </div>
            ) : null}

            {tab === 'adapters' ? (
              <div className="space-y-4">
                {withoutNotes(adapters).map((adapter) => {
                  const spec = (adapters[adapter] ?? {}) as Record<string, unknown>;

                  return (
                    <div key={adapter} className="space-y-2 rounded-lg border p-3">
                      <div className="flex items-center justify-between">
                        <span className="text-sm font-medium" data-ltr>
                          {adapter}
                        </span>
                        <label className="flex items-center gap-2 text-xs">
                          <input
                            type="checkbox"
                            className="size-4 accent-current"
                            checked={spec.enabled !== false}
                            onChange={(event) =>
                              setBlock('adapters', {
                                ...adapters,
                                [adapter]: { ...spec, enabled: event.target.checked },
                              })
                            }
                          />
                          <span data-ltr>enabled</span>
                        </label>
                      </div>
                      <div className="grid gap-2 sm:grid-cols-2">
                        {['transport', 'command', 'tier', 'maxConcurrent', 'idleTimeoutSec', 'pool'].map(
                          (field) => (
                            <input
                              key={field}
                              className={INPUT}
                              data-ltr
                              placeholder={field}
                              aria-label={field}
                              value={String(spec[field] ?? '')}
                              onChange={(event) =>
                                setBlock('adapters', {
                                  ...adapters,
                                  [adapter]: { ...spec, [field]: event.target.value },
                                })
                              }
                            />
                          ),
                        )}
                      </div>
                    </div>
                  );
                })}
              </div>
            ) : null}

            {tab === 'workflow' ? <WorkflowSteps config={draft} onChange={setDraft} /> : null}

            {tab === 'json' ? (
              <div className="space-y-2">
                <textarea
                  className="h-96 w-full rounded-md border bg-background p-2 font-mono text-xs"
                  data-ltr
                  value={raw}
                  onChange={(event) => {
                    setRaw(event.target.value);
                    const next = parseProfile(event.target.value);

                    setRawError(next.error);

                    if (next.value) {
                      setDraft(next.value);
                    }
                  }}
                />
                {rawError ? <p className="text-xs text-destructive">{rawError}</p> : null}
              </div>
            ) : null}
          </CardContent>
        </Card>

        <ProfileHistory
          id={id}
          currentVersion={loaded.version}
          busy={revert.isPending}
          onRevert={revertTo}
        />
      </div>

      <ReasonDialog
        open={asking}
        busy={save.isPending}
        onOpenChange={setAsking}
        onConfirm={(reason) => void saveNow(reason)}
      />
    </div>
  );
}
