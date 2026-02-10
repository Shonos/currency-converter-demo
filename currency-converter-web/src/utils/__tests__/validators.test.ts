import { describe, it, expect } from 'vitest';
import { validateConversionForm } from '../validators';

describe('validateConversionForm', () => {
  it('returns null for valid input', () => {
    const error = validateConversionForm('EUR', 'USD', 100);
    expect(error).toBeNull();
  });

  it('returns error for missing source currency', () => {
    const error = validateConversionForm('', 'USD', 100);
    expect(error).toBe('Please select a source currency');
  });

  it('returns error for missing target currency', () => {
    const error = validateConversionForm('EUR', '', 100);
    expect(error).toBe('Please select a target currency');
  });

  it('returns error for same source and target currency', () => {
    const error = validateConversionForm('EUR', 'EUR', 100);
    expect(error).toBe('Source and target currencies must be different');
  });

  it('returns error for zero amount', () => {
    const error = validateConversionForm('EUR', 'USD', 0);
    expect(error).toBe('Amount must be greater than 0');
  });

  it('returns error for negative amount', () => {
    const error = validateConversionForm('EUR', 'USD', -10);
    expect(error).toBe('Amount must be greater than 0');
  });

  it('returns error for NaN amount', () => {
    const error = validateConversionForm('EUR', 'USD', NaN);
    expect(error).toBe('Amount must be greater than 0');
  });

  it('accepts very small positive amounts', () => {
    const error = validateConversionForm('EUR', 'USD', 0.01);
    expect(error).toBeNull();
  });

  it('accepts large amounts', () => {
    const error = validateConversionForm('EUR', 'USD', 1000000);
    expect(error).toBeNull();
  });
});
