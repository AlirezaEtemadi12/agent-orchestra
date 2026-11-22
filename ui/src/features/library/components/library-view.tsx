'use client';

import { useTranslations } from 'next-intl';
import { useSearchParams } from 'next/navigation';
import { useRouter } from '@/i18n/navigation';
import { DateTimeText } from '@/components/app/data-text';
import { EmptyState } from '@/components/app/empty-state';
import { ErrorState } from '@/components/app/error-state';
import { Markdown } from '@/components/app/markdown';
import { PageHeader } from '@/components/app/page-header';
import { TableSkeleton } from '@/components/app/table-skeleton';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Link } from '@/i18n/navigation';
import { absoluteApiUrl } from '../api';
import { libraryHref, tabFor } from '../library-params';
import { useLibraryItem, useLibraryText, usePlanPrompt } from '../queries';
import { CopyPrompt } from './copy-prompt';
import { EvidenceGallery } from './evidence-gallery';
import { PreviewFrame } from './preview-frame';
import { LibraryStatusBadge } from './status';

export function LibraryView() {
  const t = useTranslations('library');
  const common = useTranslations('common');
  const params = useSearchParams();
  const router = useRouter();

  const project = params.get('project');
  const category = params.get('category');
  const relPath = params.get('path');

  const detail = useLibraryItem(project, category, relPath);
  const isPlan = detail.data?.item.category === 'Plan';

  const prompt = usePlanPrompt(project, relPath, isPlan);
  const text = useLibraryText(project, category, relPath);

  const back = (): void => router.push(libraryHref({ tab: tabFor(category ?? 'Plan').tab }) as never);

  if (!project || !category || !relPath) {
    return (
      <div className="space-y-6">
        <Button type="button" variant="outline" size="sm" onClick={back}>
          {common('actions.back')}
        </Button>
        <EmptyState title={t('notFound')} />
      </div>
    );
  }

  if (detail.isPending) {
    return <TableSkeleton rows={8} />;
  }

  if (detail.isError || !detail.data) {
    return (
      <div className="space-y-4">
        <ErrorState
          title={t('notFound')}
          retryLabel={common('actions.retry')}
          onRetry={() => void detail.refetch()}
        />
        <Button type="button" variant="outline" size="sm" onClick={back}>
          {common('actions.back')}
        </Button>
      </div>
    );
  }

  const item = detail.data.item;
  const previewSrc = detail.data.previewUrl ? absoluteApiUrl(detail.data.previewUrl) : null;

  return (
    <div className="space-y-6">
      <div className="space-y-2">
        <Button type="button" variant="outline" size="sm" onClick={back}>
          {common('actions.back')}
        </Button>

        <PageHeader
          title={item.title}
          description={[item.projectName, item.number ? `#${item.number}` : null, item.plannedBy]
            .filter(Boolean)
            .join(' · ')}
          actions={item.status ? <LibraryStatusBadge status={item.status} kind={item.category === 'Idea' ? 'idea' : 'plan'} /> : null}
        />

        <p className="text-xs text-muted-foreground" data-ltr>
          {item.relPath}
        </p>

        <div className="flex flex-wrap gap-4 text-xs text-muted-foreground">
          {item.repo ? (
            <span>
              {t('columns.project')}: <span data-ltr>{item.repo}</span>
            </span>
          ) : null}
          <span>
            {t('columns.created')}: <DateTimeText value={item.createdOn ?? item.modifiedAt} />
          </span>
          <span>
            {t('columns.updated')}: <DateTimeText value={item.updatedOn} />
          </span>
          {item.doneOn ? (
            <span>
              {t('columns.done')}: <DateTimeText value={item.doneOn} />
            </span>
          ) : null}
        </div>
      </div>

      {isPlan && prompt.data ? (
        <Card>
          <CardContent className="pt-6">
            <CopyPrompt text={prompt.data.text} />
          </CardContent>
        </Card>
      ) : null}

      {isPlan && detail.data.runs.length > 0 ? (
        <Card>
          <CardHeader>
            <CardTitle>{t('runs')}</CardTitle>
          </CardHeader>
          <CardContent>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('columns.run')}</TableHead>
                  <TableHead>{t('columns.status')}</TableHead>
                  <TableHead>{t('columns.updated')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {detail.data.runs.map((run) => (
                  <TableRow key={run.runKey}>
                    <TableCell>
                      <Link
                        href={{ pathname: '/runs/view/', query: { project: item.projectId, run: run.runKey } }}
                        className="underline-offset-4 hover:underline"
                      >
                        <span data-ltr>{run.runKey}</span>
                      </Link>
                    </TableCell>
                    <TableCell data-ltr>{run.status}</TableCell>
                    <TableCell>
                      <DateTimeText value={run.updatedAt} />
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      ) : null}

      {previewSrc ? (
        <Card>
          <CardHeader>
            <CardTitle>{t('tabs.previews')}</CardTitle>
          </CardHeader>
          <CardContent>
            <PreviewFrame
              src={previewSrc}
              note={t('previewSandboxed')}
              openLabel={t('openInTab')}
            />
          </CardContent>
        </Card>
      ) : null}

      {item.category === 'Idea' && item.linkedPlanNumber ? (
        <Card>
          <CardHeader>
            <CardTitle>{t('columns.linkedPlan')}</CardTitle>
          </CardHeader>
          <CardContent className="text-sm">
            <span data-ltr>{item.linkedPlanNumber}</span>
            {item.linkedPlanTitle ? ` · ${item.linkedPlanTitle}` : null}
          </CardContent>
        </Card>
      ) : null}

      {text.data ? (
        <Card>
          <CardContent className="pt-6">
            <Markdown source={text.data} />
          </CardContent>
        </Card>
      ) : null}

      {isPlan && detail.data.evidence.length > 0 ? (
        <Card>
          <CardHeader>
            <CardTitle>{t('evidenceFor')}</CardTitle>
          </CardHeader>
          <CardContent>
            <EvidenceGallery items={detail.data.evidence} />
          </CardContent>
        </Card>
      ) : null}
    </div>
  );
}
