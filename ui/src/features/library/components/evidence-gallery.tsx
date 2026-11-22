'use client';

import { FileText } from 'lucide-react';
import { useTranslations } from 'next-intl';
import { useState } from 'react';
import { Markdown } from '@/components/app/markdown';
import { TableSkeleton } from '@/components/app/table-skeleton';
import { Button } from '@/components/ui/button';
import { fileUrl } from '../api';
import { useLibraryText } from '../queries';
import type { LibraryItem } from '../types';

const IMAGES = ['.png', '.jpg', '.jpeg', '.webp', '.gif'];

function isImage(item: LibraryItem): boolean {
  return IMAGES.some((extension) => item.relPath.toLowerCase().endsWith(extension));
}

function isReport(item: LibraryItem): boolean {
  return item.relPath.toLowerCase().endsWith('report.md');
}

/** D9: an image opens at full size, a report as markdown; both through the file endpoint. */
export function EvidenceGallery({ items }: { items: LibraryItem[] }) {
  const t = useTranslations('library');
  const [selected, setSelected] = useState<LibraryItem | null>(null);

  const report = useLibraryText(
    selected?.projectId ?? null,
    'Evidence',
    selected && isReport(selected) ? selected.relPath : null,
  );

  return (
    <>
      <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
        {items.map((item) => (
          <button
            key={`${item.projectId}.${item.relPath}`}
            type="button"
            onClick={() => setSelected(item)}
            className="group space-y-2 rounded-lg border p-2 text-start transition-colors hover:bg-muted/40"
          >
            {isImage(item) ? (
              // eslint-disable-next-line @next/next/no-img-element -- a library file, not an asset we bundle.
              <img
                src={fileUrl(item.projectId, item.category, item.relPath)}
                alt={item.title}
                loading="lazy"
                className="h-32 w-full rounded object-cover"
              />
            ) : (
              <span className="flex h-32 items-center justify-center rounded bg-muted">
                <FileText className="size-6 text-muted-foreground" />
              </span>
            )}

            <span className="block text-sm font-medium">{item.title}</span>
            <span className="block text-xs text-muted-foreground" data-ltr>
              {isReport(item) ? t('evidenceReport') : item.group ?? item.relPath}
            </span>
            {isReport(item) && item.group ? (
              <span className="block text-xs text-muted-foreground" data-ltr>
                {item.group}
              </span>
            ) : null}
          </button>
        ))}
      </div>

      {selected ? (
        <div
          role="dialog"
          aria-modal="true"
          aria-label={selected.title}
          className="fixed inset-0 z-50 grid place-items-center bg-foreground/50 p-4"
          onClick={() => setSelected(null)}
        >
          <div
            className="max-h-[90vh] w-full max-w-3xl space-y-3 overflow-auto rounded-lg border bg-background p-4"
            onClick={(event) => event.stopPropagation()}
          >
            <div className="flex items-start justify-between gap-3">
              <p className="text-sm font-medium">{selected.title}</p>
              <Button type="button" variant="outline" size="sm" onClick={() => setSelected(null)}>
                ×
              </Button>
            </div>

            {isImage(selected) ? (
              // eslint-disable-next-line @next/next/no-img-element -- a library file, not an asset we bundle.
              <img
                src={fileUrl(selected.projectId, selected.category, selected.relPath)}
                alt={selected.title}
                className="w-full rounded"
              />
            ) : report.isPending ? (
              <TableSkeleton rows={4} />
            ) : (
              <Markdown source={report.data ?? ''} />
            )}
          </div>
        </div>
      ) : null}
    </>
  );
}
