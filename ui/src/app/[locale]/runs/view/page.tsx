import type { Metadata } from 'next';
import { getTranslations, setRequestLocale } from 'next-intl/server';
import { Suspense } from 'react';
import { TableSkeleton } from '@/components/app/table-skeleton';
import { RunDetail } from '@/features/runs';
import type { Locale } from '@/lib/i18n/config';
import { pageMetadata } from '@/lib/i18n/page-metadata';

type PageProps = { params: Promise<{ locale: string }> };

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { locale } = await params;

  return pageMetadata(locale as Locale, 'runs');
}

export default async function RunDetailPage({ params }: PageProps) {
  const { locale } = await params;
  setRequestLocale(locale as Locale);

  const t = await getTranslations('runs');

  return (
    <Suspense fallback={<TableSkeleton rows={8} />} aria-label={t('detail.stages')}>
      <RunDetail />
    </Suspense>
  );
}
