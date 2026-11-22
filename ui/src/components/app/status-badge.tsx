import { Badge } from '@/components/ui/badge';

const TONES: Record<string, string> = {
  Verified: 'bg-success/15 text-success',
  VerifyFailed: 'bg-destructive/15 text-destructive',
  Violation: 'bg-destructive/15 text-destructive',
  Approved: 'bg-primary/15 text-primary',
  Implementing: 'bg-info/15 text-info',
  Planning: 'bg-warning/15 text-warning',
  AwaitingApproval: 'bg-warning/15 text-warning',
};

export function StatusBadge({ status }: { status: string }) {
  return (
    <Badge variant="secondary" className={TONES[status] ?? 'bg-muted text-muted-foreground'}>
      <span data-ltr>{status}</span>
    </Badge>
  );
}
