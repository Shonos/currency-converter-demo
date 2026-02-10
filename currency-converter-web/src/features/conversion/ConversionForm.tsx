import React, { useState } from 'react';
import { Card } from '@/components/ui/Card';
import { Input } from '@/components/ui/Input';
import { Select } from '@/components/ui/Select';
import { Button } from '@/components/ui/Button';
import { Spinner } from '@/components/ui/Spinner';
import { useCurrencies } from '@/hooks/useCurrencies';
import { CurrencySwapButton } from './CurrencySwapButton';
import { validateConversionForm } from '@/utils/validators';

interface ConversionFormProps {
  onConvert: (from: string, to: string, amount: number) => void;
  isLoading?: boolean;
}

export const ConversionForm: React.FC<ConversionFormProps> = ({
  onConvert,
  isLoading = false,
}) => {
  const [from, setFrom] = useState('EUR');
  const [to, setTo] = useState('USD');
  const [amount, setAmount] = useState('100');
  const [validationError, setValidationError] = useState<string | null>(null);

  const { data: currencies, isLoading: currenciesLoading } = useCurrencies();

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setValidationError(null);

    const amountNum = parseFloat(amount);
    const error = validateConversionForm(from, to, amountNum);

    if (error) {
      setValidationError(error);
      return;
    }

    onConvert(from, to, amountNum);
  };

  const handleSwap = () => {
    setFrom(to);
    setTo(from);
  };

  const currencyOptions = currencies
    ? [
        { value: '', label: 'Select currency' },
        ...currencies.map((c) => ({ value: c.code, label: `${c.code} - ${c.name}` })),
      ]
    : [{ value: '', label: 'Loading...' }];

  if (currenciesLoading) {
    return (
      <Card title="Convert Currency">
        <div className="flex justify-center py-8">
          <Spinner />
        </div>
      </Card>
    );
  }

  return (
    <Card title="Convert Currency">
      <form onSubmit={handleSubmit} className="space-y-4">
        <Input
          label="Amount"
          type="number"
          value={amount}
          onChange={(e) => setAmount(e.target.value)}
          placeholder="Enter amount"
          required
          step="0.01"
          min="0"
        />

        <div className="grid grid-cols-1 md:grid-cols-[1fr,auto,1fr] gap-4 items-end">
          <Select
            label="From"
            value={from}
            onChange={(e) => setFrom(e.target.value)}
            options={currencyOptions}
            required
          />

          <div className="flex justify-center md:mb-2">
            <CurrencySwapButton onSwap={handleSwap} disabled={isLoading} />
          </div>

          <Select
            label="To"
            value={to}
            onChange={(e) => setTo(e.target.value)}
            options={currencyOptions}
            required
          />
        </div>

        {validationError && (
          <p className="text-sm text-red-600">{validationError}</p>
        )}

        <Button type="submit" className="w-full" disabled={isLoading}>
          {isLoading ? 'Converting...' : 'Convert'}
        </Button>
      </form>
    </Card>
  );
};
