import apiClient from './client';
import type {
  LatestRatesResponse,
  ConversionResponse,
  PagedHistoricalRatesResponse,
} from '@/types/currency';

/**
 * Get latest exchange rates for a base currency
 */
export const getLatestRates = async (
  baseCurrency: string
): Promise<LatestRatesResponse> => {
  const response = await apiClient.get<LatestRatesResponse>(
    `/api/v1/exchange-rates/latest`,
    {
      params: { baseCurrency },
    }
  );
  return response.data;
};

/**
 * Convert currency amount
 */
export const convertCurrency = async (
  from: string,
  to: string,
  amount: number
): Promise<ConversionResponse> => {
  const response = await apiClient.get<ConversionResponse>(
    `/api/v1/exchange-rates/convert`,
    {
      params: { from, to, amount },
    }
  );
  return response.data;
};

/**
 * Get historical exchange rates with pagination
 */
export const getHistoricalRates = async (
  baseCurrency: string,
  startDate: string,
  endDate: string,
  page: number = 1,
  pageSize: number = 10
): Promise<PagedHistoricalRatesResponse> => {
  const response = await apiClient.get<PagedHistoricalRatesResponse>(
    `/api/v1/exchange-rates/history`,
    {
      params: {
        baseCurrency,
        startDate,
        endDate,
        page,
        pageSize,
      },
    }
  );
  return response.data;
};
