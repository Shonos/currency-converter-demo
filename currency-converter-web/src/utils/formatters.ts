import { format, parseISO } from 'date-fns';

/**
 * Format a number as currency with appropriate decimals
 */
export const formatCurrency = (value: number, decimals: number = 2): string => {
  return value.toFixed(decimals);
};

/**
 * Format an exchange rate with appropriate precision
 */
export const formatRate = (rate: number): string => {
  return rate.toLocaleString(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 6,
  });
};

/**
 * Format amount with currency symbol
 */
export const formatAmount = (amount: number, currency: string): string => {
  try {
    return new Intl.NumberFormat(undefined, {
      style: 'currency',
      currency,
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(amount);
  } catch {
    // Fallback if currency code is invalid
    return `${amount.toFixed(2)} ${currency}`;
  }
};

/**
 * Format a date string to a readable format
 */
export const formatDate = (dateString: string): string => {
  try {
    return format(parseISO(dateString), 'MMM dd, yyyy');
  } catch {
    return dateString;
  }
};

/**
 * Format date to long format (e.g., "February 6, 2026")
 */
export const formatDateLong = (dateString: string): string => {
  try {
    return format(parseISO(dateString), 'MMMM dd, yyyy');
  } catch {
    return dateString;
  }
};

/**
 * Format a date for API consumption (YYYY-MM-DD)
 */
export const formatDateForApi = (date: Date): string => {
  return format(date, 'yyyy-MM-dd');
};
