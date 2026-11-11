import { Navigate, Outlet, useLocation } from 'react-router'
import { getLogoutReason } from '@/services/auth'
import { useAuth } from './AuthProvider'

/** Sends signed-out visitors to /login and brings them back afterwards. No-op in mock mode. */
export function RequireAuth() {
  const { required, user } = useAuth()
  const location = useLocation()

  if (required && !user) {
    if (getLogoutReason() === 'manual') return <Navigate to="/" replace />
    return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />
  }

  return <Outlet />
}
