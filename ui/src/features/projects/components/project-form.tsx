'use client';

import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslations } from 'next-intl';
import { useSearchParams } from 'next/navigation';
import { useEffect, useState, type ReactNode } from 'react';
import { useForm, useWatch, type Resolver } from 'react-hook-form';
import { toast } from 'sonner';
import { EmptyState } from '@/components/app/empty-state';
import { PageHeader } from '@/components/app/page-header';
import { TableSkeleton } from '@/components/app/table-skeleton';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useRouter } from '@/i18n/navigation';
import { isDesktop, pickFolder } from '@/lib/desktop/bridge';
import { useProjects, type ProjectDto } from '@/lib/projects/queries';
import { CheckResult } from './check-result';
import { FolderBrowser } from './folder-browser';
import { RepoPicker } from './repo-picker';
import { fillFromCheck } from '../form-autofill';
import { projectSchema, slugOf, type ProjectFormValues } from '../project-schema';
import { errorText } from '../projects-errors';
import { useCheckFolder, useUpdateProject } from '../queries';
import type { FolderCheck } from '../types';

const INPUT = 'h-9 w-full rounded-md border bg-background px-2 text-sm disabled:opacity-60';

export function ProjectForm() {
  const t = useTranslations('projects');
  const common = useTranslations('common');
  const params = useSearchParams();
  const projects = useProjects();
  const id = params.get('id');
  const existing = id ? projects.data?.find((project) => project.id === id) : undefined;

  if (id && projects.isPending) {
    return <TableSkeleton rows={6} />;
  }

  if (id && !existing) {
    return <EmptyState title={common('errors.unexpected')} />;
  }

  return (
    <ProjectFormFields
      key={existing?.id ?? 'new'}
      project={existing ?? null}
      readOnly={existing?.origin === 'config'}
      title={existing ? t('edit') : t('new')}
    />
  );
}

type FieldsProps = {
  project: ProjectDto | null;
  readOnly: boolean;
  title: string;
};

