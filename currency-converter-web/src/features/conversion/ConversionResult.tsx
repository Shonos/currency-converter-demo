import React from 'react';
import { Card } from '@/components/ui/Card';
import { formatAmount, formatRate, formatDateLong } from '@/utils/formatters';
import type { ConversionResponse } from '@/types/currency';
import { ArrowRight, Calendar, TrendingUp } from 'lucide-react';

interface ConversionResultProps {
  result: ConversionResponse;
}

export const ConversionResult: React.FC<ConversionResultProps> = ({ result }) => {
  return (
    <Card>
      <div className="space-y-4">
        {/* Main conversion display */}
        <div className="flex items-center justify-center gap-4 text-2xl font-bold text-gray-900">
          <span>{formatAmount(result.amount, result.from)}</span>
          <ArrowRight className="w-6 h-6 text-gray-400" />
          <span className="text-blue-600">
            {formatAmount(result.convertedAmount, result.to)}
          </span>
        </div>

        {/* Divider */}
        <hr className="border-gray-200" />

        {/* Exchange rate details */}
        <div className="space-y-3">
          <div className="flex items-start gap-3">
            <TrendingUp className="w-5 h-5 text-gray-400 mt-0.5" />
            <div>
              <p className="text-sm font-medium text-gray-700">Exchange Rate</p>
              <p className="text-base text-gray-900">
                1 {result.from} = {formatRate(result.rate)} {result.to}
              </p>
            </div>
          </div>

          <div className="flex items-start gap-3">
            <Calendar className="w-5 h-5 text-gray-400 mt-0.5" />
            <div>
              <p className="text-sm font-medium text-gray-700">Rate Date</p>
              <p className="text-base text-gray-900">{formatDateLong(result.date)}</p>
            </div>
          </div>
        </div>

        {/* Inverse rate (optional) */}
        <div className="mt-4 p-3 bg-gray-50 rounded-md">
          <p className="text-sm text-gray-600">
            1 {result.to} = {formatRate(1 / result.rate)} {result.from}
          </p>
        </div>
      </div>
    </Card>
  );
};
