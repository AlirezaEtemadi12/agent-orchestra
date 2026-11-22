import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { describe, expect, it } from 'vitest';

const html = readFileSync(join(process.cwd(), 'public/index.html'), 'utf8');
const script = /<script>([\s\S]*?)<\/script>/.exec(html)?.[1] ?? '';

type Options = {
  stored?: string | null;
  languages?: string[];
  throwOnRead?: boolean;
};

function redirectFor(options: Options): string | null {
  let redirected: string | null = null;

  const window = {
    localStorage: {
      getItem: () => {
        if (options.throwOnRead) {
          throw new Error('storage is blocked');
        }

        return options.stored ?? null;
      },
    },
  };

  const navigator = { languages: options.languages ?? [], language: options.languages?.[0] ?? '' };

  const location = {
    replace: (url: string) => {
      redirected = url;
    },
  };

  new Function('window', 'navigator', 'location', script)(window, navigator, location);

  return redirected;
}

describe('public/index.html', () => {
  it('uses the stored locale', () => {
    expect(redirectFor({ stored: 'fa' })).toBe('/fa/');
  });

  it('falls back to the browser language', () => {
    expect(redirectFor({ languages: ['fa-IR', 'en'] })).toBe('/fa/');
    expect(redirectFor({ languages: ['en-US'] })).toBe('/en/');
  });

  it('uses English when neither language matches', () => {
    expect(redirectFor({ languages: ['de-DE'] })).toBe('/en/');
  });

  it('still redirects when the storage is blocked', () => {
    expect(redirectFor({ throwOnRead: true, languages: ['fa'] })).toBe('/fa/');
  });
});
