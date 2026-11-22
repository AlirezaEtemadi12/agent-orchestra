'use client';

import { Button } from '@/components/ui/button';

type ErrorStateProps = {
  title: string;
  retryLabel: string;
  onRetry: () => void;
};

export function ErrorState({ title, retryLabel, onRetry }: ErrorStateProps) {
  return (
    <div
      role="alert"
      className="flex flex-wrap items-center justify-between gap-2 rounded-xl border border-destructive/40 bg-destructive/5 px-4 py-3 text-sm text-destructive"
    >
      <span>{title}</span>
      <Button type="button" size="sm" variant="outline" onClick={onRetry}>
        {retryLabel}
      </Button>
    </div>
  );
}
