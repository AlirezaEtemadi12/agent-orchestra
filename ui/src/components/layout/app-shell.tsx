'use client';

import { useTranslations } from 'next-intl';
import { useRouter } from 'next/navigation';
import { useEffect, type ReactNode } from 'react';
import { brand } from '@/config/brand';
import { ReimportButton } from '@/features/imports';
import { AppSidebar, AppSidebarTrigger } from './app-sidebar';
import { HistoryButtons } from './history-buttons';
import { LocaleSwitcher } from './locale-switcher';
import { ThemeSwitcher } from './theme-switcher';

export function AppShell({ children }: { children: ReactNode }) {
  const t = useTranslations('common');
  const router = useRouter();

  // Plan 0013 D14: the two keys do what the two header buttons do, and the browser's own handling is
  // stopped so a reload is not fired twice.
  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.altKey && event.key === 'ArrowLeft') {
        event.preventDefault();
        router.back();

        return;
      }

      if (event.key === 'F5' || (event.ctrlKey && (event.key === 'r' || event.key === 'R'))) {
        event.preventDefault();
        window.location.reload();
      }
    };

    window.addEventListener('keydown', onKeyDown);

    return () => window.removeEventListener('keydown', onKeyDown);
  }, [router]);

  return (
    <div className="flex min-h-svh bg-background">
      <AppSidebar />

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-30 flex h-14 items-center justify-between gap-2 border-b bg-background/95 px-3 backdrop-blur supports-[backdrop-filter]:bg-background/80">
          <div className="flex items-center gap-1">
            <AppSidebarTrigger />
            <HistoryButtons />
            <span className="text-sm font-semibold md:hidden">{brand.name}</span>
            <span className="sr-only md:not-sr-only md:text-sm md:text-muted-foreground">
              {t('appName')}
            </span>
          </div>

          <div className="flex items-center gap-1">
            <ReimportButton />
            <LocaleSwitcher />
            <ThemeSwitcher />
          </div>
        </header>

        <main className="mx-auto w-full max-w-6xl flex-1 space-y-6 p-4 md:p-6">{children}</main>
      </div>
    </div>
  );
}
