'use client';

import { useTranslations } from 'next-intl';
import { useState } from 'react';
import { DurationText, PercentText, TokensText } from '@/components/app/data-text';
import { EmptyState } from '@/components/app/empty-state';
import { ErrorState } from '@/components/app/error-state';
import { PageHeader } from '@/components/app/page-header';
import { ProjectChips } from '@/components/app/project-chips';
import { TableSkeleton } from '@/components/app/table-skeleton';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { useProjects } from '@/lib/projects/queries';
import { useQuality } from '../queries';

const COUNT = (value: number): string => String(value);

export function QualityPage() {
  const t = useTranslations('quality');
  const common = useTranslations('common');
  const projects = useProjects();
  const [project, setProject] = useState<string | null>(null);
  const quality = useQuality(project);
  const data = quality.data;

  const empty =
    !quality.isPending &&
    !quality.isError &&
    !data?.reviewers.length &&
    !data?.readBacks.length &&
    !data?.implementers.length;

  return (
    <div className="space-y-6">
      <PageHeader title={t('title')} description={t('note')} />

      <ProjectChips
        projects={projects.data ?? []}
        selected={project}
        allLabel={common('filters.allProjects')}
        onSelect={setProject}
      />

      {quality.isPending ? <TableSkeleton rows={8} /> : null}

      {quality.isError ? (
        <ErrorState
          title={common('errors.network')}
          retryLabel={common('actions.retry')}
          onRetry={() => void quality.refetch()}
        />
      ) : null}

      {empty ? <EmptyState title={common('states.empty')} /> : null}

      {data && data.reviewers.length > 0 ? (
        <Card>
          <CardHeader>
            <CardTitle>{t('reviewers')}</CardTitle>
          </CardHeader>
          <CardContent>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('title')}</TableHead>
                  <TableHead>{t('columns.runs')}</TableHead>
                  <TableHead>{t('columns.avgRounds')}</TableHead>
                  <TableHead>{t('columns.passFirstRound')}</TableHead>
                  <TableHead>{t('columns.findingsShare')}</TableHead>
                  <TableHead>{t('columns.finalCounts')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.reviewers.map((row) => (
                  <TableRow key={row.model}>
                    <TableCell data-ltr>{row.model}</TableCell>
                    <TableCell data-ltr>{row.runs}</TableCell>
                    <TableCell data-ltr>{row.avgRounds?.toFixed(1) ?? '—'}</TableCell>
                    <TableCell>
                      <PercentText value={row.passFirstRound} />
                    </TableCell>
                    <TableCell>
                      <PercentText value={row.findingsShare} />
                    </TableCell>
                    <TableCell className="tabular-nums" data-ltr>
                      {`${row.finalBlockers}/${row.finalShould}/${row.finalNits}`}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      ) : null}

      {data && data.readBacks.length > 0 ? (
        <Card>
          <CardHeader>
            <CardTitle>{t('readBacks')}</CardTitle>
          </CardHeader>
          <CardContent>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('title')}</TableHead>
                  <TableHead>{t('columns.runs')}</TableHead>
                  <TableHead>{t('columns.findingsShare')}</TableHead>
                  <TableHead>{t('columns.finalCounts')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.readBacks.map((row) => (
                  <TableRow key={row.model}>
                    <TableCell data-ltr>{row.model}</TableCell>
                    <TableCell data-ltr>{row.runs}</TableCell>
                    <TableCell>
                      <PercentText value={row.finalFindingsShare} />
                    </TableCell>
                    <TableCell className="tabular-nums" data-ltr>
                      {`${row.finalBlockers}/${row.finalShould}/${row.finalNits}`}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      ) : null}

      {data && data.implementers.length > 0 ? (
        <Card>
          <CardHeader>
            <CardTitle>{t('implementers')}</CardTitle>
          </CardHeader>
          <CardContent>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('title')}</TableHead>
                  <TableHead>{t('columns.phases')}</TableHead>
                  <TableHead>{t('columns.needInput')}</TableHead>
                  <TableHead>{t('columns.findingsShare')}</TableHead>
                  <TableHead>{t('columns.outsidePlan')}</TableHead>
                  <TableHead>{t('columns.violations')}</TableHead>
                  <TableHead>{t('columns.verifyFailRate')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.implementers.map((row) => (
                  <TableRow key={row.model}>
                    <TableCell data-ltr>{row.model}</TableCell>
                    <TableCell data-ltr>{row.phases}</TableCell>
                    <TableCell data-ltr>{row.needInput}</TableCell>
                    <TableCell data-ltr>{row.changedAvg?.toFixed(1) ?? '—'}</TableCell>
                    <TableCell data-ltr>{COUNT(row.outsidePlan)}</TableCell>
                    <TableCell data-ltr>{COUNT(row.violations)}</TableCell>
                    <TableCell>
                      <PercentText value={row.verifyFailRate} />
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      ) : null}

      {data && data.stageTimes.length > 0 ? (
        <Card>
          <CardHeader>
            <CardTitle>{t('stageTimes')}</CardTitle>
          </CardHeader>
          <CardContent>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('title')}</TableHead>
                  <TableHead>{t('columns.runs')}</TableHead>
                  <TableHead>{t('columns.avgTime')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.stageTimes.map((row) => (
                  <TableRow key={`${row.stage}.${row.model}`}>
                    <TableCell data-ltr>{`${row.stage} · ${row.model}`}</TableCell>
                    <TableCell data-ltr>{row.runs}</TableCell>
                    <TableCell>
                      <DurationText seconds={row.avgSeconds} />
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      ) : null}

      {data && data.cache.length > 0 ? (
        <Card>
          <CardHeader>
            <CardTitle>{t('cache')}</CardTitle>
          </CardHeader>
          <CardContent>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>{t('title')}</TableHead>
                  <TableHead>{t('columns.findingsShare')}</TableHead>
                  <TableHead>{t('columns.avgTime')}</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {data.cache.map((row) => (
                  <TableRow key={row.model}>
                    <TableCell data-ltr>{row.model}</TableCell>
                    <TableCell>
                      <TokensText value={row.cacheRead} />
                    </TableCell>
                    <TableCell>
                      <PercentText value={row.hitRate} />
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      ) : null}
    </div>
  );
}
