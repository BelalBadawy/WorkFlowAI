import React, { useEffect, useState } from 'react';
import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from './AuthContext';
import { useToast } from './ui/toast';
import { Loader2 } from 'lucide-react';

interface ProtectedRouteProps {
  allowedRoles?: string[];
  allowedPermissions?: string[];
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ allowedRoles, allowedPermissions }) => {
  const { user, isAuthenticated, loading, refreshAccessToken } = useAuth();
  const toast = useToast();
  const [checkingToken, setCheckingToken] = useState(true);
  const [permissionDenied, setPermissionDenied] = useState(false);

  useEffect(() => {
    const checkAuthStatus = async () => {
      const storedToken = localStorage.getItem('token');
      if (storedToken) {
        const { isTokenExpired } = await import('../lib/jwt');
        if (isTokenExpired(storedToken)) {
          await refreshAccessToken();
        }
      }
      setCheckingToken(false);
    };

    if (!loading) {
      checkAuthStatus();
    }
  }, [loading, refreshAccessToken]);

  useEffect(() => {
    if (!loading && !checkingToken && isAuthenticated && user && !permissionDenied) {
      // Check permissions if specified
      if (allowedPermissions) {
        const hasPerm = allowedPermissions.some((perm) => user.permissions.includes(perm));
        if (!hasPerm) {
          toast.error("Access Denied: You do not have permission to access this resource.");
          setPermissionDenied(true);
        }
      }
      
      // Check roles if specified
      if (allowedRoles) {
        const hasRole = user.roles.some((role) => allowedRoles.includes(role));
        if (!hasRole) {
          toast.error("Access Denied: You do not have the required role to access this resource.");
          setPermissionDenied(true);
        }
      }
    }
  }, [loading, checkingToken, isAuthenticated, user, allowedPermissions, allowedRoles, toast, permissionDenied]);

  if (loading || checkingToken) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-neutral-50">
        <div className="flex flex-col items-center gap-3">
          <Loader2 className="w-8 h-8 text-[#4285F4] animate-spin" />
          <p className="text-sm text-neutral-500 font-medium">Verifying authorization...</p>
        </div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (permissionDenied) {
    // If Admin, redirect to admin home, else basic home
    const isAdmin = user?.roles.includes('Admin');
    return <Navigate to={isAdmin ? '/admin' : '/'} replace />;
  }

  return <Outlet />;
};