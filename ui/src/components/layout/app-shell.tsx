'use client';

import { useTranslations } from 'next-intl';
import type { ReactNode } from 'react';
import { brand } from '@/config/brand';
import { ReimportButton } from '@/features/imports';
import { AppSidebar, AppSidebarTrigger } from './app-sidebar';
import { LocaleSwitcher } from './locale-switcher';
import { ThemeSwitcher } from './theme-switcher';

export function AppShell({ children }: { children: ReactNode }) {
  const t = useTranslations('common');

  return (
    <div className="flex min-h-svh bg-background">
      <AppSidebar />

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex h-14 items-center justify-between gap-2 border-b px-3">
          <div className="flex items-center gap-1">
            <AppSidebarTrigger />
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
