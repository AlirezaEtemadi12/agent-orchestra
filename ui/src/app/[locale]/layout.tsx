import type { Metadata } from 'next';
import { hasLocale, NextIntlClientProvider } from 'next-intl';
import { setRequestLocale } from 'next-intl/server';
import { notFound } from 'next/navigation';
import type { ReactNode } from 'react';
import { AppShell } from '@/components/layout/app-shell';
import { AppProviders } from '@/components/providers/app-providers';
import { brand } from '@/config/brand';
import { routing } from '@/i18n/routing';
import { localeHtmlProps, type Locale } from '@/lib/i18n/config';

type LocaleLayoutProps = {
  children: ReactNode;
  params: Promise<{ locale: string }>;
};

export function generateStaticParams() {
  return routing.locales.map((locale) => ({ locale }));
}

export const metadata: Metadata = {
  title: { default: brand.name, template: `%s · ${brand.name}` },
};

export default async function LocaleLayout({ children, params }: LocaleLayoutProps) {
  const { locale } = await params;

  if (!hasLocale(routing.locales, locale)) {
    notFound();
  }

  setRequestLocale(locale as Locale);

  const html = localeHtmlProps(locale as Locale);

  return (
    <html lang={html.lang} dir={html.dir} suppressHydrationWarning>
      <body className="min-h-svh antialiased">
        <NextIntlClientProvider>
          <AppProviders dir={html.dir}>
            <AppShell>{children}</AppShell>
          </AppProviders>
        </NextIntlClientProvider>
      </body>
    </html>
  );
}
