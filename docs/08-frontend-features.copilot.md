# Sub-Task 08: Frontend Core Features

> **Context**: Use with `00-master.copilot.md`. **Depends on**: Sub-task 07 (frontend setup must be complete).

---

## Objective

Implement all three core features of the frontend: currency conversion form, latest exchange rates display, and paginated historical rates view. Include proper loading states, error handling, and clean component composition.

---

## 1. Feature 1: Login Page

### 1.1 Components

- `LoginPage.tsx` – Page wrapper
- `LoginForm.tsx` – Form component

### 1.2 Behavior

- Username and password fields
- Form validation (required fields)
- Submit calls `POST /api/v1/auth/login`
- On success: store token in localStorage, update AuthContext, redirect to `/convert`
- On failure: show error message (invalid credentials)
- Loading spinner during submission

### 1.3 UI Sketch

```
┌──────────────────────────────────┐
│       Currency Converter         │
│                                  │
│  ┌────────────────────────────┐  │
│  │ Username                   │  │
│  └────────────────────────────┘  │
│  ┌────────────────────────────┐  │
│  │ Password                   │  │
│  └────────────────────────────┘  │
│                                  │
│  [ Sign In ]                     │
│                                  │
│  Demo: admin/Admin123!           │
└──────────────────────────────────┘
```

---

## 2. Feature 2: Currency Conversion

### 2.1 Components

```
features/conversion/
├── ConversionPage.tsx       # Page layout
├── ConversionForm.tsx       # Input form (amount, from, to)
├── ConversionResult.tsx     # Result display
└── CurrencySwapButton.tsx   # Swap from/to button
```

### 2.2 API Hook (`hooks/useConversion.ts`)

```typescript
import { useMutation } from '@tanstack/react-query';
import { convertCurrency } from '@/api/exchangeRates';

export function useConversion() {
  return useMutation({
    mutationFn: ({ from, to, amount }: ConversionParams) =>
      convertCurrency(from, to, amount),
  });
}
```

### 2.3 Behavior

- **Amount input**: numeric field, positive values only
- **From/To selectors**: dropdowns populated from `GET /api/v1/currencies`
  - Excluded currencies (TRY, PLN, THB, MXN) are already filtered server-side
- **Swap button**: swaps from and to currencies
- **Convert button**: calls API, shows result
- **Loading state**: spinner/disabled button during API call
- **Error handling**:
  - 400 (excluded currency): show error from API detail
  - 401: redirect to login
  - Network error: "Unable to reach the server"
  - General error: display error message from API

### 2.4 Result Display

```
┌──────────────────────────────────────────┐
│  100.00 EUR = 117.94 USD                 │
│                                          │
│  Rate: 1 EUR = 1.1794 USD               │
│  Date: February 6, 2024                  │
└──────────────────────────────────────────┘
```

### 2.5 Client-Side Validation

```typescript
// utils/validators.ts
export function validateConversionForm(from: string, to: string, amount: number): string | null {
  if (!from) return 'Please select a source currency';
  if (!to) return 'Please select a target currency';
  if (from === to) return 'Source and target currencies must be different';
  if (!amount || amount <= 0) return 'Amount must be greater than 0';
  return null; // valid
}
```

---

## 3. Feature 3: Latest Exchange Rates

### 3.1 Components

```
features/rates/
├── LatestRatesPage.tsx      # Page layout with base currency selector
├── RatesTable.tsx           # Table displaying rates
└── RateRow.tsx              # Individual rate row (optional)
```

### 3.2 API Hook (`hooks/useLatestRates.ts`)

```typescript
import { useQuery } from '@tanstack/react-query';
import { getLatestRates } from '@/api/exchangeRates';

export function useLatestRates(baseCurrency: string) {
  return useQuery({
    queryKey: ['latestRates', baseCurrency],
    queryFn: () => getLatestRates(baseCurrency),
    staleTime: 5 * 60 * 1000,  // 5 min cache
    enabled: !!baseCurrency,
  });
}
```

### 3.3 Behavior

- **Base currency selector**: dropdown, default EUR
- **Auto-fetch**: rates load automatically when base currency changes
- **Table columns**: Currency Code, Currency Name, Exchange Rate
- **Sorting**: click column header to sort (client-side)
- **Search/Filter**: optional text input to filter currencies
- **Loading state**: skeleton loader or spinner
- **Error handling**: inline error banner with retry button

### 3.4 UI Sketch

