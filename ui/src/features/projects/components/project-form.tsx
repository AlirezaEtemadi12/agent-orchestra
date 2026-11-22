'use client';

import { zodResolver } from '@hookform/resolvers/zod';
import { useTranslations } from 'next-intl';
import { useSearchParams } from 'next/navigation';
import { useEffect, useRef, useState, type ReactNode } from 'react';
import { useForm, useWatch, type Resolver } from 'react-hook-form';
import { toast } from 'sonner';
import { EmptyState } from '@/components/app/empty-state';
import { PageHeader } from '@/components/app/page-header';
import { TableSkeleton } from '@/components/app/table-skeleton';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useRouter } from '@/i18n/navigation';
import { useProjects, type ProjectDto } from '@/lib/projects/queries';
import { CheckResult } from './check-result';
import { copyPrompt, selectText } from '../clipboard';
import { parseRepos, projectSchema, runsPathAfterCheck, slugOf, type ProjectFormValues } from '../project-schema';
import { errorText, isIdTaken } from '../projects-errors';
import { useCheckFolder, useCreateProject, useScaffoldPrompt, useUpdateProject } from '../queries';
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
  const create = useCreateProject();
  const update = useUpdateProject();
  const check = useCheckFolder();
  const prompt = useScaffoldPrompt();

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
      repos: (project?.repos ?? []).join(', '),
      profile: 'default',
    },
  });

  const [checkResult, setCheckResult] = useState<FolderCheck | null>(null);
  const [promptText, setPromptText] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [idTouched, setIdTouched] = useState(false);
  const promptRef = useRef<HTMLPreElement | null>(null);

  const name = useWatch({ control: form.control, name: 'name' });

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

  const runCheck = async (): Promise<void> => {
    setMessage(null);

    try {
      const result = await check.mutateAsync(form.getValues('rootPath'));
      const runsPath = runsPathAfterCheck(form.getValues('runsPath'), result.suggestedRunsPath);

      if (runsPath !== form.getValues('runsPath')) {
        form.setValue('runsPath', runsPath);
      }

      setCheckResult(result);

      const generated = await prompt.mutateAsync({
        name: form.getValues('name'),
        rootPath: form.getValues('rootPath'),
        repos: parseRepos(form.getValues('repos')),
        check: result,
      });

      setPromptText(generated.text);
      setCopied(false);
    } catch (error) {
      setMessage(errorText(error, common('errors.unexpected')));
    }
  };

  const copy = async (): Promise<void> => {
    if (!promptText) {
      return;
    }

    const done = await copyPrompt(promptText);

    setCopied(done);

    if (!done) {
      selectText(promptRef.current);
    }
  };

  const submit = form.handleSubmit(async (values) => {
    const body = {
      id: values.id,
      name: values.name,
      rootPath: values.rootPath,
      runsPath: values.runsPath,
      repos: parseRepos(values.repos),
      profile: 'default',
    };

    setMessage(null);

    try {
      if (project) {
        await update.mutateAsync({ id: project.id, body });
      } else {
        await create.mutateAsync(body);
      }

      toast.success(t('saved'));
      back();
    } catch (error) {
      if (!project && isIdTaken(error)) {
        form.setError('id', { message: t('errors.IdTaken') });
        return;
      }

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

      <div className="grid gap-4 sm:grid-cols-2">
        <Field label={t('fields.name')} error={errors.name?.message}>
          <input {...form.register('name')} className={INPUT} disabled={readOnly} />
        </Field>

        <Field label={t('fields.id')} error={errors.id?.message}>
          <input
            {...form.register('id', { onChange: () => setIdTouched(true) })}
            className={INPUT}
            data-ltr
            disabled={readOnly || project !== null}
          />
        </Field>

        <Field label={t('fields.rootPath')} error={errors.rootPath?.message}>
          <input {...form.register('rootPath')} className={INPUT} data-ltr disabled={readOnly} />
        </Field>

        <Field label={t('fields.runsPath')} error={errors.runsPath?.message}>
          <input {...form.register('runsPath')} className={INPUT} data-ltr disabled={readOnly} />
        </Field>

        <Field label={t('fields.repos')} error={errors.repos?.message}>
          <input {...form.register('repos')} className={INPUT} data-ltr disabled={readOnly} />
        </Field>

        <Field label={t('fields.profile')} hint={t('fields.profileLater')}>
          <input value="default" className={INPUT} readOnly disabled />
        </Field>
      </div>

      <p className="text-xs text-muted-foreground">{t('dockerHint')}</p>

      {!readOnly ? (
        <div className="flex flex-wrap items-center gap-2">
          <Button
            type="button"
            size="sm"
            variant="outline"
            disabled={check.isPending}
            onClick={() => void runCheck()}
          >
            {checkResult ? t('checkAgain') : t('check')}
          </Button>
        </div>
      ) : null}

      {checkResult ? <CheckResult check={checkResult} /> : null}

      {checkResult && !checkResult.isAbsolute ? (
        <p className="text-sm text-destructive">{t('notAbsolute')}</p>
      ) : null}

      {checkResult && checkResult.isAbsolute && !checkResult.exists ? (
        <p className="text-sm text-destructive">{t('folderMissing')}</p>
      ) : null}

      {promptText ? (
        <section className="space-y-2">
          <h2 className="text-sm font-medium">{t('promptTitle')}</h2>
          <p className="text-xs text-muted-foreground">{t('promptHint')}</p>
          <pre
            ref={promptRef}
            data-ltr
            className="max-h-80 overflow-auto rounded-lg border bg-muted/40 p-3 text-xs whitespace-pre-wrap"
          >
            {promptText}
          </pre>
          <div className="flex flex-wrap gap-2">
            <Button type="button" size="sm" variant="outline" onClick={() => void copy()}>
              {copied ? t('copied') : t('copy')}
            </Button>
            {!readOnly ? (
              <Button type="button" size="sm" variant="ghost" onClick={() => void runCheck()}>
                {t('checkAgain')}
              </Button>
            ) : null}
          </div>
        </section>
      ) : null}

      <div className="flex items-center gap-2">
        {!readOnly ? (
          <Button type="submit" disabled={create.isPending || update.isPending}>
            {t('save')}
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
