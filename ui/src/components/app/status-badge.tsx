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

/// Plan 0013 D6: `label` is the owner's word for the state ("In review", «در بازبینی»); the raw engine
/// word stays the fallback, so a status the catalogue does not know still shows something.
export function StatusBadge({ status, label }: { status: string; label?: string }) {
  return (
    <Badge variant="secondary" className={TONES[status] ?? 'bg-muted text-muted-foreground'}>
      <span data-ltr>{label ?? status}</span>
    </Badge>
  );
}
