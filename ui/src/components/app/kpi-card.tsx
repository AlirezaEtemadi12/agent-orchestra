import type { ReactNode } from 'react';
import { Card, CardContent } from '@/components/ui/card';

type KpiCardProps = {
  label: string;
  value: ReactNode;
};

export function KpiCard({ label, value }: KpiCardProps) {
  return (
    <Card className="py-3">
      <CardContent className="space-y-1 px-4">
        <p className="text-xs text-muted-foreground">{label}</p>
        <p className="text-lg font-semibold tabular-nums" data-ltr>
          {value}
        </p>
      </CardContent>
    </Card>
  );
}
