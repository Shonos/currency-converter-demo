import { useQuery } from '@tanstack/react-query';
import { getLatestRates } from '@/api/exchangeRates';

export const useLatestRates = (baseCurrency: string) => {
  return useQuery({
    queryKey: ['latestRates', baseCurrency],
    queryFn: () => getLatestRates(baseCurrency),
    enabled: !!baseCurrency,
  });
};
