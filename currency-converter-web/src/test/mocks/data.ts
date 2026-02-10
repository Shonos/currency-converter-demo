/**
 * Mock data constants for tests
 */

export const mockCurrencies = [
  { code: 'EUR', name: 'Euro' },
  { code: 'USD', name: 'United States Dollar' },
  { code: 'GBP', name: 'British Pound' },
  { code: 'JPY', name: 'Japanese Yen' },
  { code: 'AUD', name: 'Australian Dollar' },
  { code: 'CAD', name: 'Canadian Dollar' },
];

export const mockExcludedCurrencies = ['TRY', 'PLN', 'THB', 'MXN'];

export const mockLatestRates = {
  baseCurrency: 'EUR',
  date: '2026-02-06',
  rates: {
    AUD: 1.688,
    CAD: 1.4563,
    GBP: 0.8679,
    JPY: 185.27,
    USD: 1.1794,
  },
};

export const mockConversionResponse = {
  from: 'EUR',
  to: 'USD',
  amount: 100,
  convertedAmount: 117.94,
  rate: 1.1794,
  date: '2026-02-06',
};

export const mockHistoricalRates = {
  baseCurrency: 'EUR',
  startDate: '2026-01-01',
  endDate: '2026-01-31',
  page: 1,
  pageSize: 10,
  totalCount: 23,
  totalPages: 3,
  hasNextPage: true,
  hasPreviousPage: false,
  rates: [
    { date: '2026-01-02', rates: { USD: 1.0956, GBP: 0.8601 } },
    { date: '2026-01-03', rates: { USD: 1.0919, GBP: 0.8592 } },
    { date: '2026-01-06', rates: { USD: 1.0933, GBP: 0.8605 } },
    { date: '2026-01-07', rates: { USD: 1.0945, GBP: 0.8612 } },
    { date: '2026-01-08', rates: { USD: 1.0921, GBP: 0.8598 } },
    { date: '2026-01-09', rates: { USD: 1.0937, GBP: 0.8603 } },
    { date: '2026-01-10', rates: { USD: 1.0952, GBP: 0.8609 } },
    { date: '2026-01-13', rates: { USD: 1.0968, GBP: 0.8615 } },
    { date: '2026-01-14', rates: { USD: 1.0941, GBP: 0.8607 } },
    { date: '2026-01-15', rates: { USD: 1.0929, GBP: 0.8594 } },
  ],
};

export const mockAuthResponse = {
  token: 'mock-jwt-token-eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9',
  expiresAt: '2026-12-31T23:59:59Z',
  role: 'Admin',
};
