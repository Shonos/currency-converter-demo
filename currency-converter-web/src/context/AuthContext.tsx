import React, { createContext, useContext, useState } from 'react';
import { login as apiLogin } from '@/api/auth';
import toast from 'react-hot-toast';

interface AuthContextType {
  token: string | null;
  role: string | null;
  isAuthenticated: boolean;
  login: (username: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({
  children,
}) => {
  const [token, setToken] = useState<string | null>(
    localStorage.getItem('token')
  );
  const [role, setRole] = useState<string | null>(
    localStorage.getItem('role')
  );

  const isAuthenticated = !!token;

  const login = async (username: string, password: string) => {
    try {
      const response = await apiLogin({ username, password });
      setToken(response.token);
      setRole(response.role);
      localStorage.setItem('token', response.token);
      localStorage.setItem('role', response.role);
      toast.success('Login successful!');
    } catch (error) {
      toast.error('Login failed. Please check your credentials.');
      throw error;
    }
  };

  const logout = () => {
    setToken(null);
    setRole(null);
    localStorage.removeItem('token');
    localStorage.removeItem('role');
    toast.success('Logged out successfully');
  };

  return (
    <AuthContext.Provider
      value={{ token, role, isAuthenticated, login, logout }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
