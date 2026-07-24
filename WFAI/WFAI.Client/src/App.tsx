import { BrowserRouter, Routes, Route, Navigate, Outlet } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { ReactQueryDevtools } from '@tanstack/react-query-devtools'
import { lazy, Suspense } from 'react'
import LoginLayout from './layouts/LoginLayout'
import Login from './pages/Login'
import Register from './pages/Register'
import ConfirmEmail from './pages/ConfirmEmail'
import ResendConfirmation from './pages/ResendConfirmation'
import ConfirmEmailChange from './pages/ConfirmEmailChange'
import ForgotPassword from './pages/ForgotPassword'
import ResetPassword from './pages/ResetPassword'
import AdminHome from './pages/AdminHome'
import PublicHome from './pages/PublicHome'
import Profile from './pages/Profile'
import AdminLayout from './layouts/AdminLayout'
import { ToastProvider } from './components/ui/toast'
import { AuthProvider, useAuth } from './components/AuthContext'
import { ProtectedRoute } from './components/ProtectedRoute'
import './App.css'

const UsersManagement = lazy(() => import('./pages/UsersManagement'))
const RoleManagement = lazy(() => import('./pages/RoleManagement'))
const CategoriesManagement = lazy(() => import('./pages/CategoriesManagement'))
const AuditLogsManagement = lazy(() => import('./pages/AuditLogsManagement'))

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: false,
    },
  },
})

// Helper component to redirect authenticated users away from Auth pages
function PublicOnlyRoute() {
  const { isAuthenticated, user } = useAuth()

  if (isAuthenticated && user) {
    const isAdmin = user.roles.includes('Admin')
    return <Navigate to={isAdmin ? '/admin' : '/'} replace />
  }

  return <Outlet />
}

function AppContent() {
  return (
    <Routes>
      {/* Public Home Page - Open to all */}
      <Route path="/" element={<PublicHome />} />

      {/* Guest/Auth Only Group (Login, Register, etc.) */}
      <Route element={<PublicOnlyRoute />}>
        <Route element={<LoginLayout />}>
          <Route path="/login" element={<Login />} />
          <Route path="/register" element={<Register />} />
          <Route path="/confirm-email" element={<ConfirmEmail />} />
          <Route path="/resend-confirmation" element={<ResendConfirmation />} />
          <Route path="/confirm-email-change" element={<ConfirmEmailChange />} />
          <Route path="/forgot-password" element={<ForgotPassword />} />
          <Route path="/reset-password" element={<ResetPassword />} />
        </Route>
      </Route>

      {/* Authenticated Route Group - Protected for all authenticated users, wrapped in AdminLayout */}
      <Route element={<ProtectedRoute />}>
        <Route element={<AdminLayout />}>
          <Route path="/profile" element={<Profile />} />
          
          {/* Admin Role Only Route */}
          <Route element={<ProtectedRoute allowedRoles={['Admin']} />}>
            <Route path="/admin" element={<AdminHome />} />
          </Route>

          {/* Permission-Guarded Routes */}
          <Route element={<ProtectedRoute allowedPermissions={['Permission.Identity.Users.Read']} />}>
            <Route path="/admin/users" element={
              <Suspense fallback={<div className="p-8 text-center text-neutral-400">Loading...</div>}>
                <UsersManagement />
              </Suspense>
            } />
          </Route>
          
          <Route element={<ProtectedRoute allowedPermissions={['Permission.Identity.Roles.Read']} />}>
            <Route path="/admin/roles" element={
              <Suspense fallback={<div className="p-8 text-center text-neutral-400">Loading...</div>}>
                <RoleManagement />
              </Suspense>
            } />
          </Route>

          <Route element={<ProtectedRoute allowedPermissions={['Permission.Product.Categories.Read']} />}>
            <Route path="/admin/categories" element={
              <Suspense fallback={<div className="p-8 text-center text-neutral-400">Loading...</div>}>
                <CategoriesManagement />
              </Suspense>
            } />
          </Route>

          <Route element={<ProtectedRoute allowedPermissions={['Permission.Identity.AuditTrails.Read']} />}>
            <Route path="/admin/audit-logs" element={
              <Suspense fallback={<div className="p-8 text-center text-neutral-400">Loading...</div>}>
                <AuditLogsManagement />
              </Suspense>
            } />
          </Route>
        </Route>
      </Route>

      {/* Catch-all redirect */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ToastProvider>
        <AuthProvider>
          <BrowserRouter>
            <AppContent />
          </BrowserRouter>
        </AuthProvider>
      </ToastProvider>
      <ReactQueryDevtools initialIsOpen={false} />
    </QueryClientProvider>
  )
}

export default App
