import { Badge } from '@/components/ui/badge';

const TONES: Record<string, string> = {
  PASS: 'bg-success/15 text-success',
  FINDINGS: 'bg-warning/15 text-warning',
  NEED_INPUT: 'bg-warning/15 text-warning',
  FAIL: 'bg-destructive/15 text-destructive',
};

export function VerdictBadge({ verdict }: { verdict: string | null }) {
  if (!verdict) {
    return <span className="text-muted-foreground">—</span>;
  }

  return (
    <Badge variant="secondary" className={TONES[verdict] ?? 'bg-muted text-muted-foreground'}>
      <span data-ltr>{verdict}</span>
    </Badge>
  );
}
