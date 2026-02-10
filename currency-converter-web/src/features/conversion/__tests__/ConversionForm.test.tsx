import { describe, it, expect, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@/test/utils/renderWithProviders';
import userEvent from '@testing-library/user-event';
import { ConversionForm } from '../ConversionForm';

describe('ConversionForm', () => {
  const mockOnConvert = vi.fn();

  beforeEach(() => {
    mockOnConvert.mockClear();
  });

  it('renders amount input and currency selectors', async () => {
    render(<ConversionForm onConvert={mockOnConvert} />);

    await waitFor(() => {
      expect(screen.getByLabelText(/amount/i)).toBeInTheDocument();
    });

    expect(screen.getByLabelText(/from/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/to/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /convert/i })).toBeInTheDocument();
  });

  it('populates currency dropdowns from API', async () => {
    render(<ConversionForm onConvert={mockOnConvert} />);

    await waitFor(() => {
      expect(screen.getByText('EUR - Euro')).toBeInTheDocument();
    });

    expect(screen.getByText('USD - United States Dollar')).toBeInTheDocument();
    expect(screen.getByText('GBP - British Pound')).toBeInTheDocument();
    expect(screen.getByText('JPY - Japanese Yen')).toBeInTheDocument();
  });

  it('performs conversion and calls onConvert', async () => {
    const user = userEvent.setup();
    render(<ConversionForm onConvert={mockOnConvert} />);

    await waitFor(() => {
      expect(screen.getByLabelText(/amount/i)).toBeInTheDocument();
    });

    const amountInput = screen.getByLabelText(/amount/i);
    await user.clear(amountInput);
    await user.type(amountInput, '100');

    const fromSelect = screen.getByLabelText(/from/i);
    const toSelect = screen.getByLabelText(/to/i);
    
    await user.selectOptions(fromSelect, 'EUR');
    await user.selectOptions(toSelect, 'USD');

    await user.click(screen.getByRole('button', { name: /convert/i }));

    await waitFor(() => {
      expect(mockOnConvert).toHaveBeenCalledWith('EUR', 'USD', 100);
    });
  });

  it('validates amount is positive', async () => {
    const user = userEvent.setup();
    render(<ConversionForm onConvert={mockOnConvert} />);

    await waitFor(() => {
      expect(screen.getByLabelText(/amount/i)).toBeInTheDocument();
    });

    const amountInput = screen.getByLabelText(/amount/i);
    await user.clear(amountInput);
    await user.type(amountInput, '0');

    await user.click(screen.getByRole('button', { name: /convert/i }));

    await waitFor(() => {
      expect(screen.getByText(/amount must be greater than zero/i)).toBeInTheDocument();
    });

    expect(mockOnConvert).not.toHaveBeenCalled();
  });

  it('swaps currencies when swap button clicked', async () => {
    const user = userEvent.setup();
    render(<ConversionForm onConvert={mockOnConvert} />);

    await waitFor(() => {
      expect(screen.getByLabelText(/from/i)).toBeInTheDocument();
    });

    const fromSelect = screen.getByLabelText(/from/i) as HTMLSelectElement;
    const toSelect = screen.getByLabelText(/to/i) as HTMLSelectElement;

    // Set initial values
    await user.selectOptions(fromSelect, 'EUR');
    await user.selectOptions(toSelect, 'USD');

    // Find and click swap button
    const swapButton = screen.getByRole('button', { name: /swap/i });
    await user.click(swapButton);

    // Values should be swapped
    await waitFor(() => {
      expect(fromSelect.value).toBe('USD');
      expect(toSelect.value).toBe('EUR');
    });
  });

  it('validates currencies are different', async () => {
    const user = userEvent.setup();
    render(<ConversionForm onConvert={mockOnConvert} />);

    await waitFor(() => {
      expect(screen.getByLabelText(/from/i)).toBeInTheDocument();
    });

    const fromSelect = screen.getByLabelText(/from/i);
    const toSelect = screen.getByLabelText(/to/i);

    await user.selectOptions(fromSelect, 'EUR');
    await user.selectOptions(toSelect, 'EUR');

    await user.click(screen.getByRole('button', { name: /convert/i }));

    await waitFor(() => {
      expect(
        screen.getByText(/source and target currencies must be different/i)
      ).toBeInTheDocument();
    });

    expect(mockOnConvert).not.toHaveBeenCalled();
  });

  it('shows loading state during conversion', async () => {
    render(<ConversionForm onConvert={mockOnConvert} isLoading={true} />);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /converting/i })).toBeInTheDocument();
    });

    const button = screen.getByRole('button', { name: /converting/i });
    expect(button).toBeDisabled();
  });
});
