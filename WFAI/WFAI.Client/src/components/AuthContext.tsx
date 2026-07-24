import React, { createContext, useContext, useState, useEffect } from 'react';
import { decodeToken, isTokenExpired } from '../lib/jwt';
import type { DecodedToken } from '../lib/jwt';
import { api } from '../lib/api-client';

interface AuthContextType {
  user: DecodedToken | null;
  token: string | null;
  loading: boolean;
  login: (token: string, refreshToken: string) => void;
  logout: () => void;
  isAuthenticated: boolean;
  refreshAccessToken: () => Promise<boolean>;
  hasPermission: (permissionName: string) => boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<DecodedToken | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const logout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
    setUser(null);
    setToken(null);
  };

  const login = (newToken: string, newRefreshToken: string) => {
    localStorage.setItem('token', newToken);
    localStorage.setItem('refreshToken', newRefreshToken);
    setToken(newToken);
    const decoded = decodeToken(newToken);
    setUser(decoded);
  };

  const refreshAccessToken = async (): Promise<boolean> => {
    const currentToken = localStorage.getItem('token');
    const currentRefreshToken = localStorage.getItem('refreshToken');

    if (!currentToken || !currentRefreshToken) {
      logout();
      return false;
    }

    try {
      const response = await api.post('api/v1/account/refresh-token', {
        token: currentToken,
        refreshToken: currentRefreshToken,
      });

      if (response.isSuccessful && response.data?.token) {
        const nextToken = response.data.token;
        const nextRefreshToken = response.data.refreshToken || currentRefreshToken;
        login(nextToken, nextRefreshToken);
        return true;
      } else {
        logout();
        return false;
      }
    } catch (error) {
      console.error('Error refreshing access token:', error);
      logout();
      return false;
    }
  };

  const hasPermission = (permissionName: string): boolean => {
    if (!user || !user.permissions) return false;
    return user.permissions.includes(permissionName);
  };

  useEffect(() => {
    const initAuth = async () => {
      const storedToken = localStorage.getItem('token');
      if (storedToken) {
        if (isTokenExpired(storedToken)) {
          const success = await refreshAccessToken();
          if (!success) {
            // Logout already handled in refreshAccessToken
          }
        } else {
          setToken(storedToken);
          setUser(decodeToken(storedToken));
        }
      }
      setLoading(false);
    };

    initAuth();
  }, []);

  const value: AuthContextType = {
    user,
    token,
    loading,
    login,
    logout,
    isAuthenticated: !!user,
    refreshAccessToken,
    hasPermission,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};