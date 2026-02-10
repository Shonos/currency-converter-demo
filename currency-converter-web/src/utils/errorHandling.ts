import type { ApiError } from '@/types/api';
import { AxiosError } from 'axios';

/**
 * Extract a user-friendly error message from an API error
 */
export function getErrorMessage(error: unknown): string {
  if (error instanceof AxiosError) {
    const apiError = error.response?.data as ApiError | undefined;
    
    // Use API error detail if available
    if (apiError?.detail) {
      return apiError.detail;
    }
    
    // Handle common HTTP status codes
    if (error.response?.status === 401) {
      return 'Please log in to continue.';
    }
    if (error.response?.status === 403) {
      return 'You do not have permission for this action.';
    }
    if (error.response?.status === 429) {
      return 'Too many requests. Please wait and try again.';
    }
    if (error.response?.status === 500) {
      return 'Server error. Please try again later.';
    }
    
    // Use axios error message
    if (error.message) {
      return error.message;
    }
  }
  
  if (error instanceof Error) {
    return error.message;
  }
  
  return 'An unexpected error occurred. Please try again.';
}

/**
 * Check if an error is a network/connectivity error
 */
export function isNetworkError(error: unknown): boolean {
  if (error instanceof AxiosError) {
    return !error.response && error.code === 'ERR_NETWORK';
  }
  return false;
}

/**
 * Check if an error is an authentication error
 */
export function isAuthError(error: unknown): boolean {
  if (error instanceof AxiosError) {
    return error.response?.status === 401;
  }
  return false;
}
