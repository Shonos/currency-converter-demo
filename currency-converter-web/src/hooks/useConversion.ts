import { useQuery } from '@tanstack/react-query';
import { convertCurrency } from '@/api/exchangeRates';

export const useConversion = (
  from: string,
  to: string,
  amount: number,
  enabled: boolean = true
) => {
  return useQuery({
    queryKey: ['conversion', from, to, amount],
    queryFn: () => convertCurrency(from, to, amount),
    enabled: enabled && !!from && !!to && amount > 0,
  });
};
