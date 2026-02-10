// Excluded currencies based on business rules
export const EXCLUDED_CURRENCIES = ['TRY', 'PLN', 'THB', 'MXN'];

// API base URL from environment variable
export const API_BASE_URL = import.meta.env.VITE_CURRENCY_CONVERTER_API_URL || '/api';
