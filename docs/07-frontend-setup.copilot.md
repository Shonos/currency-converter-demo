# Sub-Task 07: Frontend Project Setup

> **Context**: Use with `00-master.copilot.md`. This task has **no backend dependencies** and can run in parallel with backend tasks.

---

## Objective

Scaffold a React/TypeScript frontend application using Vite, configure tooling (ESLint, Prettier, Tailwind CSS), set up routing, and establish the project structure for feature development.

---

## 1. Project Initialization

### 1.1 Create React + TypeScript + Vite Project

```bash
cd currency-converter-web
npm create vite@latest . -- --template react-ts
npm install
```

### 1.2 Install Dependencies

```bash
# Core dependencies
npm install react-router-dom axios @tanstack/react-query date-fns

# UI
npm install tailwindcss @tailwindcss/vite
npm install lucide-react             # Icons
npm install clsx                      # Conditional classnames
npm install react-hot-toast           # Toast notifications

# Dev dependencies
npm install -D @types/react @types/react-dom
npm install -D eslint @eslint/js typescript-eslint
npm install -D prettier eslint-config-prettier
npm install -D @testing-library/react @testing-library/jest-dom @testing-library/user-event
npm install -D vitest jsdom @vitest/coverage-v8
npm install -D msw                    # API mocking for tests
```

---

## 2. Project Structure

```
currency-converter-web/
├── public/
│   └── favicon.ico
├── src/
│   ├── api/
│   │   ├── client.ts                 # Axios instance with interceptors
│   │   ├── auth.ts                   # Login API calls
│   │   ├── currencies.ts             # Currency API calls
│   │   └── exchangeRates.ts          # Exchange rate API calls
│   ├── components/
│   │   ├── ui/                       # Generic UI components
│   │   │   ├── Button.tsx
│   │   │   ├── Input.tsx
│   │   │   ├── Select.tsx
│   │   │   ├── Card.tsx
│   │   │   ├── Spinner.tsx
│   │   │   ├── ErrorMessage.tsx
│   │   │   └── Pagination.tsx
│   │   ├── layout/
│   │   │   ├── Header.tsx
│   │   │   ├── Footer.tsx
│   │   │   └── Layout.tsx
│   │   └── auth/
│   │       ├── LoginForm.tsx
│   │       └── ProtectedRoute.tsx
│   ├── features/
│   │   ├── conversion/
│   │   │   ├── ConversionPage.tsx
│   │   │   ├── ConversionForm.tsx
│   │   │   └── ConversionResult.tsx
│   │   ├── rates/
│   │   │   ├── LatestRatesPage.tsx
│   │   │   └── RatesTable.tsx
│   │   └── history/
│   │       ├── HistoryPage.tsx
│   │       ├── DateRangeSelector.tsx
│   │       └── HistoryTable.tsx
│   ├── hooks/
│   │   ├── useAuth.ts
│   │   ├── useCurrencies.ts
│   │   ├── useLatestRates.ts
│   │   ├── useConversion.ts
│   │   └── useHistoricalRates.ts
│   ├── context/
│   │   └── AuthContext.tsx
│   ├── types/
│   │   ├── api.ts                    # API response types
│   │   ├── auth.ts                   # Auth-related types
│   │   └── currency.ts               # Currency domain types
│   ├── utils/
│   │   ├── constants.ts              # API base URL, excluded currencies
│   │   ├── formatters.ts             # Number/date formatting
│   │   └── validators.ts             # Client-side validation
│   ├── App.tsx
│   ├── main.tsx
│   └── index.css
├── .env                              # VITE_API_BASE_URL=http://localhost:5000
├── .env.production                   # VITE_API_BASE_URL=https://api.example.com
├── index.html
├── package.json
├── tsconfig.json
├── tsconfig.app.json
├── tsconfig.node.json
├── vite.config.ts
├── eslint.config.js
├── .prettierrc
└── vitest.config.ts
```

---

## 3. Configuration Files

### 3.1 Vite Config

```typescript
// vite.config.ts
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5000',  // Backend API
        changeOrigin: true,
      },
    },
  },
  resolve: {
    alias: {
      '@': '/src',
    },
  },
});
```

### 3.2 TypeScript Config

```json
// tsconfig.json
{
  "compilerOptions": {
    "target": "ES2020",
    "useDefineForClassFields": true,
    "lib": ["ES2020", "DOM", "DOM.Iterable"],
    "module": "ESNext",
    "skipLibCheck": true,
    "moduleResolution": "bundler",
    "allowImportingTsExtensions": true,
    "isolatedModules": true,
    "moduleDetection": "force",
    "noEmit": true,
    "jsx": "react-jsx",
    "strict": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noFallthroughCasesInSwitch": true,
    "paths": {
      "@/*": ["./src/*"]
    }
  },
  "include": ["src"]
}
```

