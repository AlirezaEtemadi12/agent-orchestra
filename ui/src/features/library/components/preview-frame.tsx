/// D6 / §6.4: scripts run, nothing else does. Never `allow-same-origin` — the frame must not share the
/// dashboard's origin, storage or cookies.
export function PreviewFrame({
  src,
  note,
  openLabel,
}: {
  src: string;
  note: string;
  openLabel: string;
}) {
  return (
    <div className="space-y-2">
      <iframe
        title="preview"
        src={src}
        sandbox="allow-scripts"
        referrerPolicy="no-referrer"
        className="h-[70vh] w-full rounded-md border bg-background"
      />
      <p className="text-xs text-muted-foreground">{note}</p>
      <a
        href={src}
        target="_blank"
        rel="noreferrer"
        className="text-sm underline underline-offset-4"
        data-ltr
      >
        {openLabel}
      </a>
    </div>
  );
}
