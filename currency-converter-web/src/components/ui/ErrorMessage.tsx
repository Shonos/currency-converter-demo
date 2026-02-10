import React from 'react';
import { AlertCircle, RefreshCw } from 'lucide-react';
import { getErrorMessage } from '@/utils/errorHandling';
import { Button } from './Button';

interface ErrorMessageProps {
  error: unknown;
  onRetry?: () => void;
  className?: string;
}

export const ErrorMessage: React.FC<ErrorMessageProps> = ({ error, onRetry, className }) => {
  const message = getErrorMessage(error);
  
  return (
    <div className={`flex items-start gap-3 p-4 bg-red-50 border border-red-200 rounded-md text-red-800 ${className || ''}`}>
      <AlertCircle className="w-5 h-5 flex-shrink-0 mt-0.5" />
      <div className="flex-1">
        <p className="text-sm font-medium">Error</p>
        <p className="text-sm mt-1">{message}</p>
        {onRetry && (
          <Button
            variant="danger"
            size="sm"
            onClick={onRetry}
            className="mt-3 flex items-center gap-2"
          >
            <RefreshCw className="w-4 h-4" />
            Try Again
          </Button>
        )}
      </div>
    </div>
  );
};
