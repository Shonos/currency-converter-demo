import { http, HttpResponse } from 'msw';
import type { LoginRequest, LoginResponse } from '@/types/auth';
import {
  mockCurrencies,
  mockLatestRates,
  mockConversionResponse,
  mockHistoricalRates,
  mockAuthResponse,
  mockExcludedCurrencies,
} from './data';

const API_BASE = import.meta.env.VITE_CURRENCY_CONVERTER_API_URL || 'https://localhost:7241';

export const handlers = [
  // Auth - Login
  http.post(`${API_BASE}/api/v1/auth/login`, async ({ request }) => {
    const body = (await request.json()) as LoginRequest;

    if (body.username === 'admin' && body.password === 'Admin123!') {
      return HttpResponse.json<LoginResponse>(mockAuthResponse);
    }

    return HttpResponse.json(
      {
        type: 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
        title: 'Unauthorized',
        status: 401,
        detail: 'Invalid username or password.',
      },
      { status: 401 }
    );
  }),

  // Currencies - Get all
  http.get(`${API_BASE}/api/v1/currencies`, () => {
    return HttpResponse.json({
      currencies: mockCurrencies,
    });
  }),

  // Exchange Rates - Latest
  http.get(`${API_BASE}/api/v1/exchange-rates/latest`, ({ request }) => {
    const url = new URL(request.url);
    const baseCurrency = url.searchParams.get('baseCurrency') || 'EUR';

    return HttpResponse.json({
      ...mockLatestRates,
      baseCurrency,
    });
  }),

  // Exchange Rates - Convert
  http.get(`${API_BASE}/api/v1/exchange-rates/convert`, ({ request }) => {
    const url = new URL(request.url);
    const from = url.searchParams.get('from');
    const to = url.searchParams.get('to');
    const amount = parseFloat(url.searchParams.get('amount') || '0');

    // Check for excluded currencies
    if (
      mockExcludedCurrencies.includes(from!) ||
      mockExcludedCurrencies.includes(to!)
    ) {
      const excludedCurrency = mockExcludedCurrencies.find(
        (c) => c === from || c === to
      );
      return HttpResponse.json(
        {
          type: 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
          title: 'Bad Request',
          status: 400,
          detail: `Currency '${excludedCurrency}' is not supported for conversion.`,
        },
        { status: 400 }
      );
    }

    // Check for same currency
    if (from === to) {
      return HttpResponse.json(
        {
          type: 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
          title: 'Bad Request',
          status: 400,
          detail: 'Source and target currencies cannot be the same.',
        },
        { status: 400 }
      );
    }

    // Return mock conversion
    const rate = from === 'EUR' && to === 'USD' ? 1.1794 : 1.25;
    return HttpResponse.json({
      from,
      to,
      amount,
      convertedAmount: amount * rate,
      rate,
      date: '2026-02-06',
    });
  }),

  // Exchange Rates - Historical
  http.get(`${API_BASE}/api/v1/exchange-rates/history`, ({ request }) => {
    const url = new URL(request.url);
    const baseCurrency = url.searchParams.get('baseCurrency') || 'EUR';
    const startDate = url.searchParams.get('startDate') || '2026-01-01';
    const endDate = url.searchParams.get('endDate') || '2026-01-31';
    const page = parseInt(url.searchParams.get('page') || '1', 10);
    const pageSize = parseInt(url.searchParams.get('pageSize') || '10', 10);

    return HttpResponse.json({
      ...mockHistoricalRates,
      baseCurrency,
      startDate,
      endDate,
      page,
      pageSize,
    });
  }),
];
