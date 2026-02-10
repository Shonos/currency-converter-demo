import { describe, it, expect } from 'vitest';
import { AxiosError } from 'axios';
import { getErrorMessage } from '../errorHandling';

describe('getErrorMessage', () => {
  it('returns default message for non-Error types', () => {
    expect(getErrorMessage('Simple error')).toBe('An unexpected error occurred. Please try again.');
  });

  it('extracts message from Error object', () => {
    const error = new Error('Error object message');
    expect(getErrorMessage(error)).toBe('Error object message');
  });

  it('extracts detail from ProblemDetails in AxiosError', () => {
    const axiosError = {
      isAxiosError: true,
      response: {
        status: 400,
        data: {
          title: 'Bad Request',
          detail: 'Currency is not supported',
          status: 400,
        },
      },
      message: 'Request failed',
    } as AxiosError;

    expect(getErrorMessage(axiosError)).toBe('Currency is not supported');
  });

  it('returns appropriate message for 401 status', () => {
    const axiosError = {
      isAxiosError: true,
      response: {
        status: 401,
        data: {},
      },
      message: 'Unauthorized',
    } as AxiosError;

    expect(getErrorMessage(axiosError)).toBe('Please log in to continue.');
  });

  it('returns appropriate message for 500 status', () => {
    const axiosError = {
      isAxiosError: true,
      response: {
        status: 500,
        data: {},
      },
      message: 'Server error',
    } as AxiosError;

    expect(getErrorMessage(axiosError)).toBe('Server error. Please try again later.');
  });

  it('uses axios error message as fallback', () => {
    const axiosError = {
      isAxiosError: true,
      message: 'Network Error',
      response: undefined,
    } as AxiosError;

    expect(getErrorMessage(axiosError)).toBe('Network Error');
  });

  it('returns default message for unknown error types', () => {
    expect(getErrorMessage(null)).toBe('An unexpected error occurred. Please try again.');
    expect(getErrorMessage(undefined)).toBe('An unexpected error occurred. Please try again.');
    expect(getErrorMessage(123)).toBe('An unexpected error occurred. Please try again.');
    expect(getErrorMessage({})).toBe('An unexpected error occurred. Please try again.');
  });

  it('returns default message for objects without Error type', () => {
    const nested = {
      error: 'Nested error message',
    };
    expect(getErrorMessage(nested)).toBe('An unexpected error occurred. Please try again.');
  });
});
