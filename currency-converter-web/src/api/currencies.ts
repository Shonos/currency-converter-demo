import apiClient from './client';
import type { Currency, CurrenciesResponse } from '@/types/currency';

/**
 * Get list of all supported currencies
 */
export const getCurrencies = async (): Promise<Currency[]> => {
  const response = await apiClient.get<CurrenciesResponse>(
    '/api/v1/currencies'
  );
  
  return response.data.currencies;
};
