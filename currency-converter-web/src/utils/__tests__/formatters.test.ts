import { describe, it, expect } from 'vitest';
import {
  formatCurrency,
  formatRate,
  formatAmount,
  formatDate,
  formatDateLong,
  formatDateForApi,
} from '../formatters';

describe('formatCurrency', () => {
  it('formats numbers with 2 decimals by default', () => {
    expect(formatCurrency(123.456)).toBe('123.46');
  });

  it('formats numbers with custom decimals', () => {
    expect(formatCurrency(123.456, 4)).toBe('123.4560');
  });

  it('rounds correctly', () => {
    expect(formatCurrency(123.995, 2)).toBe('124.00');
  });
});

describe('formatRate', () => {
  it('formats rates with proper precision', () => {
    expect(formatRate(1.1794)).toBe('1.1794');
  });

  it('uses locale-specific formatting', () => {
    const result = formatRate(1234.56789);
    // Should contain the number with thousands separator
    expect(result).toContain('1');
    expect(result).toContain('234');
  });

  it('handles small decimal rates', () => {
    const result = formatRate(0.001234);
    expect(result).toContain('0.001234');
  });
});

describe('formatAmount', () => {
  it('formats with currency symbol for USD', () => {
    const result = formatAmount(100, 'USD');
    expect(result).toContain('100');
    // Should contain dollar sign or USD
    expect(result.match(/\$|USD/)).toBeTruthy();
  });

  it('formats with currency symbol for EUR', () => {
    const result = formatAmount(100, 'EUR');
    expect(result).toContain('100');
    // Should contain euro sign or EUR
    expect(result.match(/€|EUR/)).toBeTruthy();
  });

  it('falls back for invalid currency codes', () => {
    const result = formatAmount(100, 'INVALID');
    expect(result).toBe('100.00 INVALID');
  });

  it('formats with 2 decimal places', () => {
    const result = formatAmount(100.456, 'USD');
    expect(result).toContain('100.46');
  });
});

describe('formatDate', () => {
  it('formats ISO date string to readable format', () => {
    expect(formatDate('2026-02-06')).toBe('Feb 06, 2026');
  });

  it('handles full ISO datetime strings', () => {
    expect(formatDate('2026-02-06T12:00:00Z')).toBe('Feb 06, 2026');
  });

  it('returns original string for invalid dates', () => {
    expect(formatDate('invalid')).toBe('invalid');
  });
});

describe('formatDateLong', () => {
  it('formats date to long format', () => {
    expect(formatDateLong('2026-02-06')).toBe('February 06, 2026');
  });

  it('handles full ISO datetime strings', () => {
    expect(formatDateLong('2026-02-06T12:00:00Z')).toBe('February 06, 2026');
  });

  it('returns original string for invalid dates', () => {
    expect(formatDateLong('invalid')).toBe('invalid');
  });
});

describe('formatDateForApi', () => {
  it('formats Date object to YYYY-MM-DD', () => {
    const date = new Date('2026-02-06T12:00:00Z');
    expect(formatDateForApi(date)).toBe('2026-02-06');
  });

  it('handles different dates correctly', () => {
    const date = new Date('2025-12-25T00:00:00Z');
    expect(formatDateForApi(date)).toBe('2025-12-25');
  });
});
