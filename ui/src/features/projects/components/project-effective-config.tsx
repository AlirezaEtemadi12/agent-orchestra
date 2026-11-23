'use client';

import { useTranslations } from 'next-intl';
import { useSearchParams } from 'next/navigation';
import { useState } from 'react';
import { toast } from 'sonner';
import { EmptyState } from '@/components/app/empty-state';
import { ErrorState } from '@/components/app/error-state';
import { PageHeader } from '@/components/app/page-header';
import { TableSkeleton } from '@/components/app/table-skeleton';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { useProjects } from '@/lib/projects/queries';
import { useEffectiveConfig } from '../queries';
import { copyPrompt } from '../clipboard';

function valueAt(config: unknown, path: string): string {
  let current: unknown = config;

  for (const part of path.split('.')) {
    if (current === null || typeof current !== 'object' || Array.isArray(current)) {
      return '—';
    }

    current = (current as Record<string, unknown>)[part];
  }

  if (current === null || current === undefined) {
    return '—';
  }

  return typeof current === 'object' ? JSON.stringify(current) : String(current);
}

export function ProjectEffectiveConfig() {
  const t = useTranslations('projects');
  const from = useTranslations('profiles');
  const common = useTranslations('common');
  const params = useSearchParams();
  const id = params.get('id');
  const projects = useProjects();
  const config = useEffectiveConfig(id);
  const [copied, setCopied] = useState(false);

  if (!id) {
    return <EmptyState title={common('errors.unexpected')} />;
  }

  if (projects.isPending || config.isPending) {
    return <TableSkeleton rows={8} />;
  }

  if (config.isError || !config.data) {
    return (
      <ErrorState
        title={common('errors.network')}
        retryLabel={common('actions.retry')}
        onRetry={() => void config.refetch()}
      />
    );
  }

  const project = projects.data?.find((item) => item.id === id);
  const name = project?.name ?? id;

  // Plan 0007 D4a: the API says which file it read, so the prompt follows the project's layout.
  const prompt = `Replace ${config.data.configPath} in ${name} with this; commit nothing.`;

  const copy = async (text: string): Promise<void> => {
    const ok = await copyPrompt(text);

    setCopied(ok);

    if (ok) {
      toast.success(t('copied'));
    }
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title={`${name} · effectiveConfig`}
        description={
          config.data.profile
            ? `${from('from.profile')}: ${config.data.profile} ${from('version', { n: config.data.profileVersion ?? 0 })}`
            : undefined
        }
      />

      <Card>
        <CardHeader>
          <CardTitle className="text-sm">path · value · from</CardTitle>
        </CardHeader>
        <CardContent>
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>path</TableHead>
                <TableHead>value</TableHead>
                <TableHead>from</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {config.data.sources.map((source) => (
                <TableRow key={source.path}>
                  <TableCell data-ltr className="text-xs">
                    {source.path}
                  </TableCell>
                  <TableCell data-ltr className="max-w-96 truncate text-xs">
                    {valueAt(config.data.effective, source.path)}
                  </TableCell>
                  <TableCell>
                    <Badge variant={source.from === 'project' ? 'secondary' : 'outline'}>
                      {source.from === 'project' ? from('from.project') : from('from.profile')}
                    </Badge>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-sm">orchestrator.json</CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          <pre
            data-ltr
            className="max-h-96 overflow-auto rounded-md border bg-muted/30 p-3 text-xs"
          >
            {config.data.suggestedOrchestratorJson}
          </pre>
          <div className="flex flex-wrap gap-2">
            <Button type="button" size="sm" onClick={() => void copy(config.data.suggestedOrchestratorJson)}>
              {copied ? t('copied') : t('copy')}
            </Button>
            <Button type="button" size="sm" variant="outline" onClick={() => void copy(prompt)}>
              {prompt}
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
