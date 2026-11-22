import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { NextIntlClientProvider } from 'next-intl';
import type { ReactElement } from 'react';
import { describe, expect, it, vi } from 'vitest';
import { copyText, selectText } from './clipboard';
import { EvidenceGallery } from './components/evidence-gallery';
import { PreviewFrame } from './components/preview-frame';
import { LibraryStatusBadge } from './components/status';
import { libraryEn } from './i18n/en';
import {
  LIBRARY_TABS,
  detailQuery,
  detailHref,
  dirFrom,
  sortFrom,
  statusColumnVisible,
  tabFor,
  tabFrom,
  tabQuery,
} from './library-params';
import type { LibraryItem } from './types';

const messages = { library: libraryEn, common: { actions: { retry: 'Try again' } } };

function withProviders(ui: ReactElement) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });

  return render(
    <NextIntlClientProvider locale="en" messages={messages}>
      <QueryClientProvider client={client}>{ui}</QueryClientProvider>
    </NextIntlClientProvider>,
  );
}

function item(overrides: Partial<LibraryItem>): LibraryItem {
  return {
    projectId: 'demo',
    projectName: 'Demo',
    category: 'Evidence',
    relPath: '.orchestra/evidence/0001-demo/phase-A/shot.png',
    title: 'shot.png',
    number: null,
    slug: 'shot',
    repo: null,
    status: null,
    createdOn: null,
    updatedOn: null,
    doneOn: null,
    plannedBy: null,
    previewRelPath: null,
    linkedPlanNumber: null,
    runKey: null,
    group: '0001-demo/phase-A',
    modifiedAt: '2026-11-10T00:00:00Z',
    runStatus: null,
    linkedPlanTitle: null,
    ...overrides,
  };
}

describe('library url state', () => {
  it('switches tabs from ?tab=, falling back to plans', () => {
    expect(tabFrom('evidence').category).toBe('Evidence');
    expect(tabFrom(null).tab).toBe('plans');
    expect(tabFrom('nope').tab).toBe('plans');
    expect(tabFor('Research').tab).toBe('research');
  });

  it('keeps the project, sort and direction in a tab link', () => {
    expect(tabQuery(LIBRARY_TABS[3], 'demo', 'title', 'asc')).toEqual({
      tab: 'research',
      project: 'demo',
      sort: 'title',
      dir: 'asc',
    });
  });

  it('falls back on unknown sort keys and directions', () => {
    expect(sortFrom('nope')).toBe('updated');
    expect(sortFrom('done')).toBe('done');
    expect(dirFrom('asc')).toBe('asc');
    expect(dirFrom(null)).toBe('desc');
  });

  it('hides the status filter where status has no meaning', () => {
    expect(statusColumnVisible('Plan')).toBe(true);
    expect(statusColumnVisible('Idea')).toBe(true);
    expect(statusColumnVisible('Research')).toBe(false);
    expect(statusColumnVisible('Preview')).toBe(false);
    expect(statusColumnVisible('Evidence')).toBe(false);
  });

  it('carries the category in the detail URL', () => {
    expect(detailQuery('Idea', 'demo', '.orchestra/ideas/0001-first-idea.md')).toEqual({
      project: 'demo',
      category: 'Idea',
      path: '.orchestra/ideas/0001-first-idea.md',
    });
    expect(detailHref('Plan', 'demo', '.orchestra/plans/0001-alpha.md').pathname).toBe('/library/view/');
  });
});

describe('the copy prompt', () => {
  it('reports success when the clipboard accepts the text', async () => {
    const writeText = vi.fn().mockResolvedValue(undefined);
    vi.stubGlobal('navigator', { clipboard: { writeText } });

    await expect(copyText('hello')).resolves.toBe(true);
    expect(writeText).toHaveBeenCalledWith('hello');

    vi.unstubAllGlobals();
  });

  it('reports failure and selects the text when the clipboard refuses', async () => {
    vi.stubGlobal('navigator', {
      clipboard: { writeText: vi.fn().mockRejectedValue(new Error('denied')) },
    });

    await expect(copyText('hello')).resolves.toBe(false);

    const pre = document.createElement('pre');
    pre.textContent = 'hello';
    document.body.append(pre);

    selectText(pre);

    expect(window.getSelection()?.toString()).toBe('hello');

    pre.remove();
    vi.unstubAllGlobals();
  });
});

describe('the preview frame', () => {
  it('is sandboxed with scripts only, never same origin', () => {
    render(<PreviewFrame src="/api/library/demo/preview/file?path=x.html" note="note" openLabel="open" />);

    const frame = screen.getByTitle('preview');

    expect(frame).toHaveAttribute('sandbox', 'allow-scripts');
    expect(frame.getAttribute('sandbox')).not.toContain('allow-same-origin');
  });
});

describe('the evidence gallery', () => {
  it('opens an image at full size', async () => {
    const user = userEvent.setup();

    withProviders(<EvidenceGallery items={[item({})]} />);

    await user.click(screen.getByRole('button', { name: /shot\.png/i }));

    const dialog = screen.getByRole('dialog');

    expect(dialog).toBeInTheDocument();
    expect(dialog.querySelectorAll('img')).toHaveLength(1);
  });

  it('names a report card and keeps its group', () => {
    withProviders(
      <EvidenceGallery
        items={[item({ relPath: '.orchestra/evidence/0001-demo/phase-A/report.md', title: 'Phase report' })]}
      />,
    );

    expect(screen.getByText('Phase report')).toBeInTheDocument();
    expect(screen.getByText('Report')).toBeInTheDocument();
    expect(screen.getByText('0001-demo/phase-A')).toBeInTheDocument();
  });
});

describe('the status badge', () => {
  it('translates a plan status and an idea status, and hides a missing one', () => {
    const { container, rerender } = withProviders(<LibraryStatusBadge status="Done" kind="plan" />);

    expect(screen.getByText('Done')).toBeInTheDocument();

    rerender(
      <NextIntlClientProvider locale="en" messages={messages}>
        <LibraryStatusBadge status="planned" kind="idea" />
      </NextIntlClientProvider>,
    );

    expect(screen.getByText('Planned')).toBeInTheDocument();

    rerender(
      <NextIntlClientProvider locale="en" messages={messages}>
        <LibraryStatusBadge status={null} kind="idea" />
      </NextIntlClientProvider>,
    );

    expect(container.querySelector('[data-slot="badge"]')).toBeNull();
  });
});
