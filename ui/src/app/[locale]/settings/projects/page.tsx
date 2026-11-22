import type { Metadata } from 'next';
import { setRequestLocale } from 'next-intl/server';
import { Suspense } from 'react';
import { TableSkeleton } from '@/components/app/table-skeleton';
import { ProjectsPage } from '@/features/projects';
import type { Locale } from '@/lib/i18n/config';
import { pageMetadata } from '@/lib/i18n/page-metadata';

type PageProps = { params: Promise<{ locale: string }> };

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { locale } = await params;

  return pageMetadata(locale as Locale, 'projects');
}

export default async function ProjectsRoute({ params }: PageProps) {
  const { locale } = await params;
  setRequestLocale(locale as Locale);

  return (
    <Suspense fallback={<TableSkeleton rows={4} />}>
      <ProjectsPage />
    </Suspense>
  );
}