### 3.3 Vitest Config

```typescript
// vitest.config.ts
import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: './src/test/setup.ts',
    css: true,
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html'],
      exclude: ['node_modules/', 'src/test/'],
    },
  },
  resolve: {
    alias: {
      '@': '/src',
    },
  },
});
```

### 3.4 Test Setup File

```typescript
// src/test/setup.ts
import '@testing-library/jest-dom';
```

### 3.5 Environment Variables

```env
# .env
VITE_API_BASE_URL=http://localhost:5000

# .env.production
VITE_API_BASE_URL=https://api.production.example.com
```

### 3.6 Tailwind CSS (v4)

```css
/* src/index.css */
@import "tailwindcss";
```

---

## 4. Core Infrastructure Code

### 4.1 API Client (`src/api/client.ts`)

```typescript
import axios from 'axios';

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api',
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor: attach JWT token
apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Response interceptor: handle 401 (redirect to login)
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('token');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default apiClient;
```

### 4.2 Auth Context (`src/context/AuthContext.tsx`)

```typescript
interface AuthContextType {
  token: string | null;
  role: string | null;
  isAuthenticated: boolean;
  login: (username: string, password: string) => Promise<void>;
  logout: () => void;
}
```

### 4.3 React Query Provider (`src/main.tsx`)

```typescript
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000,    // 5 minutes
      retry: 2,
      refetchOnWindowFocus: false,
    },
  },
});

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <AuthProvider>
          <App />
        </AuthProvider>
      </BrowserRouter>
    </QueryClientProvider>
  </StrictMode>
);
```

### 4.4 Routing (`src/App.tsx`)

```typescript
import { Routes, Route, Navigate } from 'react-router-dom';

function App() {
  return (
    <Layout>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/" element={<ProtectedRoute />}>
          <Route index element={<Navigate to="/convert" replace />} />
          <Route path="convert" element={<ConversionPage />} />
          <Route path="rates" element={<LatestRatesPage />} />
          <Route path="history" element={<HistoryPage />} />
        </Route>
      </Routes>
    </Layout>
  );
}
```

---

## 5. TypeScript Types

### 5.1 API Types (`src/types/api.ts`)

```typescript
export interface ApiError {
  type: string;
  title: string;
  status: number;
  detail: string;
  traceId: string;
}

export interface PaginatedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}
```

### 5.2 Currency Types (`src/types/currency.ts`)

```typescript
export interface Currency {
  code: string;
  name: string;
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
```

### 5.3 Auth Types (`src/types/auth.ts`)

```typescript
export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  role: string;
}
```

---

## 6. NPM Scripts

```json
{
  "scripts": {
    "dev": "vite",
    "build": "tsc -b && vite build",
    "preview": "vite preview",
    "lint": "eslint .",
    "format": "prettier --write .",
    "test": "vitest",
    "test:run": "vitest run",
    "test:coverage": "vitest run --coverage"
  }
}
```

---

## 7. Acceptance Criteria

- [ ] `npm run dev` starts the dev server on port 5173
- [ ] `npm run build` produces a production build with no errors
- [ ] TypeScript strict mode enabled with zero type errors
- [ ] Tailwind CSS is configured and working
- [ ] React Router routes are defined (login, convert, rates, history)
- [ ] Axios client is configured with JWT interceptor
- [ ] Auth context and protected route wrapper exist
- [ ] React Query is configured with sensible defaults
- [ ] All TypeScript types defined for API responses
- [ ] ESLint and Prettier are configured
- [ ] Vitest is configured and a sample test passes
- [ ] Environment variables are set up for API base URL
- [ ] Path alias `@/` works for imports
- [ ] Vite proxy is configured for `/api` → backend

---

## 8. Notes for Agent

- Use **Tailwind CSS v4** (imported via `@import "tailwindcss"` in CSS, plugin in Vite config).
- Use **React Query** (`@tanstack/react-query`) for all API calls — do NOT use `useEffect` + `useState` for data fetching.
- Keep the Axios client as a **single instance** with interceptors for auth.
- The `ProtectedRoute` component should check `AuthContext` and redirect to `/login` if not authenticated.
- **Do NOT implement** the actual feature components yet — that's sub-task 08.
- Create **stub/placeholder** components for each page so routing works.
- The Vite proxy is for development only. In production, the frontend is served from a separate origin and uses CORS.
