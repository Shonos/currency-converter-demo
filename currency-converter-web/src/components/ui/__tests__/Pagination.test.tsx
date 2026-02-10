import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@/test/utils/renderWithProviders';
import userEvent from '@testing-library/user-event';
import { Pagination } from '../Pagination';

describe('Pagination', () => {
  const defaultProps = {
    currentPage: 1,
    totalPages: 5,
    onPageChange: vi.fn(),
    hasNextPage: true,
    hasPreviousPage: false,
  };

  it('renders page info correctly', () => {
    render(<Pagination {...defaultProps} />);
    expect(screen.getByText(/Page 1 of 5/i)).toBeInTheDocument();
  });

  it('disables previous button on first page', () => {
    render(<Pagination {...defaultProps} currentPage={1} hasPreviousPage={false} />);
    const buttons = screen.getAllByRole('button');
    const prevButton = buttons[0]; // First button is previous
    expect(prevButton).toBeDisabled();
  });

  it('disables next button on last page', () => {
    render(<Pagination {...defaultProps} currentPage={5} totalPages={5} hasNextPage={false} />);
    const buttons = screen.getAllByRole('button');
    const nextButton = buttons[1]; // Second button is next
    expect(nextButton).toBeDisabled();
  });

  it('enables both buttons on middle page', () => {
    render(<Pagination {...defaultProps} currentPage={3} hasNextPage={true} hasPreviousPage={true} />);
    const buttons = screen.getAllByRole('button');
    expect(buttons[0]).not.toBeDisabled();
    expect(buttons[1]).not.toBeDisabled();
  });

  it('calls onPageChange when clicking next', async () => {
    const user = userEvent.setup();
    const onPageChange = vi.fn();
    render(<Pagination {...defaultProps} currentPage={2} onPageChange={onPageChange} hasNextPage={true} hasPreviousPage={true} />);

    const buttons = screen.getAllByRole('button');
    const nextButton = buttons[1];
    await user.click(nextButton);

    expect(onPageChange).toHaveBeenCalledWith(3);
  });

  it('calls onPageChange when clicking previous', async () => {
    const user = userEvent.setup();
    const onPageChange = vi.fn();
    render(<Pagination {...defaultProps} currentPage={3} onPageChange={onPageChange} hasNextPage={true} hasPreviousPage={true} />);

    const buttons = screen.getAllByRole('button');
    const prevButton = buttons[0];
    await user.click(prevButton);

    expect(onPageChange).toHaveBeenCalledWith(2);
  });
});
