import { useQuery } from '@tanstack/react-query';
import { getCurrencies } from '@/api/currencies';

export const useCurrencies = () => {
  return useQuery({
    queryKey: ['currencies'],
    queryFn: getCurrencies,
    staleTime: 24 * 60 * 60 * 1000, // 24 hours (currencies don't change often)
  });
};
