import { Navigate, Outlet } from 'react-router'
import { useAuth } from './AuthProvider'

export function RequireAdmin() {
  const { required, user } = useAuth()
  return !required || user?.role !== 'admin' ? <Navigate to="/dashboard" replace /> : <Outlet />
}