```
┌──────────────────────────────────────────────────────┐
│  Latest Exchange Rates                               │
│                                                      │
│  Base Currency: [ EUR ▼ ]     Last updated: Feb 6    │
│                                                      │
│  ┌──────────┬─────────────────────┬──────────────┐   │
│  │ Code     │ Currency            │ Rate         │   │
│  ├──────────┼─────────────────────┼──────────────┤   │
│  │ AUD      │ Australian Dollar   │ 1.6880       │   │
│  │ BRL      │ Brazilian Real      │ 6.1767       │   │
│  │ CAD      │ Canadian Dollar     │ 1.6118       │   │
│  │ CHF      │ Swiss Franc         │ 0.9175       │   │
│  │ GBP      │ British Pound       │ 0.8679       │   │
│  │ JPY      │ Japanese Yen        │ 185.27       │   │
│  │ USD      │ United States Dollar│ 1.1794       │   │
│  └──────────┴─────────────────────┴──────────────┘   │
└──────────────────────────────────────────────────────┘
```

---

## 4. Feature 4: Historical Rates (Paginated)

### 4.1 Components

```
features/history/
├── HistoryPage.tsx           # Page layout
├── DateRangeSelector.tsx     # Start/end date pickers
├── HistoryTable.tsx          # Paginated results table
└── HistoryFilters.tsx        # Base currency + optional target filter
```

### 4.2 API Hook (`hooks/useHistoricalRates.ts`)

```typescript
import { useQuery, keepPreviousData } from '@tanstack/react-query';
import { getHistoricalRates } from '@/api/exchangeRates';

export function useHistoricalRates(
  baseCurrency: string,
  startDate: string,
  endDate: string,
  page: number,
  pageSize: number
) {
  return useQuery({
    queryKey: ['historicalRates', baseCurrency, startDate, endDate, page, pageSize],
    queryFn: () => getHistoricalRates(baseCurrency, startDate, endDate, page, pageSize),
    enabled: !!baseCurrency && !!startDate && !!endDate,
    placeholderData: keepPreviousData,  // Keep showing old data while loading next page
  });
}
```

### 4.3 Behavior

- **Date range selector**: two date inputs (start, end)
  - Default: last 30 days
  - Validation: start < end, max 365 days
- **Base currency selector**: dropdown
- **Fetch button**: triggers the API call
- **Results table**:
  - Columns: Date, then one column per currency rate
  - Or: Date, Currency, Rate (flat format)
- **Pagination**:
  - Page navigation: Previous / Next buttons
  - Page info: "Page 1 of 3 (23 results)"
  - Page size selector: 10, 25, 50
- **Loading state**: show skeleton/spinner; keep previous data visible (React Query `keepPreviousData`)
- **Empty state**: "No data available for the selected range"

### 4.4 UI Sketch

```
┌──────────────────────────────────────────────────────────┐
│  Historical Exchange Rates                               │
│                                                          │
│  Base: [ EUR ▼ ]                                         │
│  From: [ 2024-01-01 ]  To: [ 2024-01-31 ]  [ Search ]   │
│                                                          │
│  ┌────────────┬─────────┬─────────┬─────────┬─────────┐  │
│  │ Date       │ AUD     │ CAD     │ GBP     │ USD     │  │
│  ├────────────┼─────────┼─────────┼─────────┼─────────┤  │
│  │ 2024-01-02 │ 1.6147  │ 1.4565  │ 0.8601  │ 1.0956  │  │
│  │ 2024-01-03 │ 1.6236  │ 1.4574  │ 0.8592  │ 1.0919  │  │
│  │ 2024-01-04 │ 1.6280  │ 1.4603  │ 0.8604  │ 1.0953  │  │
│  │ ...        │ ...     │ ...     │ ...     │ ...     │  │
│  └────────────┴─────────┴─────────┴─────────┴─────────┘  │
│                                                          │
│  ◀ Previous    Page 1 of 3 (23 results)    Next ▶        │
│  Show: [10 ▼] per page                                   │
└──────────────────────────────────────────────────────────┘
```

---

## 5. API Functions (`src/api/exchangeRates.ts`)

```typescript
import apiClient from './client';
import type { LatestRatesResponse, ConversionResponse, PagedHistoricalRatesResponse } from '@/types/currency';

export async function getLatestRates(baseCurrency: string): Promise<LatestRatesResponse> {
  const { data } = await apiClient.get('/v1/exchange-rates/latest', {
    params: { baseCurrency },
  });
  return data;
}

export async function convertCurrency(
  from: string, to: string, amount: number
): Promise<ConversionResponse> {
  const { data } = await apiClient.get('/v1/exchange-rates/convert', {
    params: { from, to, amount },
  });
  return data;
}

export async function getHistoricalRates(
  baseCurrency: string,
  startDate: string,
  endDate: string,
  page: number,
  pageSize: number
): Promise<PagedHistoricalRatesResponse> {
  const { data } = await apiClient.get('/v1/exchange-rates/history', {
    params: { baseCurrency, startDate, endDate, page, pageSize },
  });
  return data;
}
```

