import { NextIntlClientProvider } from 'next-intl';
import { fireEvent, render, screen } from '@testing-library/react';
import type { ReactElement } from 'react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { ApiError } from '@/lib/api/api-error';
import type { ProjectDto } from '@/lib/projects/queries';
import { copyPrompt } from './clipboard';
import { CheckResult } from './components/check-result';
import { ConfirmDialog } from './components/confirm-dialog';
import { ProjectsTable } from './components/projects-table';
import { fillFromCheck } from './form-autofill';
import { projectsEn } from './i18n/en';
import { isIdTaken } from './projects-errors';
import { parseRepos, projectSchema, runsPathAfterCheck, slugOf } from './project-schema';
import { RepoPicker } from './components/repo-picker';
import type { FolderCheck } from './types';

const messages = { projects: projectsEn, common: { actions: { retry: 'Try again', back: 'Back' } } };

function withProviders(ui: ReactElement) {
  return render(
    <NextIntlClientProvider locale="en" messages={messages}>
      {ui}
    </NextIntlClientProvider>,
  );
}

function project(overrides: Partial<ProjectDto>): ProjectDto {
  return {
    id: 'shop',
    name: 'MyShop',
    runCount: 0,
    openRunCount: 0,
    sessions: 0,
    calls: 0,
    requests: 0,
    costUsd: null,
    lastImport: null,
    origin: 'dashboard',
    rootPath: '/work/MyShop',
    runsPath: '/work/MyShop/.orchestra/runs',
    repos: [],
    ...overrides,
  };
}

describe('the projects table', () => {
  it('offers remove only for a dashboard project', () => {
    withProviders(
      <ProjectsTable
        rows={[project({ id: 'from-config', origin: 'config' }), project({})]}
        onRemove={() => undefined}
      />,
    );

    expect(screen.getAllByRole('button', { name: 'Remove' })).toHaveLength(1);
    expect(screen.getByText('Added here')).toBeInTheDocument();
    expect(screen.getByText('From the configuration file')).toBeInTheDocument();
  });

  it('shows a dash for a project without a folder', () => {
    const { container } = withProviders(
      <ProjectsTable rows={[project({ rootPath: null, runsPath: null })]} onRemove={() => undefined} />,
    );

    expect(container.textContent).toContain('—');
  });
});

describe('the folder check result', () => {
  it('marks each item found or missing', () => {
    const check: FolderCheck = {
      exists: true,
      isAbsolute: true,
      items: [
        { name: 'CLAUDE.md', kind: 'file', found: true },
        { name: '.ai/', kind: 'folder', found: false },
      ],
      repos: ['api'],
      suggestedRunsPath: '/shop/.orchestra/runs',
      repoKnowledge: [],
      hostPath: '/shop',
      suggestedName: 'shop',
      isReady: false,
      visible: true,
    };

    withProviders(<CheckResult check={check} />);

    expect(screen.getByText('CLAUDE.md')).toBeInTheDocument();
    expect(screen.getByText('found')).toBeInTheDocument();
    expect(screen.getByText('missing')).toBeInTheDocument();
    expect(screen.getByText('api')).toBeInTheDocument();
  });
});

describe('the remove dialog', () => {
  it('names the project', () => {
    withProviders(
      <ConfirmDialog
        open
        title="Remove MyShop?"
        body="It leaves the dashboard with its runs and library. Its folder is not touched."
        confirmLabel="Remove"
        cancelLabel="Back"
        onOpenChange={() => undefined}
        onConfirm={() => undefined}
      />,
    );

    expect(screen.getByRole('dialog')).toBeInTheDocument();
    expect(screen.getByText('Remove MyShop?')).toBeInTheDocument();
    expect(screen.getByText(/folder is not touched/i)).toBeInTheDocument();
  });
});

describe('the form fields', () => {
  it('has no profile field any more (plan 0009 D12)', () => {
    expect(projectsEn.fields).not.toHaveProperty('profile');
  });
});

