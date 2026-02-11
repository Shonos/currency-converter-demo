import apiClient from './client';
import type { LoginRequest, LoginResponse } from '@/types/auth';

/**
 * Authenticate user and receive JWT token
 */
export const login = async (
  credentials: LoginRequest
): Promise<LoginResponse> => {
  const response = await apiClient.post<LoginResponse>(
    '/api/v1/auth/login',
    credentials
  );
  return response.data;
};

/**
 * Logout user and invalidate current token on server
 */
export const logout = async (): Promise<void> => {
  await apiClient.post('/api/v1/auth/logout');
};
