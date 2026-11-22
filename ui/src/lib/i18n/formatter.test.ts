import { describe, expect, it } from 'vitest';
import {
  formatDate,
  formatDuration,
  formatMoney,
  formatPercent,
  formatTokens,
} from './formatter';

describe('formatter', () => {
  it('writes the Persian date in the Jalali calendar with Latin digits', () => {
    expect(formatDate('fa', '2026-11-19T12:00:00Z')).toMatch(/^1405\/0?8\/0?28$/);
  });

  it('writes the English date in the medium English style', () => {
    expect(formatDate('en', '2026-11-19T12:00:00Z')).toBe('Nov 19, 2026');
  });

  it('writes a missing cost as a dash', () => {
    expect(formatMoney('en', null)).toBe('—');
    expect(formatMoney('en', 0.387)).toBe('$0.387');
  });

  it('writes tokens and durations as the ledger does', () => {
    expect(formatTokens('en', 529_900)).toBe('529.9k');
    expect(formatTokens('en', 2_200_000)).toBe('2.2m');
    expect(formatTokens('en', 0)).toBe('0');
    expect(formatDuration(1_325)).toBe('22:05');
    expect(formatDuration(9_481)).toBe('2:38:01');
  });

  it('writes a share as a percentage', () => {
    expect(formatPercent('en', 0.5)).toBe('50%');
  });
});
