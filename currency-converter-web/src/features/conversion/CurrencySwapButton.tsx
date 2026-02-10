import React from 'react';
import { ArrowLeftRight } from 'lucide-react';

interface CurrencySwapButtonProps {
  onSwap: () => void;
  disabled?: boolean;
}

export const CurrencySwapButton: React.FC<CurrencySwapButtonProps> = ({
  onSwap,
  disabled = false,
}) => {
  return (
    <button
      type="button"
      onClick={onSwap}
      disabled={disabled}
      className="p-2 rounded-full hover:bg-gray-100 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
      aria-label="Swap currencies"
    >
      <ArrowLeftRight className="w-5 h-5 text-gray-600" />
    </button>
  );
};
