# Currency Converter Web - Frontend

React + TypeScript + Vite frontend application for the Currency Converter platform.

## Setup Complete ✓

This project has been scaffolded according to sub-task 07 specifications. The following has been implemented:

### Configuration
- ✓ Vite 7 with React 19 and TypeScript
- ✓ Tailwind CSS v2 with PostCSS
- ✓ ESLint + Prettier for code quality
- ✓ Vitest for testing with jsdom environment
- ✓ Path aliases configured (`@/*` -> `src/*`)

### Project Structure
- ✓ API client with Axios interceptors for JWT auth
- ✓ React Query for server state management
- ✓ React Router v6 for routing
- ✓ Auth context for authentication state
- ✓ Custom hooks for API calls
- ✓ TypeScript types for all API responses
- ✓ Reusable UI components (Button, Input, Select, Card, etc.)
- ✓ Layout components (Header, Footer, Layout)
- ✓ Protected route wrapper
- ✓ Feature page placeholders (Login, Conversion, Rates, History)

### Scripts

```bash
# Development
npm run dev          # Start dev server on http://localhost:5173

# Build
npm run build        # Production build (includes TypeScript check)
npm run preview      # Preview production build

# Code Quality
npm run lint         # Run ESLint
npm run format       # Format code with Prettier

# Testing
npm run test         # Run tests in watch mode
npm run test:run     # Run tests once
npm run test:coverage # Run tests with coverage report
```

### Environment Variables

Create `.env` files as needed:

```env
# .env (development)
VITE_CURRENCY_CONVERTER_API_URL=https://localhost:7241

# .env.production
VITE_CURRENCY_CONVERTER_API_URL=https://api.production.example.com
```

### Vite Proxy

The dev server is configured with a proxy for `/api` requests to `http://localhost:5000` (backend API).

### Next Steps

The feature implementations (actual UI forms, tables, etc.) will be completed in **sub-task 08** (frontend-features).

Current state:
- All infrastructure and boilerplate is ready
- Placeholder pages exist for all routes
- API client and hooks are implemented
- Can build and run without errors

## License

Demo project - see root README for details.
