import { render, type RenderOptions } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter, MemoryRouter } from 'react-router-dom';
import { AuthProvider } from '@/context/AuthContext';
import type { ReactElement } from 'react';

/**
 * Create a new QueryClient for testing with retry disabled
 */
function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        gcTime: 0,
      },
      mutations: {
        retry: false,
      },
    },
    logger: {
      log: console.log,
      warn: console.warn,
      error: () => {}, // Suppress error logs in tests
    },
  });
}

interface CustomRenderOptions extends Omit<RenderOptions, 'wrapper'> {
  initialRoute?: string;
  useBrowserRouter?: boolean;
}

/**
 * Custom render function that wraps components with all required providers
 */
export function renderWithProviders(
  ui: ReactElement,
  options: CustomRenderOptions = {}
) {
  const {
    initialRoute = '/',
    useBrowserRouter = false,
    ...renderOptions
  } = options;

  const queryClient = createTestQueryClient();

  const Router = useBrowserRouter ? BrowserRouter : MemoryRouter;
  const routerProps = useBrowserRouter ? {} : { initialEntries: [initialRoute] };

  function Wrapper({ children }: { children: React.ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>
        <Router {...routerProps}>
          <AuthProvider>{children}</AuthProvider>
        </Router>
      </QueryClientProvider>
    );
  }

  return {
    ...render(ui, { wrapper: Wrapper, ...renderOptions }),
    queryClient,
  };
}

/**
 * Re-export everything from React Testing Library
 */
export * from '@testing-library/react';
export { renderWithProviders as render };
