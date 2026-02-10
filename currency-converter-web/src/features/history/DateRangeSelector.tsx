import React from 'react';
import { Input } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';
import { Select } from '@/components/ui/Select';
import { Search } from 'lucide-react';

interface DateRangeSelectorProps {
  startDate: string;
  endDate: string;
  baseCurrency: string;
  currencies: Array<{ value: string; label: string }>;
  onStartDateChange: (date: string) => void;
  onEndDateChange: (date: string) => void;
  onBaseCurrencyChange: (currency: string) => void;
  onSearch: () => void;
  isLoading?: boolean;
}

export const DateRangeSelector: React.FC<DateRangeSelectorProps> = ({
  startDate,
  endDate,
  baseCurrency,
  currencies,
  onStartDateChange,
  onEndDateChange,
  onBaseCurrencyChange,
  onSearch,
  isLoading = false,
}) => {
  return (
    <div className="space-y-4">
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        <Select
          label="Base Currency"
          value={baseCurrency}
          onChange={(e) => onBaseCurrencyChange(e.target.value)}
          options={currencies}
        />

        <Input
          label="Start Date"
          type="date"
          value={startDate}
          onChange={(e) => onStartDateChange(e.target.value)}
          max={endDate}
        />

        <Input
          label="End Date"
          type="date"
          value={endDate}
          onChange={(e) => onEndDateChange(e.target.value)}
          min={startDate}
          max={new Date().toISOString().split('T')[0]}
        />

        <div className="flex items-end">
          <Button
            type="button"
            onClick={onSearch}
            disabled={isLoading}
            className="w-full flex items-center justify-center gap-2"
          >
            <Search className="w-4 h-4" />
            {isLoading ? 'Searching...' : 'Search'}
          </Button>
        </div>
      </div>
    </div>
  );
};
