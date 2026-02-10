import { EXCLUDED_CURRENCIES } from './constants';

/**
 * Validate if a currency code is allowed
 */
export const isValidCurrency = (currencyCode: string): boolean => {
  return !EXCLUDED_CURRENCIES.includes(currencyCode.toUpperCase());
};

/**
 * Validate an amount is positive and within reasonable bounds
 */
export const isValidAmount = (amount: number): boolean => {
  return amount > 0 && amount <= 1000000000; // Max 1 billion
};

/**
 * Validate date range (start must be before end)
 */
export const isValidDateRange = (startDate: Date, endDate: Date): boolean => {
  return startDate < endDate;
};

/**
 * Validate currency conversion form
 */
export const validateConversionForm = (
  from: string,
  to: string,
  amount: number
): string | null => {
  if (!from) return 'Please select a source currency';
  if (!to) return 'Please select a target currency';
  if (from === to) return 'Source and target currencies must be different';
  if (!amount || amount <= 0) return 'Amount must be greater than 0';
  if (amount > 1000000000) return 'Amount is too large';
  return null; // valid
};