describe('the form rules', () => {
  const schema = projectSchema({ required: 'Required.', idFormat: 'Lowercase letters, digits and dashes, up to 40.' });

  it('asks for the required fields', () => {
    const result = schema.safeParse({ name: '', id: '', rootPath: '', runsPath: '', repos: [] });

    expect(result.success).toBe(false);

    const messages = result.error!.issues.map((issue) => issue.message);

    expect(messages).toContain('Required.');
    expect(messages).toContain('Lowercase letters, digits and dashes, up to 40.');
  });

  it('accepts a valid project', () => {
    const result = schema.safeParse({
      name: 'MyShop',
      id: 'myshop',
      rootPath: '/work/MyShop',
      runsPath: '/work/MyShop/.orchestra/runs',
      repos: ['api', 'web'],
    });

    expect(result.success).toBe(true);
  });

  it('builds an id from the name until the owner types one', () => {
    expect(slugOf('My Shop 24')).toBe('my-shop-24');
    expect(slugOf('  --x-- ')).toBe('x');
  });

  it('splits repos on commas and newlines', () => {
    expect(parseRepos('api, web\ncli')).toEqual(['api', 'web', 'cli']);
  });

  it('fills the runs field only when it is empty', () => {
    expect(runsPathAfterCheck('', '/shop/.orchestra/runs')).toBe('/shop/.orchestra/runs');
    expect(runsPathAfterCheck('  ', '/shop/.orchestra/runs')).toBe('/shop/.orchestra/runs');
    expect(runsPathAfterCheck('/typed/runs', '/shop/.orchestra/runs')).toBe('/typed/runs');
  });

  it('puts a taken id on the id field', () => {
    expect(isIdTaken(new ApiError(409, 'Projects.Project.IdTaken', 'taken'))).toBe(true);
    expect(isIdTaken(new ApiError(400, 'Projects.Project.Invalid', 'bad'))).toBe(false);
    expect(isIdTaken(new Error('network'))).toBe(false);
  });
});

describe('the auto-fill rule', () => {
  const check: FolderCheck = {
    exists: true,
    isAbsolute: true,
    items: [],
    repos: ['api', 'web'],
    suggestedRunsPath: 'F:\\Projects\\MyShop\\.orchestra\\runs',
    repoKnowledge: [],
    hostPath: 'F:\\Projects\\MyShop',
    suggestedName: 'MyShop',
    isReady: true,
    visible: true,
  };

  const empty = { name: '  ', id: '', runsPath: '', repos: [], idTouched: false, reposTouched: false };

  it('fills name, runs and repos when they are empty', () => {
    expect(fillFromCheck(empty, check)).toEqual({
      name: 'MyShop',
      runsPath: 'F:\\Projects\\MyShop\\.orchestra\\runs',
      repos: ['api', 'web'],
    });
  });

  it('never overwrites a typed name or runs folder', () => {
    const patch = fillFromCheck({ ...empty, name: 'Typed', runsPath: '/typed/runs' }, check);

    expect(patch.name).toBeUndefined();
    expect(patch.runsPath).toBeUndefined();
  });

  it('keeps a repo list the owner changed', () => {
    expect(fillFromCheck({ ...empty, repos: ['api'], reposTouched: true }, check).repos).toBeUndefined();
  });
});

describe('the repo picker', () => {
  it('ticks the detected repos and untickes one', () => {
    const onChange = vi.fn();

    withProviders(<RepoPicker detected={['api']} value={['api', 'web']} disabled={false} onChange={onChange} />);

    expect(screen.getByRole('checkbox', { name: 'api' })).toBeChecked();
    expect(screen.getByRole('checkbox', { name: 'web' })).toBeChecked();

    fireEvent.click(screen.getByRole('checkbox', { name: 'api' }));

    expect(onChange).toHaveBeenCalledWith(['web']);
  });

  it('adds a repo by its folder name', () => {
    const onChange = vi.fn();

    withProviders(<RepoPicker detected={[]} value={[]} disabled={false} onChange={onChange} />);

    fireEvent.change(screen.getByPlaceholderText('Add a repo folder name'), { target: { value: 'cli' } });
    fireEvent.click(screen.getByRole('button', { name: 'Add' }));

    expect(onChange).toHaveBeenCalledWith(['cli']);
  });
});

describe('copying the prompt', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('reports success when the clipboard accepts the text', async () => {
    const writeText = vi.fn().mockResolvedValue(undefined);
    vi.stubGlobal('navigator', { clipboard: { writeText } });

    await expect(copyPrompt('hello')).resolves.toBe(true);
    expect(writeText).toHaveBeenCalledWith('hello');
  });

  it('reports failure when the clipboard refuses', async () => {
    vi.stubGlobal('navigator', { clipboard: { writeText: vi.fn().mockRejectedValue(new Error('denied')) } });

    await expect(copyPrompt('hello')).resolves.toBe(false);
  });
});
