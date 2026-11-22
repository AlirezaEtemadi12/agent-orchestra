import Link from 'next/link';

export default function NotFound() {
  return (
    <html lang="en">
      <body className="flex min-h-svh items-center justify-center bg-background p-6 text-foreground">
        <main className="space-y-3 text-center">
          <p className="text-sm font-medium">404</p>
          <p className="text-sm text-muted-foreground">
            This page does not exist. — این صفحه وجود ندارد.
          </p>
          <p className="flex items-center justify-center gap-3 text-sm">
            <Link className="text-primary underline-offset-4 hover:underline" href="/fa/">
              فارسی
            </Link>
            <Link className="text-primary underline-offset-4 hover:underline" href="/en/">
              English
            </Link>
          </p>
        </main>
      </body>
    </html>
  );
}
