'use client';

import { useState } from 'react';
import { useTranslations } from 'next-intl';
import { TableSkeleton } from '@/components/app/table-skeleton';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { useVersions } from '../queries';

type ProfileHistoryProps = {
  id: string;
  currentVersion: number;
  busy: boolean;
  onRevert: (version: number) => void;
};

export function ProfileHistory({ id, currentVersion, busy, onRevert }: ProfileHistoryProps) {
  const t = useTranslations('profiles');
  const [left, setLeft] = useState<number | null>(null);
  const [right, setRight] = useState<number | null>(null);
  const diff = left !== null && right !== null && left !== right ? `${left},${right}` : undefined;
  const versions = useVersions(id, diff);

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t('history')}</CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        {versions.isPending ? <TableSkeleton rows={3} /> : null}

        {versions.data ? (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>n</TableHead>
                <TableHead>{t('reason')}</TableHead>
                <TableHead />
                <TableHead />
              </TableRow>
            </TableHeader>
            <TableBody>
              {versions.data.versions.map((version) => (
                <TableRow key={version.version}>
                  <TableCell data-ltr>{t('version', { n: version.version })}</TableCell>
                  <TableCell className="text-sm">{version.reason}</TableCell>
                  <TableCell>
                    <div className="flex gap-1">
                      <Button
                        type="button"
                        size="xs"
                        variant={left === version.version ? 'secondary' : 'ghost'}
                        onClick={() => setLeft(version.version)}
                      >
                        a
                      </Button>
                      <Button
                        type="button"
                        size="xs"
                        variant={right === version.version ? 'secondary' : 'ghost'}
                        onClick={() => setRight(version.version)}
                      >
                        b
                      </Button>
                    </div>
                  </TableCell>
                  <TableCell>
                    {version.version === currentVersion ? null : (
                      <Button
                        type="button"
                        size="xs"
                        variant="ghost"
                        disabled={busy}
                        onClick={() => onRevert(version.version)}
                      >
                        {t('revert')}
                      </Button>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        ) : null}

        {versions.data?.diff ? (
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>path</TableHead>
                <TableHead>a</TableHead>
                <TableHead>b</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {versions.data.diff.map((change) => (
                <TableRow key={change.path}>
                  <TableCell data-ltr className="text-xs">
                    {change.path}
                  </TableCell>
                  <TableCell data-ltr className="text-xs text-muted-foreground">
                    {change.from ?? '—'}
                  </TableCell>
                  <TableCell data-ltr className="text-xs">
                    {change.to ?? '—'}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        ) : null}
      </CardContent>
    </Card>
  );
}
