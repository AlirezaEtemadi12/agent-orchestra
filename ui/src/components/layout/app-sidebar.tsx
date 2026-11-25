'use client';

import { Menu } from 'lucide-react';
import { useLocale, useTranslations } from 'next-intl';
import { Button } from '@/components/ui/button';
import { Sheet, SheetContent, SheetTitle, SheetTrigger } from '@/components/ui/sheet';
import { brand } from '@/config/brand';
import { FEATURES } from '@/features/registry';
import { messages } from '@/i18n/messages';
import { Link, usePathname } from '@/i18n/navigation';
import type { Locale } from '@/lib/i18n/config';
import { cn } from '@/lib/utils';

const MENU_CATEGORIES = ['overview', 'work', 'system', 'settings'] as const;

function SidebarNav() {
  const t = useTranslations('common');
  const locale = useLocale() as Locale;
  const pathname = usePathname();

  return (
    <div className="flex h-full flex-col gap-6 p-3">
      <span className="px-2 pt-2 text-sm font-semibold tracking-wide">{brand.name}</span>

      {MENU_CATEGORIES.map((category) => {
        const items = FEATURES.filter((feature) => feature.menu === category);

        if (items.length === 0) {
          return null;
        }

        return (
          <nav key={category} className="space-y-1">
            <p className="px-2 text-xs text-sidebar-foreground/60">{t(`menu.${category}`)}</p>
            {items.map((feature) => {
              const isActive =
                feature.route === '/' ? pathname === '/' : pathname.startsWith(feature.route);
              const Icon = feature.icon;

              return (
                <Link
                  key={feature.key}
                  href={feature.route as never}
                  className={cn(
                    'flex items-center gap-2 rounded-lg px-2 py-1.5 text-sm transition-colors',
                    'hover:bg-sidebar-accent hover:text-sidebar-accent-foreground',
                    isActive && 'bg-sidebar-accent text-sidebar-accent-foreground',
                  )}
                >
                  <Icon className="size-4" />
                  {messages[locale][feature.key].title}
                </Link>
              );
            })}
          </nav>
        );
      })}
    </div>
  );
}

export function AppSidebar() {
  return (
    <aside className="sticky top-0 hidden h-svh w-60 shrink-0 overflow-y-auto border-e border-sidebar-border bg-sidebar text-sidebar-foreground md:block">
      <SidebarNav />
    </aside>
  );
}

export function AppSidebarTrigger() {
  const t = useTranslations('common');
  const locale = useLocale();
  const pathname = usePathname();
  const side = locale === 'fa' ? 'right' : 'left';

  return (
    <div className="md:hidden">
      <Sheet>
        <SheetTrigger asChild>
          <Button variant="ghost" size="icon" aria-label={t('appName')}>
            <Menu className="size-4" />
          </Button>
        </SheetTrigger>
        <SheetContent
          side={side}
          showCloseButton={false}
          className="w-64 border-sidebar-border bg-sidebar p-0 text-sidebar-foreground"
        >
          <SheetTitle className="sr-only">{t('appName')}</SheetTitle>
          <div key={pathname}>
            <SidebarNav />
          </div>
        </SheetContent>
      </Sheet>
    </div>
  );
}
