'use client';

import { useAppFormatter } from '@/lib/i18n/formatter';

const CELL = 'tabular-nums';

export function MoneyText({ usd }: { usd: number | null }) {
  const formatter = useAppFormatter();

  return (
    <span className={CELL} data-ltr>
      {formatter.money(usd)}
    </span>
  );
}

export function TokensText({ value }: { value: number | null }) {
  const formatter = useAppFormatter();

  if (value === null) {
    return <span className="text-muted-foreground">—</span>;
  }

  return (
    <span className={CELL} data-ltr>
      {formatter.tokens(value)}
    </span>
  );
}

export function DurationText({ seconds }: { seconds: number | null }) {
  const formatter = useAppFormatter();

  if (seconds === null) {
    return <span className="text-muted-foreground">—</span>;
  }

  return (
    <span className={CELL} data-ltr>
      {formatter.duration(seconds)}
    </span>
  );
}

export function DateTimeText({ value }: { value: string | null }) {
  const formatter = useAppFormatter();

  if (!value) {
    return <span className="text-muted-foreground">—</span>;
  }

  return <span className={CELL}>{formatter.dateTime(value)}</span>;
}

export function PercentText({ value }: { value: number | null }) {
  const formatter = useAppFormatter();

  if (value === null) {
    return <span className="text-muted-foreground">—</span>;
  }

  return <span className={CELL}>{formatter.percent(value)}</span>;
}
