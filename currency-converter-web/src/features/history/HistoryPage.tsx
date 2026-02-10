import React, { useState } from 'react';
import { subDays } from 'date-fns';
import { Card } from '@/components/ui/Card';
import { Spinner } from '@/components/ui/Spinner';
import { ErrorMessage } from '@/components/ui/ErrorMessage';
import { useCurrencies } from '@/hooks/useCurrencies';
import { useHistoricalRates } from '@/hooks/useHistoricalRates';
import { DateRangeSelector } from './DateRangeSelector';
import { HistoryTable } from './HistoryTable';
import { formatDateForApi } from '@/utils/formatters';

export const HistoryPage: React.FC = () => {
  // Default: last 30 days
  const today = new Date();
  const thirtyDaysAgo = subDays(today, 30);

  const [baseCurrency, setBaseCurrency] = useState('EUR');
  const [startDate, setStartDate] = useState(formatDateForApi(thirtyDaysAgo));
  const [endDate, setEndDate] = useState(formatDateForApi(today));
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [hasSearched, setHasSearched] = useState(false);

  const { data: currencies, isLoading: currenciesLoading } = useCurrencies();

  const {
    data: historyData,
    isLoading: historyLoading,
    isError,
    error,
    refetch,
  } = useHistoricalRates(
    baseCurrency,
    startDate,
    endDate,
    page,
    pageSize
  );

  const handleSearch = () => {
    setPage(1); // Reset to first page on new search
    setHasSearched(true);
    refetch();
  };

  const handlePageChange = (newPage: number) => {
    setPage(newPage);
  };

  const handlePageSizeChange = (newPageSize: number) => {
    setPageSize(newPageSize);
    setPage(1); // Reset to first page when changing page size
  };

  const currencyOptions = currencies
    ? currencies.map((c) => ({ value: c.code, label: `${c.code} - ${c.name}` }))
    : [];

  return (
    <div>
      <h1 className="text-3xl font-bold text-gray-900 mb-6">
        Historical Exchange Rates
      </h1>

      <Card>
        <div className="space-y-6">
          {/* Date range selector */}
          {currenciesLoading ? (
            <div className="flex justify-center py-4">
              <Spinner />
            </div>
          ) : (
            <DateRangeSelector
              startDate={startDate}
              endDate={endDate}
              baseCurrency={baseCurrency}
              currencies={currencyOptions}
              onStartDateChange={setStartDate}
              onEndDateChange={setEndDate}
              onBaseCurrencyChange={setBaseCurrency}
              onSearch={handleSearch}
              isLoading={historyLoading}
            />
          )}

          {/* Loading state */}
          {historyLoading && hasSearched && (
            <div className="flex justify-center py-12">
              <Spinner />
            </div>
          )}

          {/* Error state */}
          {isError && hasSearched && (
            <ErrorMessage error={error} onRetry={() => refetch()} />
          )}

          {/* History table */}
          {historyData && hasSearched && !historyLoading && (
            <HistoryTable
              data={historyData}
              onPageChange={handlePageChange}
              onPageSizeChange={handlePageSizeChange}
            />
          )}

          {/* Empty state before search */}
          {!hasSearched && !historyLoading && (
            <div className="text-center py-12 text-gray-500">
              Select a date range and click Search to view historical rates
            </div>
          )}
        </div>
      </Card>
    </div>
  );
};
