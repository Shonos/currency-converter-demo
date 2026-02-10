import React from 'react';
import { Pagination } from '@/components/ui/Pagination';
import { formatRate, formatDate } from '@/utils/formatters';
import type { PagedHistoricalRatesResponse } from '@/types/currency';

interface HistoryTableProps {
  data: PagedHistoricalRatesResponse;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
}

export const HistoryTable: React.FC<HistoryTableProps> = ({
  data,
  onPageChange,
  onPageSizeChange,
}) => {
  // Get all unique currency codes from the rates
  const currencyCodes = React.useMemo(() => {
    const codes = new Set<string>();
    data.rates.forEach((rate) => {
      Object.keys(rate.rates).forEach((code) => codes.add(code));
    });
    return Array.from(codes).sort();
  }, [data.rates]);

  if (data.rates.length === 0) {
    return (
      <div className="text-center py-12 text-gray-500">
        No data available for the selected range
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Table */}
      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-700 uppercase tracking-wider sticky left-0 bg-gray-50">
                Date
              </th>
              {currencyCodes.map((code) => (
                <th
                  key={code}
                  className="px-6 py-3 text-right text-xs font-medium text-gray-700 uppercase tracking-wider"
                >
                  {code}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {data.rates.map((rate) => (
              <tr key={rate.date} className="hover:bg-gray-50">
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900 sticky left-0 bg-white">
                  {formatDate(rate.date)}
                </td>
                {currencyCodes.map((code) => (
                  <td
                    key={code}
                    className="px-6 py-4 whitespace-nowrap text-sm text-gray-900 text-right font-mono"
                  >
                    {rate.rates[code] ? formatRate(rate.rates[code]) : '-'}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Pagination controls */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 pt-4 border-t">
        <div className="flex items-center gap-2">
          <label htmlFor="pageSize" className="text-sm text-gray-700">
            Show:
          </label>
          <select
            id="pageSize"
            value={data.pageSize}
            onChange={(e) => onPageSizeChange(Number(e.target.value))}
            className="px-3 py-1 border border-gray-300 rounded-md text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value={10}>10</option>
            <option value={25}>25</option>
            <option value={50}>50</option>
          </select>
          <span className="text-sm text-gray-700">per page</span>
        </div>

        <Pagination
          currentPage={data.page}
          totalPages={data.totalPages}
          onPageChange={onPageChange}
          hasNextPage={data.hasNextPage}
          hasPreviousPage={data.hasPreviousPage}
        />
      </div>

      <div className="text-sm text-gray-600 text-center">
        Showing {data.rates.length} of {data.totalCount} results
      </div>
    </div>
  );
};
