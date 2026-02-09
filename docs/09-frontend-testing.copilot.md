# Sub-Task 09: Frontend Testing

> **Context**: Use with `master.copilot.md`. **Depends on**: Sub-tasks 07, 08.

---

## Objective

Write component and integration tests for key frontend flows using Vitest, React Testing Library, and MSW (Mock Service Worker) for API mocking. Focus on correctness of critical paths, not exhaustive UI testing.

---

## 1. Testing Stack

| Tool                          | Purpose                              |
|-------------------------------|--------------------------------------|
| Vitest                        | Test runner (Jest-compatible)        |
| React Testing Library         | Component rendering & interaction    |
| @testing-library/user-event   | Simulating user interactions         |
| MSW (Mock Service Worker)     | Mocking API responses                |
| @testing-library/jest-dom     | Custom DOM matchers                  |

---

## 2. Test Structure

```
src/
├── test/
│   ├── setup.ts                      # Test setup (jest-dom, MSW)
│   ├── mocks/
│   │   ├── handlers.ts               # MSW request handlers
│   │   ├── server.ts                 # MSW server instance
│   │   └── data.ts                   # Mock data constants
│   └── utils/
│       └── renderWithProviders.tsx    # Custom render with all providers
├── features/
│   ├── conversion/
│   │   └── __tests__/
│   │       ├── ConversionForm.test.tsx
│   │       └── ConversionPage.test.tsx
│   ├── rates/
│   │   └── __tests__/
│   │       └── LatestRatesPage.test.tsx
│   └── history/
│       └── __tests__/
│           └── HistoryPage.test.tsx
├── components/
│   ├── ui/__tests__/
│   │   ├── Pagination.test.tsx
│   │   └── ErrorMessage.test.tsx
│   └── auth/__tests__/
│       ├── LoginForm.test.tsx
│       └── ProtectedRoute.test.tsx
├── hooks/__tests__/
│   ├── useAuth.test.ts
│   └── useCurrencies.test.ts
└── utils/__tests__/
    ├── validators.test.ts
    └── formatters.test.ts
```

---

## 3. MSW Setup

### 3.1 Mock Server (`src/test/mocks/server.ts`)

```typescript
import { setupServer } from 'msw/node';
import { handlers } from './handlers';

export const server = setupServer(...handlers);
```

### 3.2 Request Handlers (`src/test/mocks/handlers.ts`)

```typescript
import { http, HttpResponse } from 'msw';

const API_BASE = 'http://localhost:5000/api';

export const handlers = [
  // Auth
  http.post(`${API_BASE}/v1/auth/login`, async ({ request }) => {
    const body = await request.json() as { username: string; password: string };
    if (body.username === 'admin' && body.password === 'Admin123!') {
      return HttpResponse.json({
        token: 'mock-jwt-token',
        expiresAt: '2024-12-31T23:59:59Z',
        role: 'Admin',
      });
    }
    return new HttpResponse(null, { status: 401 });
  }),

  // Currencies
  http.get(`${API_BASE}/v1/currencies`, () => {
    return HttpResponse.json({
      currencies: [
        { code: 'EUR', name: 'Euro' },
        { code: 'USD', name: 'United States Dollar' },
        { code: 'GBP', name: 'British Pound' },
        { code: 'JPY', name: 'Japanese Yen' },
      ],
    });
  }),

  // Latest rates
  http.get(`${API_BASE}/v1/exchange-rates/latest`, ({ request }) => {
    const url = new URL(request.url);
    const base = url.searchParams.get('baseCurrency') || 'EUR';
    return HttpResponse.json({
      baseCurrency: base,
      date: '2024-02-06',
      rates: { AUD: 1.688, GBP: 0.8679, JPY: 185.27, USD: 1.1794 },
    });
  }),

  // Conversion
  http.get(`${API_BASE}/v1/exchange-rates/convert`, ({ request }) => {
    const url = new URL(request.url);
    const from = url.searchParams.get('from');
    const to = url.searchParams.get('to');
    const amount = parseFloat(url.searchParams.get('amount') || '0');

    // Simulate excluded currency error
    const excluded = ['TRY', 'PLN', 'THB', 'MXN'];
    if (excluded.includes(from!) || excluded.includes(to!)) {
      return HttpResponse.json(
        {
          type: 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
          title: 'Bad Request',
          status: 400,
          detail: `Currency '${excluded.find(c => c === from || c === to)}' is not supported.`,
        },
        { status: 400 }
      );
    }

    return HttpResponse.json({
      from,
      to,
      amount,
      convertedAmount: amount * 1.1794,
      rate: 1.1794,
      date: '2024-02-06',
    });
  }),

  // Historical rates
  http.get(`${API_BASE}/v1/exchange-rates/history`, () => {
    return HttpResponse.json({
      baseCurrency: 'EUR',
      startDate: '2024-01-01',
      endDate: '2024-01-31',
      page: 1,
      pageSize: 10,
      totalCount: 23,
      totalPages: 3,
      hasNextPage: true,
      hasPreviousPage: false,
      rates: [
        { date: '2024-01-02', rates: { USD: 1.0956, GBP: 0.8601 } },
        { date: '2024-01-03', rates: { USD: 1.0919, GBP: 0.8592 } },
      ],
    });
  }),
];
```

