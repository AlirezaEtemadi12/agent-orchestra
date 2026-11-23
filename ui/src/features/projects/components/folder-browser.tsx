'use client';

import { useTranslations } from 'next-intl';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Sheet, SheetContent, SheetHeader, SheetTitle } from '@/components/ui/sheet';
import { errorText } from '../projects-errors';
import { useFolders } from '../queries';

type Props = {
  open: boolean;
  path: string | null;
  onPath: (path: string | null) => void;
  onOpenChange: (open: boolean) => void;
  onChoose: (path: string) => void;
};

/** Folder names only, inside the roots the API allows; the Windows picker is not reachable from a browser. */
export function FolderBrowser({ open, path, onPath, onOpenChange, onChoose }: Props) {
  const t = useTranslations('projects');
  const common = useTranslations('common');
  const folders = useFolders(path, open);
  const page = folders.data;

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side="right" className="w-full sm:max-w-md">
        <SheetHeader>
          <SheetTitle>{t('form.browse')}</SheetTitle>
        </SheetHeader>

        <div className="flex items-center gap-2 px-4">
          <Button
            type="button"
            size="sm"
            variant="ghost"
            disabled={path === null}
            onClick={() => onPath(page ? page.parent : null)}
          >
            {t('form.up')}
          </Button>
          <span className="truncate text-xs text-muted-foreground" data-ltr>
            {page?.path ?? path ?? ''}
          </span>
        </div>

        {folders.isError ? (
          <p className="px-4 text-sm text-destructive">{errorText(folders.error, common('errors.unexpected'))}</p>
        ) : null}

        <ul className="min-h-0 flex-1 space-y-1 overflow-auto px-2 pb-2">
          {(page?.folders ?? []).map((folder) => (
            <li key={folder.path}>
              <button
                type="button"
                className="flex w-full items-center justify-between gap-2 rounded-lg px-2 py-1.5 text-start text-sm hover:bg-accent"
                onClick={() => onPath(folder.path)}
              >
                <span className="truncate" data-ltr>
                  {folder.name}
                </span>
                <span className="flex shrink-0 items-center gap-1">
                  {folder.isReady ? <Badge variant="secondary">{t('form.badgeReady')}</Badge> : null}
                  {folder.isRepo ? <Badge variant="outline">{t('form.badgeRepo')}</Badge> : null}
                </span>
              </button>
            </li>
          ))}
        </ul>

        <div className="border-t p-4">
          <Button
            type="button"
            className="w-full"
            disabled={!page?.path}
            onClick={() => {
              if (page?.path) {
                onChoose(page.path);
                onOpenChange(false);
              }
            }}
          >
            {t('form.choose')}
          </Button>
        </div>
      </SheetContent>
    </Sheet>
  );
}
