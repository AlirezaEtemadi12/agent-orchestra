'use client';

import { ArrowLeft, ArrowRight, RotateCw } from 'lucide-react';
import { useLocale, useTranslations } from 'next-intl';
import { useRouter } from 'next/navigation';
import { Button } from '@/components/ui/button';

/// Plan 0013 D14: back and reload in every build, the browser included. The arrow follows the reading
/// direction: right in Persian, left elsewhere.
export function HistoryButtons() {
  const t = useTranslations('common');
  const locale = useLocale();
  const router = useRouter();
  const Back = locale === 'fa' ? ArrowRight : ArrowLeft;

  return (
    <>
      <Button variant="ghost" size="icon" aria-label={t('actions.back')} onClick={() => router.back()}>
        <Back className="size-4" />
      </Button>
      <Button
        variant="ghost"
        size="icon"
        aria-label={t('actions.reload')}
        onClick={() => window.location.reload()}
      >
        <RotateCw className="size-4" />
      </Button>
    </>
  );
}
