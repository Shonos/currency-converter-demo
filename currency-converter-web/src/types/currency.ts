export interface Currency {
  code: string;
  name: string;
}

export interface CurrenciesResponse {
  currencies: Currency[];
}

export interface LatestRatesResponse {
  baseCurrency: string;
  date: string;
  rates: Record<string, number>;
}

export interface ConversionResponse {
  from: string;
  to: string;
  amount: number;
  convertedAmount: number;
  rate: number;
  date: string;
}

export interface HistoricalRate {
  date: string;
  rates: Record<string, number>;
}

export interface PagedHistoricalRatesResponse {
  baseCurrency: string;
  startDate: string;
  endDate: string;
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  rates: HistoricalRate[];
}
