import React, { useState } from 'react';
import { Card } from '@/components/ui/Card';
import { Select } from '@/components/ui/Select';
import { Spinner } from '@/components/ui/Spinner';
import { ErrorMessage } from '@/components/ui/ErrorMessage';
import { useCurrencies } from '@/hooks/useCurrencies';
import { useLatestRates } from '@/hooks/useLatestRates';
import { RatesTable } from './RatesTable';
import { formatDateLong } from '@/utils/formatters';

export const LatestRatesPage: React.FC = () => {
  const [baseCurrency, setBaseCurrency] = useState('EUR');

  const { data: currencies, isLoading: currenciesLoading } = useCurrencies();
  const {
    data: ratesData,
    isLoading: ratesLoading,
    isError,
    error,
    refetch,
  } = useLatestRates(baseCurrency);

  const currencyOptions = currencies
    ? [
        ...currencies.map((c) => ({ value: c.code, label: `${c.code} - ${c.name}` })),
      ]
    : [];

  return (
    <div>
      <h1 className="text-3xl font-bold text-gray-900 mb-6">
        Latest Exchange Rates
      </h1>

      <Card>
        <div className="space-y-6">
          {/* Base currency selector */}
          <div className="flex flex-col sm:flex-row sm:items-end sm:justify-between gap-4">
            <div className="w-full sm:w-64">
              {currenciesLoading ? (
                <div className="py-2">
                  <Spinner size="sm" />
                </div>
              ) : (
                <Select
                  label="Base Currency"
                  value={baseCurrency}
                  onChange={(e) => setBaseCurrency(e.target.value)}
                  options={currencyOptions}
                />
              )}
            </div>
            {ratesData && (
              <div className="text-sm text-gray-600">
                Last updated: {formatDateLong(ratesData.date)}
              </div>
            )}
          </div>

          {/* Loading state */}
          {ratesLoading && (
            <div className="flex justify-center py-12">
              <Spinner />
            </div>
          )}

          {/* Error state */}
          {isError && <ErrorMessage error={error} onRetry={() => refetch()} />}

          {/* Rates table */}
          {ratesData && currencies && !ratesLoading && (
            <RatesTable rates={ratesData.rates} currencies={currencies} />
          )}
        </div>
      </Card>
    </div>
  );
};
