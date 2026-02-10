import { Routes, Route, Navigate } from 'react-router-dom';
import { Layout } from '@/components/layout/Layout';
import { ProtectedRoute } from '@/components/auth/ProtectedRoute';
import { LoginPage } from '@/features/auth/LoginPage';
import { ConversionPage } from '@/features/conversion/ConversionPage';
import { LatestRatesPage } from '@/features/rates/LatestRatesPage';
import { HistoryPage } from '@/features/history/HistoryPage';

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

export default App;
