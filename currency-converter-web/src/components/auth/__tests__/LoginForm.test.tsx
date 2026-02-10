import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@/test/utils/renderWithProviders';
import userEvent from '@testing-library/user-event';
import { LoginForm } from '../LoginForm';

describe('LoginForm', () => {
  const mockOnSuccess = vi.fn();

  beforeEach(() => {
    mockOnSuccess.mockClear();
    localStorage.clear();
  });

  it('renders login form with all fields', () => {
    render(<LoginForm onSuccess={mockOnSuccess} />);

    expect(screen.getByLabelText(/username/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /log in/i })).toBeInTheDocument();
  });

  it('displays demo credentials', () => {
    render(<LoginForm onSuccess={mockOnSuccess} />);

    expect(screen.getByText(/demo credentials/i)).toBeInTheDocument();
    expect(screen.getByText(/admin/i)).toBeInTheDocument();
    expect(screen.getByText(/Admin123!/i)).toBeInTheDocument();
  });

  it('logs in successfully with valid credentials', async () => {
    const user = userEvent.setup();
    render(<LoginForm onSuccess={mockOnSuccess} />);

    await user.type(screen.getByLabelText(/username/i), 'admin');
    await user.type(screen.getByLabelText(/password/i), 'Admin123!');
    await user.click(screen.getByRole('button', { name: /log in/i }));

    await waitFor(() => {
      expect(mockOnSuccess).toHaveBeenCalled();
    });

    // Token should be stored
    expect(localStorage.getItem('token')).toBeTruthy();
  });

  it('shows error message for invalid credentials', async () => {
    const user = userEvent.setup();
    render(<LoginForm onSuccess={mockOnSuccess} />);

    await user.type(screen.getByLabelText(/username/i), 'wrong');
    await user.type(screen.getByLabelText(/password/i), 'wrong');
    await user.click(screen.getByRole('button', { name: /log in/i }));

    await waitFor(() => {
      expect(screen.getByText(/invalid username or password/i)).toBeInTheDocument();
    });

    expect(mockOnSuccess).not.toHaveBeenCalled();
  });

  it('validates required fields', async () => {
    const user = userEvent.setup();
    render(<LoginForm onSuccess={mockOnSuccess} />);

    await user.click(screen.getByRole('button', { name: /log in/i }));

    // HTML5 validation should prevent submission
    const usernameInput = screen.getByLabelText(/username/i);
    expect(usernameInput).toBeRequired();
    expect(usernameInput).toBeInvalid();
  });

  it('shows loading state during submission', async () => {
    const user = userEvent.setup();
    render(<LoginForm onSuccess={mockOnSuccess} />);

    await user.type(screen.getByLabelText(/username/i), 'admin');
    await user.type(screen.getByLabelText(/password/i), 'Admin123!');

    const submitButton = screen.getByRole('button', { name: /log in/i });
    await user.click(submitButton);

    // Button should show loading state briefly
    await waitFor(() => {
      expect(submitButton).toHaveTextContent(/logging in|loading/i);
    });
  });

  it('clears error message when typing', async () => {
    const user = userEvent.setup();
    render(<LoginForm onSuccess={mockOnSuccess} />);

    // Submit with wrong credentials
    await user.type(screen.getByLabelText(/username/i), 'wrong');
    await user.type(screen.getByLabelText(/password/i), 'wrong');
    await user.click(screen.getByRole('button', { name: /log in/i }));

    await waitFor(() => {
      expect(screen.getByText(/invalid username or password/i)).toBeInTheDocument();
    });

    // Start typing in username
    await user.clear(screen.getByLabelText(/username/i));
    await user.type(screen.getByLabelText(/username/i), 'a');

    // Error should be cleared
    await waitFor(() => {
      expect(screen.queryByText(/invalid username or password/i)).not.toBeInTheDocument();
    });
  });
});