### 3.3 Test Setup (`src/test/setup.ts`)

```typescript
import '@testing-library/jest-dom';
import { server } from './mocks/server';

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
afterEach(() => server.resetHandlers());
afterAll(() => server.close());
```

### 3.4 Custom Render with Providers

```typescript
// src/test/utils/renderWithProviders.tsx
import { render, type RenderOptions } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter } from 'react-router-dom';
import { AuthProvider } from '@/context/AuthContext';

function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });
}

export function renderWithProviders(
  ui: React.ReactElement,
  options?: RenderOptions
) {
  const queryClient = createTestQueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AuthProvider>
          {ui}
        </AuthProvider>
      </BrowserRouter>
    </QueryClientProvider>,
    options
  );
}
```

---

## 4. Key Test Cases

### 4.1 Login Flow

```typescript
// components/auth/__tests__/LoginForm.test.tsx
describe('LoginForm', () => {
  it('logs in successfully with valid credentials', async () => {
    // Render login form
    // Fill in username and password
    // Click submit
    // Assert: token stored, user redirected
  });

  it('shows error message for invalid credentials', async () => {
    // Fill in wrong credentials
    // Click submit
    // Assert: error message displayed
  });

  it('validates required fields', async () => {
    // Click submit without filling fields
    // Assert: validation messages shown
  });

  it('shows loading state during submission', async () => {
    // Click submit
    // Assert: button is disabled / spinner shown
  });
});
```

### 4.2 Currency Conversion

```typescript
// features/conversion/__tests__/ConversionForm.test.tsx
describe('ConversionForm', () => {
  it('renders amount input and currency selectors', async () => {
    // Assert: amount input, from selector, to selector, convert button exist
  });

  it('populates currency dropdowns from API', async () => {
    // Assert: dropdowns contain EUR, USD, GBP, JPY
    // Assert: excluded currencies NOT present
  });

  it('performs conversion and displays result', async () => {
    // Select EUR -> USD, amount 100
    // Click convert
    // Assert: result shows "100.00 EUR = 117.94 USD"
  });

  it('displays error for excluded currency', async () => {
    // If somehow an excluded currency is used
    // Assert: error message is shown
  });

  it('validates amount is positive', async () => {
    // Enter 0 or negative amount
    // Assert: validation error
  });

  it('swaps currencies when swap button clicked', async () => {
    // Select EUR -> USD
    // Click swap
    // Assert: now USD -> EUR
  });
});
```

### 4.3 Latest Rates

```typescript
// features/rates/__tests__/LatestRatesPage.test.tsx
describe('LatestRatesPage', () => {
  it('displays rates table for default base currency', async () => {
    // Assert: table with currency codes and rates is rendered
  });

  it('updates rates when base currency changes', async () => {
    // Change base currency to USD
    // Assert: new API call made, table updated
  });

  it('shows loading state while fetching', async () => {
    // Assert: spinner or skeleton shown during load
  });

  it('shows error message on API failure', async () => {
    // Override handler to return 500
    // Assert: error message displayed
  });
});
```

