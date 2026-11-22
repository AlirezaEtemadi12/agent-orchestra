'use client';

import { useTranslations } from 'next-intl';
import { useRef, useState } from 'react';
import { Button } from '@/components/ui/button';
import { copyText, selectText } from '../clipboard';

export function CopyPrompt({ text }: { text: string }) {
  const t = useTranslations('library');
  const [state, setState] = useState<'idle' | 'copied' | 'failed'>('idle');
  const promptRef = useRef<HTMLPreElement>(null);

  const copy = async (): Promise<void> => {
    if (await copyText(text)) {
      setState('copied');
      return;
    }

    // The fallback: the prompt is selected, so Ctrl+C copies it.
    selectText(promptRef.current);
    setState('failed');
  };

  return (
    <div className="space-y-2">
      <span title={t('copyPromptHint')}>
        <Button type="button" size="sm" onClick={() => void copy()}>
          {t('copyPrompt')}
        </Button>
      </span>

      {state === 'copied' ? <p className="text-xs text-success">{t('copied')}</p> : null}
      {state === 'failed' ? <p className="text-xs text-destructive">{t('copyFailed')}</p> : null}

      <pre
        ref={promptRef}
        data-ltr
        className="max-h-40 overflow-auto rounded-md border bg-muted/40 p-2 text-xs whitespace-pre-wrap"
      >
        {text}
      </pre>
    </div>
  );
}
