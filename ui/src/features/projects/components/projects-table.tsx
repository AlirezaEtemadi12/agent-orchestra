'use client';

import { useTranslations } from 'next-intl';
import { DateTimeText } from '@/components/app/data-text';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table';
import { Link } from '@/i18n/navigation';
import type { ProjectDto } from '@/lib/projects/queries';

type ProjectsTableProps = {
  rows: ProjectDto[];
  onRemove: (project: ProjectDto) => void;
};

export function ProjectsTable({ rows, onRemove }: ProjectsTableProps) {
  const t = useTranslations('projects');

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>{t('fields.name')}</TableHead>
          <TableHead>{t('fields.id')}</TableHead>
          <TableHead>{t('fields.rootPath')}</TableHead>
          <TableHead>{t('fields.runsPath')}</TableHead>
          <TableHead />
          <TableHead />
          <TableHead />
          <TableHead />
        </TableRow>
      </TableHeader>
      <TableBody>
        {rows.map((project) => {
          const dashboard = project.origin === 'dashboard';

          return (
            <TableRow key={project.id}>
              <TableCell className="font-medium">{project.name}</TableCell>
              <TableCell data-ltr>{project.id}</TableCell>
              <TableCell data-ltr className="max-w-64 truncate">
                {project.rootPath ? project.rootPath : <span className="text-muted-foreground">—</span>}
              </TableCell>
              <TableCell data-ltr className="max-w-64 truncate">
                {project.runsPath ? project.runsPath : <span className="text-muted-foreground">—</span>}
              </TableCell>
              <TableCell>
                {project.origin === 'config' ? (
                  <Badge variant="outline">{t('origin.config')}</Badge>
                ) : project.origin === 'dashboard' ? (
                  <Badge variant="secondary">{t('origin.dashboard')}</Badge>
                ) : null}
              </TableCell>
              <TableCell>
                <DateTimeText value={project.lastImport?.finishedAt ?? null} />
              </TableCell>
              <TableCell>
                {dashboard ? (
                  <Button type="button" size="xs" variant="ghost" onClick={() => onRemove(project)}>
                    {t('remove')}
                  </Button>
                ) : null}
              </TableCell>
              <TableCell>
                <Link
                  className="text-xs underline underline-offset-4"
                  data-ltr
                  href={{ pathname: '/settings/projects/effective/', query: { id: project.id } } as never}
                >
                  effectiveConfig
                </Link>
              </TableCell>
            </TableRow>
          );
        })}
      </TableBody>
    </Table>
  );
}
