import type { ReactNode } from 'react';

type DayColumn = {
  label: string;
  value: number;
  display: ReactNode;
};

export function DayColumns({ items }: { items: DayColumn[] }) {
  const max = Math.max(...items.map((item) => item.value), 1);

  return (
    <div className="flex items-end gap-2 overflow-x-auto pb-1" dir="ltr">
      {items.map((item) => (
        <div key={item.label} className="flex w-10 shrink-0 flex-col items-center gap-1">
          <span className="text-[0.65rem] text-muted-foreground tabular-nums">{item.display}</span>
          <div
            className="w-6 rounded-t bg-chart-1"
            style={{ height: `${Math.max(4, (item.value / max) * 96)}px` }}
            title={`${item.label}: ${item.value}`}
          />
          <span className="text-[0.6rem] text-muted-foreground tabular-nums">{item.label.slice(5)}</span>
        </div>
      ))}
    </div>
  );
}