### 4.4 Historical Rates

```typescript
// features/history/__tests__/HistoryPage.test.tsx
describe('HistoryPage', () => {
  it('renders date range selector and search button', () => {
    // Assert: start date, end date, base currency, search button exist
  });

  it('fetches and displays historical rates', async () => {
    // Select dates, click search
    // Assert: table with dated rates displayed
  });

  it('displays pagination controls', async () => {
    // Assert: page info "Page 1 of 3", next button enabled, previous disabled
  });

  it('navigates to next page', async () => {
    // Click next
    // Assert: page 2 data loaded
  });

  it('validates date range', async () => {
    // Set end date before start date
    // Assert: validation error
  });
});
```

### 4.5 Utility Tests

```typescript
// utils/__tests__/validators.test.ts
describe('validateConversionForm', () => {
  it('returns null for valid input', () => { /* ... */ });
  it('returns error for missing source currency', () => { /* ... */ });
  it('returns error for same source and target', () => { /* ... */ });
  it('returns error for zero amount', () => { /* ... */ });
  it('returns error for negative amount', () => { /* ... */ });
});

// utils/__tests__/formatters.test.ts
describe('formatRate', () => {
  it('formats rates with proper decimal places', () => { /* ... */ });
});
describe('formatAmount', () => {
  it('formats amounts as currency', () => { /* ... */ });
});
describe('formatDate', () => {
  it('formats ISO date strings to readable format', () => { /* ... */ });
});
```

### 4.6 Component Tests

```typescript
// components/ui/__tests__/Pagination.test.tsx
describe('Pagination', () => {
  it('renders page info correctly', () => { /* ... */ });
  it('disables previous on first page', () => { /* ... */ });
  it('disables next on last page', () => { /* ... */ });
  it('calls onPageChange when clicking next', () => { /* ... */ });
  it('calls onPageSizeChange when selecting new size', () => { /* ... */ });
});

// components/ui/__tests__/ErrorMessage.test.tsx
describe('ErrorMessage', () => {
  it('displays error message', () => { /* ... */ });
  it('shows retry button when onRetry provided', () => { /* ... */ });
  it('extracts message from Axios error', () => { /* ... */ });
});
```

---

## 5. Running Tests

```bash
# Run all tests
npm test

# Run tests once (CI mode)
npm run test:run

# Run with coverage
npm run test:coverage

# Run specific test file
npx vitest run src/features/conversion/__tests__/ConversionForm.test.tsx
```

---

## 6. Acceptance Criteria

- [ ] All tests pass with `npm run test:run`
- [ ] MSW mocks all API endpoints used by the frontend
- [ ] Login flow is tested (success, failure, validation)
- [ ] Currency conversion flow is tested (success, error, validation, swap)
- [ ] Latest rates display is tested (load, change base, error)
- [ ] Historical rates view is tested (date selection, pagination)
- [ ] Pagination component is tested in isolation
- [ ] Utility functions (validators, formatters) have unit tests
- [ ] Custom render helper wraps all required providers
- [ ] No tests hit the real backend API
- [ ] Test coverage report can be generated with `npm run test:coverage`

---

## 7. Notes for Agent

- Use `@testing-library/user-event` (not `fireEvent`) for user interactions — it's more realistic.
- Use `screen` queries from RTL: `screen.getByRole`, `screen.getByText`, `screen.findByText` (async).
- Use `waitFor` for async assertions; use `findBy` queries which are already async.
- MSW v2 uses `http.get()` / `http.post()` syntax (not `rest.get()`).
- Override specific handlers per test with `server.use(...)` for error scenarios.
- React Query retry must be **disabled** in tests to avoid flaky behavior.
- The `renderWithProviders` helper should create a **new** QueryClient per test to avoid shared state.
- Focus on **user-visible behavior** — don't test implementation details.
- Test what the user sees and interacts with, not internal state.
