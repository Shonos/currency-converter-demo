import React from 'react';
import { useMutation } from '@tanstack/react-query';
import { convertCurrency } from '@/api/exchangeRates';
import { ConversionForm } from './ConversionForm';
import { ConversionResult } from './ConversionResult';
import { ErrorMessage } from '@/components/ui/ErrorMessage';

export const ConversionPage: React.FC = () => {
  const mutation = useMutation({
    mutationFn: ({
      from,
      to,
      amount,
    }: {
      from: string;
      to: string;
      amount: number;
    }) => convertCurrency(from, to, amount),
  });

  const handleConvert = (from: string, to: string, amount: number) => {
    mutation.mutate({ from, to, amount });
  };

  return (
    <div className="max-w-2xl mx-auto">
      <h1 className="text-3xl font-bold text-gray-900 mb-6">
        Currency Conversion
      </h1>

      <div className="space-y-6">
        <ConversionForm onConvert={handleConvert} isLoading={mutation.isPending} />

        {mutation.isError && (
          <ErrorMessage error={mutation.error} onRetry={() => mutation.reset()} />
        )}

        {mutation.isSuccess && mutation.data && (
          <ConversionResult result={mutation.data} />
        )}
      </div>
    </div>
  );
};
