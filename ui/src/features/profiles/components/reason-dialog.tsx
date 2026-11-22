'use client';

import { Dialog } from 'radix-ui';
import { useState } from 'react';
import { useTranslations } from 'next-intl';
import { Button } from '@/components/ui/button';

type ReasonDialogProps = {
  open: boolean;
  busy: boolean;
  onOpenChange: (open: boolean) => void;
  onConfirm: (reason: string) => void;
};

export function ReasonDialog({ open, busy, onOpenChange, onConfirm }: ReasonDialogProps) {
  const t = useTranslations('profiles');
  const common = useTranslations('common');
  const [reason, setReason] = useState('');
  const valid = reason.trim().length >= 1 && reason.trim().length <= 300;

  return (
    <Dialog.Root
      open={open}
      onOpenChange={(next) => {
        if (!next) {
          setReason('');
        }

        onOpenChange(next);
      }}
    >
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-50 bg-black/20" />
        <Dialog.Content className="fixed top-1/2 left-1/2 z-50 w-[min(90vw,26rem)] -translate-x-1/2 -translate-y-1/2 space-y-3 rounded-xl border bg-popover p-5 text-popover-foreground shadow-lg">
          <Dialog.Title className="text-base font-medium">{t('save')}</Dialog.Title>
          <Dialog.Description className="text-sm text-muted-foreground">{t('reason')}</Dialog.Description>
          <input
            className="h-9 w-full rounded-md border bg-background px-2 text-sm"
            value={reason}
            maxLength={300}
            autoFocus
            onChange={(event) => setReason(event.target.value)}
          />
          <div className="flex justify-end gap-2 pt-2">
            <Dialog.Close asChild>
              <Button type="button" variant="outline" size="sm">
                {common('actions.back')}
              </Button>
            </Dialog.Close>
            <Button
              type="button"
              size="sm"
              disabled={!valid || busy}
              onClick={() => {
                if (valid) {
                  onConfirm(reason.trim());
                  setReason('');
                }
              }}
            >
              {t('save')}
            </Button>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
