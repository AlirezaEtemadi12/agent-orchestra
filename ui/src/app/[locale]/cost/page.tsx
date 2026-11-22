import type { Metadata } from 'next';
import { setRequestLocale } from 'next-intl/server';
import { CostPage } from '@/features/cost';
import type { Locale } from '@/lib/i18n/config';
import { pageMetadata } from '@/lib/i18n/page-metadata';

type PageProps = { params: Promise<{ locale: string }> };

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { locale } = await params;

  return pageMetadata(locale as Locale, 'cost');
}

export default async function CostRoute({ params }: PageProps) {
  const { locale } = await params;
  setRequestLocale(locale as Locale);

  return <CostPage />;
}
