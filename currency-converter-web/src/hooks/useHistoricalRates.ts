import { useQuery } from '@tanstack/react-query';
import { getHistoricalRates } from '@/api/exchangeRates';

export const useHistoricalRates = (
  baseCurrency: string,
  startDate: string,
  endDate: string,
  page: number = 1,
  pageSize: number = 10
) => {
  return useQuery({
    queryKey: ['historicalRates', baseCurrency, startDate, endDate, page, pageSize],
    queryFn: () =>
      getHistoricalRates(baseCurrency, startDate, endDate, page, pageSize),
    enabled: !!baseCurrency && !!startDate && !!endDate,
  });
};
