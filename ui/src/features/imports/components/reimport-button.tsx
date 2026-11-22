'use client';

import { useTranslations } from 'next-intl';
import { Button } from '@/components/ui/button';
import { useReimport } from '../queries';

export function ReimportButton() {
  const t = useTranslations('common');
  const reimport = useReimport();

  return (
    <div className="flex items-center gap-2">
      <Button
        type="button"
        size="sm"
        variant="outline"
        disabled={reimport.isPending}
        onClick={() => reimport.mutate()}
      >
        {t('actions.reimport')}
      </Button>
      {reimport.isError ? <span className="text-xs text-destructive">{t('errors.unexpected')}</span> : null}
    </div>
  );
}
