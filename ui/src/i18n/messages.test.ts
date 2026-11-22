import { describe, expect, it } from 'vitest';
import { messages } from './messages';

function flatten(value: unknown, prefix = ''): Map<string, string> {
  const flat = new Map<string, string>();

  if (typeof value !== 'object' || value === null) {
    flat.set(prefix, String(value));
    return flat;
  }

  for (const [key, child] of Object.entries(value as Record<string, unknown>)) {
    const path = prefix === '' ? key : `${prefix}.${key}`;

    for (const [childPath, text] of flatten(child, path)) {
      flat.set(childPath, text);
    }
  }

  return flat;
}

describe('message catalogues', () => {
  const fa = flatten(messages.fa);
  const en = flatten(messages.en);

  it('has the same keys in Persian and English', () => {
    expect([...fa.keys()].sort()).toEqual([...en.keys()].sort());
  });

  it('has no empty message in either language', () => {
    for (const [key, text] of [...fa, ...en]) {
      expect(text.trim().length, key).toBeGreaterThan(0);
    }
  });
});
