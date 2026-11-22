import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join, relative } from 'node:path';
import { describe, expect, it } from 'vitest';
import { FEATURES } from '@/features/registry';
import { FEATURE_KEYS } from '@/lib/features/define-feature';

const SRC = join(process.cwd(), 'src');

function files(directory = SRC): string[] {
  return readdirSync(directory).flatMap((entry) => {
    const full = join(directory, entry);

    if (statSync(full).isDirectory()) {
      return files(full);
    }

    return /\.(ts|tsx)$/.test(entry) ? [full] : [];
  });
}

const sources = files()
  .filter((file) => !/\.test\.tsx?$/.test(file))
  .map((file) => ({ file, path: relative(SRC, file).replaceAll('\\', '/'), text: readFileSync(file, 'utf8') }));

const imports = (text: string): string[] => [...text.matchAll(/from '([^']+)'/g)].map((match) => match[1]);

describe('architecture', () => {
  it('registers every feature exactly once', () => {
    expect(FEATURES.map((feature) => feature.key).sort()).toEqual([...FEATURE_KEYS].sort());
    expect(new Set(FEATURES.map((feature) => feature.key)).size).toBe(FEATURE_KEYS.length);
  });

  it('imports a feature only through its index', () => {
    const offenders = sources.filter(({ path, text }) =>
      imports(text).some((target) => {
        const match = /^@\/features\/([a-z]+)\/.+/.exec(target);
        return match !== null && !path.startsWith(`features/${match[1]}/`);
      }),
    );

    expect(offenders.map(({ path }) => path)).toEqual([]);
  });

  it('never lets a feature import another feature', () => {
    const offenders = sources
      .filter(({ path }) => path.startsWith('features/'))
      .filter(({ path, text }) => {
        const own = path.split('/')[1];
        return imports(text).some((target) => {
          const match = /^@\/features\/([a-z]+)/.exec(target);
          return match !== null && match[1] !== own;
        });
      });

    expect(offenders.map(({ path }) => path)).toEqual([]);
  });

  it('never lets lib or the shared components import a feature', () => {
    const offenders = sources
      .filter(({ path }) => path.startsWith('lib/') || /^components\/(ui|app)\//.test(path))
      .filter(({ text }) => imports(text).some((target) => target === '@/features/registry' || target.startsWith('@/features/')));

    expect(offenders.map(({ path }) => path)).toEqual([]);
  });

  it('calls fetch only in the api client', () => {
    const offenders = sources.filter(
      ({ path, text }) => path !== 'lib/api/client.ts' && /\bfetch\(/.test(text),
    );

    expect(offenders.map(({ path }) => path)).toEqual([]);
  });

  it('formats through the formatter only', () => {
    const offenders = sources.filter(
      ({ path, text }) =>
        path !== 'lib/i18n/formatter.ts' &&
        (/new Intl\.\w*Format\(/.test(text) || /useFormatter\(/.test(text)),
    );

    expect(offenders.map(({ path }) => path)).toEqual([]);
  });

  it('keeps colours in the tokens, not in the components', () => {
    const offenders = sources.filter(({ text }) => /#[0-9a-fA-F]{3,8}\b/.test(text));

    expect(offenders.map(({ path }) => path)).toEqual([]);
  });
});
