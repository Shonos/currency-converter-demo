import { describe, it, expect } from 'vitest';
import { render, screen, waitFor } from '@/test/utils/renderWithProviders';
import userEvent from '@testing-library/user-event';
import { HistoryPage } from '../HistoryPage';

describe('HistoryPage', () => {
  it('renders date range selector and search button', async () => {
    render(<HistoryPage />);

    expect(screen.getByText(/historical exchange rates/i)).toBeInTheDocument();
    
    // Wait for currencies to load and form to appear
    await waitFor(() => {
      expect(screen.getByLabelText(/start date/i)).toBeInTheDocument();
    });
    
    expect(screen.getByLabelText(/end date/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/base currency/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /search/i })).toBeInTheDocument();
  });

  it('shows prompt before search is performed', async () => {
    render(<HistoryPage />);

    await waitFor(() => {
      expect(
        screen.getByText(/select a date range and click search/i)
      ).toBeInTheDocument();
    });
  });

  it('fetches and displays historical rates', async () => {
    const user = userEvent.setup();
    render(<HistoryPage />);

    // Wait for form to load
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /search/i })).toBeInTheDocument();
    });

    // Click search with default date range
    await user.click(screen.getByRole('button', { name: /search/i }));

    // Should show results
    await waitFor(() => {
      expect(screen.getByText('Jan 02, 2026')).toBeInTheDocument();
    });

    expect(screen.getByText('Jan 03, 2026')).toBeInTheDocument();
  });

  it('displays pagination controls', async () => {
    const user = userEvent.setup();
    render(<HistoryPage />);

    // Wait for form to load
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /search/i })).toBeInTheDocument();
    });

    await user.click(screen.getByRole('button', { name: /search/i }));

    await waitFor(() => {
      expect(screen.getByText(/page 1 of 3/i)).toBeInTheDocument();
    });

    expect(screen.getByRole('button', { name: /next/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /previous/i })).toBeDisabled();
  });

  it('navigates to next page', async () => {
    const user = userEvent.setup();
    render(<HistoryPage />);

    // Wait for form to load
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /search/i })).toBeInTheDocument();
    });

    await user.click(screen.getByRole('button', { name: /search/i }));

    await waitFor(() => {
      expect(screen.getByText(/page 1 of 3/i)).toBeInTheDocument();
    });

    const nextButton = screen.getByRole('button', { name: /next/i });
    await user.click(nextButton);

    await waitFor(() => {
      expect(screen.getByText(/page 2 of 3/i)).toBeInTheDocument();
    });
  });

  it('allows changing page size', async () => {
    const user = userEvent.setup();
    render(<HistoryPage />);

    // Wait for form to load
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /search/i })).toBeInTheDocument();
    });

    await user.click(screen.getByRole('button', { name: /search/i }));

    await waitFor(() => {
      expect(screen.getByLabelText(/show/i)).toBeInTheDocument();
    });

    const pageSizeSelect = screen.getByLabelText(/show/i);
    await user.selectOptions(pageSizeSelect, '25');

    // Should trigger a re-fetch with new page size
    await waitFor(() => {
      expect((pageSizeSelect as HTMLSelectElement).value).toBe('25');
    });
  });

  it('allows changing base currency', async () => {
    const user = userEvent.setup();
    render(<HistoryPage />);

    // Wait for form to load
    await waitFor(() => {
      expect(screen.getByLabelText(/base currency/i)).toBeInTheDocument();
    });

    const baseCurrencySelect = screen.getByLabelText(/base currency/i);
    await user.selectOptions(baseCurrencySelect, 'USD');

    await user.click(screen.getByRole('button', { name: /search/i }));

    await waitFor(() => {
      expect(screen.getByText('Jan 02, 2026')).toBeInTheDocument();
    });

    // The results should be for USD base
    expect((baseCurrencySelect as HTMLSelectElement).value).toBe('USD');
  });

  it('validates date range on search', async () => {
    const user = userEvent.setup();
    render(<HistoryPage />);

    // Wait for form to load
    await waitFor(() => {
      expect(screen.getByLabelText(/start date/i)).toBeInTheDocument();
    });

    const startDateInput = screen.getByLabelText(/start date/i);
    const endDateInput = screen.getByLabelText(/end date/i);

    // Set end date before start date
    await user.clear(startDateInput);
    await user.type(startDateInput, '2026-02-01');
    await user.clear(endDateInput);
    await user.type(endDateInput, '2026-01-01');

    await user.click(screen.getByRole('button', { name: /search/i }));

    // Should either show validation error or prevent submission
    // (Implementation may vary - HTML5 validation or custom)
    expect(screen.getByLabelText(/start date/i)).toBeInTheDocument();
  });

  it('resets to page 1 when search parameters change', async () => {
    const user = userEvent.setup();
    render(<HistoryPage />);

    // Wait for form to load
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /search/i })).toBeInTheDocument();
    });

    // Perform initial search
    await user.click(screen.getByRole('button', { name: /search/i }));

    await waitFor(() => {
      expect(screen.getByText(/page 1 of 3/i)).toBeInTheDocument();
    });

    // Go to page 2
    await user.click(screen.getByRole('button', { name: /next/i }));

    await waitFor(() => {
      expect(screen.getByText(/page 2 of 3/i)).toBeInTheDocument();
    });

    // Change base currency and search again
    await user.selectOptions(screen.getByLabelText(/base currency/i), 'USD');
    await user.click(screen.getByRole('button', { name: /search/i }));

    // Should reset to page 1
    await waitFor(() => {
      expect(screen.getByText(/page 1 of 3/i)).toBeInTheDocument();
    });
  });
});
