import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '@/hooks/useAuth';
import { Button } from '@/components/ui/Button';
import { LogOut, DollarSign } from 'lucide-react';

export const Header: React.FC = () => {
  const { isAuthenticated, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <header className="bg-white shadow">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between items-center h-16">
          <div className="flex items-center">
            <DollarSign className="w-8 h-8 text-blue-600" />
            <span className="ml-2 text-xl font-bold text-gray-900">
              Currency Converter
            </span>
          </div>

          {isAuthenticated && (
            <nav className="flex items-center gap-6">
              <Link
                to="/convert"
                className="text-gray-700 hover:text-blue-600 font-medium"
              >
                Convert
              </Link>
              <Link
                to="/rates"
                className="text-gray-700 hover:text-blue-600 font-medium"
              >
                Latest Rates
              </Link>
              <Link
                to="/history"
                className="text-gray-700 hover:text-blue-600 font-medium"
              >
                History
              </Link>
              <Button
                variant="secondary"
                size="sm"
                onClick={handleLogout}
                className="flex items-center gap-2"
              >
                <LogOut className="w-4 h-4" />
                Logout
              </Button>
            </nav>
          )}
        </div>
      </div>
    </header>
  );
};
