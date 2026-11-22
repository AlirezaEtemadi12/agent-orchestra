import type { ReactNode } from 'react';

type BarListItem = {
  label: string;
  value: number;
  display: ReactNode;
};

export function BarList({ items }: { items: BarListItem[] }) {
  const max = Math.max(...items.map((item) => item.value), 1);

  return (
    <ul className="space-y-2">
      {items.map((item) => (
        <li key={item.label} className="space-y-1">
          <div className="flex items-baseline justify-between gap-3 text-sm">
            <span className="truncate" data-ltr>
              {item.label}
            </span>
            <span className="shrink-0 tabular-nums" data-ltr>
              {item.display}
            </span>
          </div>
          <div className="h-2 rounded-full bg-muted">
            <div
              className="h-2 rounded-full bg-chart-1"
              style={{ width: `${Math.max(2, (item.value / max) * 100)}%` }}
            />
          </div>
        </li>
      ))}
    </ul>
  );
}
