import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { NextIntlClientProvider } from 'next-intl';
import type { ReactElement } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { commonEn } from '@/lib/i18n/messages/common.en';
import { libraryApi } from './api';
import { copyText, selectText } from './clipboard';
import { EvidenceGallery } from './components/evidence-gallery';
import { LibraryPage } from './components/library-page';
import { PreviewFrame } from './components/preview-frame';
import { LibraryStatusBadge } from './components/status';
import { libraryEn } from './i18n/en';
import {
  LIBRARY_TABS,
  detailQuery,
  detailHref,
  statusColumnVisible,
  tabFor,
  tabFrom,
  tabQuery,
} from './library-params';
import type { LibraryItem } from './types';

const navigation = vi.hoisted(() => {
  let search = new URLSearchParams();

  return {
    push: vi.fn(),
    replace: vi.fn(),
    get search() {
      return search;
    },
    set search(value: URLSearchParams) {
      search = value;
    },
  };
});

const projects = vi.hoisted(() => ({
  data: [] as { id: string; name: string }[],
}));

vi.mock('next/navigation', () => ({
  useSearchParams: () => navigation.search,
  useRouter: () => ({ push: navigation.push, replace: navigation.replace }),
  usePathname: () => '/library/',
  redirect: vi.fn(),
  permanentRedirect: vi.fn(),
}));

vi.mock('@/lib/projects/queries', () => ({
  useProjects: () => ({ data: projects.data }),
}));

vi.mock('./api', () => ({
  libraryApi: {
    table: vi.fn(),
    item: vi.fn(),
    prompt: vi.fn(),
    text: vi.fn(),
  },
  fileUrl: vi.fn(() => '/api/library/demo/evidence/file?path=x'),
  absoluteApiUrl: vi.fn((value: string) => `/api/${value}`),
}));

const messages = { library: libraryEn, common: commonEn };

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

  it('keeps the project in a tab link', () => {
    expect(tabQuery(LIBRARY_TABS[3], 'demo')).toEqual({
      tab: 'research',
      project: 'demo',
    });
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

describe('the library page', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    projects.data = [];
    navigation.search = new URLSearchParams();
  });

  it('keeps the tab and the project in the table request', async () => {
    navigation.search = new URLSearchParams('tab=research&project=demo');
    projects.data = [{ id: 'demo', name: 'Demo' }];
    vi.mocked(libraryApi.table).mockResolvedValue({
      count: 1,
      data: [item({ category: 'Research', title: 'A note' })],
    });

    withProviders(<LibraryPage />);

    expect(await screen.findByText('A note')).toBeInTheDocument();

    const [, filter] = vi.mocked(libraryApi.table).mock.calls[0];

    expect(filter.project).toBe('demo');
    expect(filter.category).toBe('Research');
  });

  it('puts the shared pager under the evidence gallery over one hundred items', async () => {
    navigation.search = new URLSearchParams('tab=evidence');
    vi.mocked(libraryApi.table).mockResolvedValue({ count: 150, data: [] });

    withProviders(<LibraryPage />);

    expect(await screen.findByText('1–100 of 150')).toBeInTheDocument();
  });
});
