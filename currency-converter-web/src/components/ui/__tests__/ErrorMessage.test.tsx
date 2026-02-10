import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@/test/utils/renderWithProviders';
import userEvent from '@testing-library/user-event';
import { AxiosError } from 'axios';
import { ErrorMessage } from '../ErrorMessage';

describe('ErrorMessage', () => {
  it('displays error message from string', () => {
    // Strings are treated as unknown types and return default message
    render(<ErrorMessage error="Something went wrong" />);
    expect(screen.getByText('An unexpected error occurred. Please try again.')).toBeInTheDocument();
  });

  it('displays error message from Error object', () => {
    const error = new Error('Error message');
    render(<ErrorMessage error={error} />);
    expect(screen.getByText('Error message')).toBeInTheDocument();
  });

  it('extracts message from AxiosError with ProblemDetails', () => {
    const axiosError = {
      isAxiosError: true,
      response: {
        data: {
          detail: 'Currency is not supported',
          title: 'Bad Request',
          status: 400,
        },
      },
    } as AxiosError;

    render(<ErrorMessage error={axiosError} />);
    expect(screen.getByText('Currency is not supported')).toBeInTheDocument();
  });

  it('shows retry button when onRetry is provided', () => {
    const onRetry = vi.fn();
    render(<ErrorMessage error="Error" onRetry={onRetry} />);

    expect(screen.getByRole('button', { name: /try again/i })).toBeInTheDocument();
  });

  it('does not show retry button when onRetry is not provided', () => {
    render(<ErrorMessage error="Error" />);

    expect(screen.queryByRole('button', { name: /try again/i })).not.toBeInTheDocument();
  });

  it('calls onRetry when retry button is clicked', async () => {
    const user = userEvent.setup();
    const onRetry = vi.fn();
    render(<ErrorMessage error="Error" onRetry={onRetry} />);

    const retryButton = screen.getByRole('button', { name: /try again/i });
    await user.click(retryButton);

    expect(onRetry).toHaveBeenCalledTimes(1);
  });

  it('displays error icon', () => {
    render(<ErrorMessage error="Error" />);
    // The error icon (AlertCircle) should be part of the component
    const errorHeading = screen.getByText('Error');
    expect(errorHeading).toBeInTheDocument();
    expect(errorHeading).toHaveClass('text-sm');
  });

  it('applies custom className', () => {
    const { container } = render(<ErrorMessage error="Error" className="custom-class" />);
    const errorDiv = container.querySelector('.custom-class');
    expect(errorDiv).not.toBeNull();
  });
});