function ProjectFormFields({ project, readOnly, title }: FieldsProps) {
  const t = useTranslations('projects');
  const common = useTranslations('common');
  const router = useRouter();
  const update = useUpdateProject();
  const check = useCheckFolder();

  const schema = projectSchema({
    required: t('errors.required'),
    idFormat: t('errors.idFormat'),
  });

  const form = useForm<ProjectFormValues>({
    resolver: zodResolver(schema) as Resolver<ProjectFormValues>,
    defaultValues: {
      name: project?.name ?? '',
      id: project?.id ?? '',
      rootPath: project?.rootPath ?? '',
      runsPath: project?.runsPath ?? '',
      repos: [...(project?.repos ?? [])],
    },
  });

  const [checkResult, setCheckResult] = useState<FolderCheck | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [idTouched, setIdTouched] = useState(false);
  const [browserOpen, setBrowserOpen] = useState(false);
  const [browsePath, setBrowsePath] = useState<string | null>(null);
  const [checkedPath, setCheckedPath] = useState(project?.rootPath ?? '');
  const [reposTouched, setReposTouched] = useState((project?.repos ?? []).length > 0);

  const name = useWatch({ control: form.control, name: 'name' });
  const rootPath = useWatch({ control: form.control, name: 'rootPath' });
  const repos = useWatch({ control: form.control, name: 'repos' });

  useEffect(() => {
    if (readOnly || idTouched) {
      return;
    }

    const next = slugOf(name);

    if (form.getValues('id') !== next) {
      form.setValue('id', next, { shouldValidate: false });
    }
  }, [name, form, readOnly, idTouched]);

  const back = (): void => {
    router.push('/settings/projects/' as never);
  };

  // Plan 0012 §6.5: the system dialog in the desktop window, the in-app browser in a plain browser.
  const browse = async (): Promise<void> => {
    const start = form.getValues('rootPath').trim();

    if (!isDesktop()) {
      setBrowsePath(start.length > 0 ? start : null);
      setBrowserOpen(true);
      return;
    }

    try {
      const path = await pickFolder(start);

      if (path) {
        form.setValue('rootPath', path, { shouldValidate: true });
        void runCheck(path);
      }
    } catch (error) {
      setMessage(errorText(error, common('errors.unexpected')));
    }
  };

  const runCheck = async (path: string): Promise<FolderCheck | null> => {
    if (path.trim().length === 0) {
      return null;
    }

    setMessage(null);

    try {
      const result = await check.mutateAsync(path);
      const values = form.getValues();

      const patch = fillFromCheck(
        {
          name: values.name,
          id: values.id,
          runsPath: values.runsPath,
          repos: values.repos,
          idTouched,
          reposTouched,
        },
        result,
      );

      if (patch.name) {
        form.setValue('name', patch.name);
      }

      if (patch.runsPath) {
        form.setValue('runsPath', patch.runsPath);
      }

      if (patch.repos) {
        form.setValue('repos', patch.repos);
      }

      setCheckResult(result);
      setCheckedPath(path);

      return result;
    } catch (error) {
      setMessage(errorText(error, common('errors.unexpected')));
      return null;
    }
  };

  // Plan 0012 D11: nothing is written here. A new project goes on to the setup step with these values;
  // only an existing card is saved, because its folder is already set up.
  const submit = form.handleSubmit(async (values) => {
    setMessage(null);

    if (!project) {
      router.push({
        pathname: '/settings/projects/setup/',
        query: {
          id: values.id,
          name: values.name,
          rootPath: values.rootPath,
          repos: values.repos.join(','),
        },
      } as never);

      return;
    }

    try {
      await update.mutateAsync({
        id: project.id,
        body: {
          id: values.id,
          name: values.name,
          rootPath: values.rootPath,
          runsPath: values.runsPath,
          repos: values.repos,
          profile: 'default',
        },
      });

      toast.success(t('saved'));
      back();
    } catch (error) {
      setMessage(errorText(error, common('errors.unexpected')));
    }
  });

  const errors = form.formState.errors;

  return (
    <form className="space-y-6" onSubmit={(event) => void submit(event)} noValidate>
      <PageHeader
        title={title}
        actions={readOnly ? <Badge variant="outline">{t('origin.config')}</Badge> : undefined}
      />

      <Field label={t('fields.rootPath')} hint={t('hints.rootPath')} error={errors.rootPath?.message}>
        <div className="flex items-center gap-2">
          <input
            {...form.register('rootPath', {
              onBlur: () => {
                if (!readOnly && form.getValues('rootPath') !== checkedPath) {
                  void runCheck(form.getValues('rootPath'));
                }
              },
            })}
            className={INPUT}
            data-ltr
            disabled={readOnly}
          />
          {!readOnly ? (
            <Button
              type="button"
              size="sm"
              variant="outline"
              onClick={() => void browse()}
            >
              {t('form.browse')}
            </Button>
          ) : null}
        </div>
      </Field>

      {checkResult ? <CheckResult check={checkResult} /> : null}

      {checkResult && !checkResult.visible ? (
        <p className="text-sm text-destructive">{t('form.notVisible')}</p>
      ) : null}

      {checkResult && checkResult.visible && !checkResult.exists ? (
        <p className="text-sm text-destructive">{t('folderMissing')}</p>
      ) : null}

      <div className="grid gap-4 sm:grid-cols-2">
        <Field label={t('fields.name')} hint={t('hints.name')} error={errors.name?.message}>
          <input {...form.register('name')} className={INPUT} disabled={readOnly} />
        </Field>

        <Field label={t('fields.id')} hint={t('hints.id')} error={errors.id?.message}>
          <input
            {...form.register('id', { onChange: () => setIdTouched(true) })}
            className={INPUT}
            data-ltr
            disabled={readOnly || project !== null}
          />
        </Field>
      </div>

      <Field label={t('fields.repos')} hint={t('hints.repos')}>
        <RepoPicker
          detected={checkResult?.repos ?? []}
          value={repos}
          disabled={readOnly}
          onChange={(next) => {
            setReposTouched(true);
            form.setValue('repos', next);
          }}
        />
      </Field>

      <details className="rounded-xl border bg-card/40 p-4">
        <summary className="cursor-pointer text-sm font-medium">{t('form.advanced')}</summary>
        <div className="space-y-3 pt-3">
          <Field label={t('fields.runsPath')} hint={t('hints.runsPath')} error={errors.runsPath?.message}>
            <input {...form.register('runsPath')} className={INPUT} data-ltr disabled={readOnly} />
          </Field>
        </div>
      </details>

      {!readOnly ? (
        <div className="flex flex-wrap items-center gap-2">
          <Button
            type="button"
            size="sm"
            variant="outline"
            disabled={check.isPending || rootPath.trim().length === 0}
            onClick={() => void runCheck(form.getValues('rootPath'))}
          >
            {checkResult ? t('checkAgain') : t('check')}
          </Button>
        </div>
      ) : null}

      <div className="flex items-center gap-2">
        {!readOnly ? (
          <Button type="submit" disabled={update.isPending}>
            {project ? t('save') : t('setup.continue')}
          </Button>
        ) : null}
        <Button type="button" variant="ghost" onClick={back}>
          {common('actions.back')}
        </Button>
      </div>

      {message ? (
        <p role="alert" className="text-sm text-destructive">
          {message}
        </p>
      ) : null}

      <FolderBrowser
        open={browserOpen}
        path={browsePath}
        onPath={setBrowsePath}
        onOpenChange={setBrowserOpen}
        onChoose={(path) => {
          form.setValue('rootPath', path, { shouldValidate: true });
          void runCheck(path);
        }}
      />
    </form>
  );
}

function Field({
  label,
  error,
  hint,
  children,
}: {
  label: string;
  error?: string;
  hint?: string;
  children: ReactNode;
}) {
  return (
    <label className="block space-y-1 text-sm">
      <span className="text-muted-foreground">{label}</span>
      {children}
      {hint ? <span className="block text-xs text-muted-foreground">{hint}</span> : null}
      {error ? <span className="block text-xs text-destructive">{error}</span> : null}
    </label>
  );
}