### `src/api/currencies.ts`

```typescript
import apiClient from './client';
import type { Currency } from '@/types/currency';

export async function getCurrencies(): Promise<Currency[]> {
  const { data } = await apiClient.get('/v1/currencies');
  return data.currencies;
}
```

### `src/api/auth.ts`

```typescript
import apiClient from './client';
import type { LoginRequest, LoginResponse } from '@/types/auth';

export async function login(credentials: LoginRequest): Promise<LoginResponse> {
  const { data } = await apiClient.post('/v1/auth/login', credentials);
  return data;
}
```

---

## 6. Shared Hooks

### `hooks/useCurrencies.ts`

```typescript
import { useQuery } from '@tanstack/react-query';
import { getCurrencies } from '@/api/currencies';

export function useCurrencies() {
  return useQuery({
    queryKey: ['currencies'],
    queryFn: getCurrencies,
    staleTime: 24 * 60 * 60 * 1000,  // 24 hours (currencies rarely change)
  });
}
```

---

## 7. Reusable UI Components

### 7.1 `Pagination.tsx`

```typescript
interface PaginationProps {
  page: number;
  totalPages: number;
  totalCount: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  onPageChange: (page: number) => void;
  pageSize: number;
  onPageSizeChange: (size: number) => void;
}
```

### 7.2 `ErrorMessage.tsx`

```typescript
interface ErrorMessageProps {
  error: unknown;
  onRetry?: () => void;
}

// Extracts message from Axios error or API ProblemDetails
```

### 7.3 `Spinner.tsx`

Simple loading spinner component for consistent loading states.

---

## 8. Error Handling Strategy

### 8.1 API Error Extraction

```typescript
// utils/errorHandling.ts
import type { ApiError } from '@/types/api';
import { AxiosError } from 'axios';

export function getErrorMessage(error: unknown): string {
  if (error instanceof AxiosError) {
    const apiError = error.response?.data as ApiError | undefined;
    if (apiError?.detail) return apiError.detail;
    if (error.response?.status === 401) return 'Please log in to continue.';
    if (error.response?.status === 403) return 'You do not have permission for this action.';
    if (error.response?.status === 429) return 'Too many requests. Please wait and try again.';
    if (error.message) return error.message;
  }
  return 'An unexpected error occurred. Please try again.';
}
```

### 8.2 Error Display Patterns

- **Inline errors**: below form fields for validation
- **Banner errors**: top of page for API errors
- **Toast notifications**: for transient success/error messages
- **Full page errors**: for auth failures (redirect to login)

---

## 9. Number & Date Formatting

```typescript
// utils/formatters.ts
export function formatRate(rate: number): string {
  return rate.toLocaleString(undefined, {
    minimumFractionDigits: 2,
    maximumFractionDigits: 6,
  });
}

export function formatAmount(amount: number, currency: string): string {
  return new Intl.NumberFormat(undefined, {
    style: 'currency',
    currency,
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  }).format(amount);
}

export function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });
}
```

---

## 10. Responsive Design

- Mobile-first approach with Tailwind responsive classes
- Conversion form: single column on mobile, side-by-side on desktop
- Rates table: horizontal scroll on mobile
- Navigation: hamburger menu on mobile

---

## 11. Acceptance Criteria

- [ ] Login page works with demo credentials (admin/Admin123!)
- [ ] After login, token is stored and user is redirected
- [ ] Currency conversion form works end-to-end
- [ ] Converting with excluded currency shows clear error message
- [ ] Swap button swaps source and target currencies
- [ ] Latest rates table loads and displays correctly
- [ ] Changing base currency refreshes the rates
- [ ] Historical rates view has working date range selector
- [ ] Pagination controls work (next, previous, page size)
- [ ] Loading spinners shown during API calls
- [ ] Error messages displayed for API failures
- [ ] Client-side validation prevents invalid submissions
- [ ] UI is responsive on mobile/tablet/desktop
- [ ] Logout clears token and redirects to login
- [ ] Navigation between pages works correctly

---

## 12. Notes for Agent

- Use **React Query** for all data fetching — `useQuery` for GET, `useMutation` for POST.
- Keep components **small and focused** — extract sub-components when a file exceeds ~100 lines.
- Use **controlled components** for forms.
- **Do NOT use** `useEffect` for data fetching — React Query handles this.
- Currency dropdowns must call the `/currencies` endpoint (which already excludes restricted currencies).
- Use `keepPreviousData` in React Query for pagination to avoid flash of empty state.
- All API errors should be caught and displayed — never show raw JSON to the user.
- Use **toast notifications** (react-hot-toast) for success messages and transient errors.
- Use Tailwind utility classes — avoid writing custom CSS unless absolutely necessary.
