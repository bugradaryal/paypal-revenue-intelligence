import { lazy, Suspense } from 'react'
import { Navigate, Route, Routes } from 'react-router'
import { PageFallback } from './PageFallback'
import { RequireAuth } from '@/features/auth/RequireAuth'
import { RequireAdmin } from '@/features/auth/RequireAdmin'
import { AppShell } from '@/components/layout/AppShell'
import { NotFoundPage } from '@/pages/NotFoundPage'
const AiCostPage = lazy(() => import('@/pages/AiCostPage').then((m) => ({ default: m.AiCostPage })))
const AiWorkspacePage = lazy(() => import('@/pages/AiWorkspacePage').then((m) => ({ default: m.AiWorkspacePage })))
const CashFlowPage = lazy(() => import('@/pages/CashFlowPage').then((m) => ({ default: m.CashFlowPage })))
const HomePage = lazy(() => import('@/pages/HomePage').then((m) => ({ default: m.HomePage })))
const LoginPage = lazy(() => import('@/pages/LoginPage').then((m) => ({ default: m.LoginPage })))
const OverviewPage = lazy(() => import('@/pages/OverviewPage').then((m) => ({ default: m.OverviewPage })))
const PaymentsPage = lazy(() => import('@/pages/PaymentsPage').then((m) => ({ default: m.PaymentsPage })))
const RecoveryCommandPage = lazy(() => import('@/pages/RecoveryCommandPage').then((m) => ({ default: m.RecoveryCommandPage })))
const CustomerCohortsPage = lazy(() => import('@/pages/CustomerCohortsPage').then((m) => ({ default: m.CustomerCohortsPage })))
const RevenueSignalsPage = lazy(() => import('@/pages/RevenueSignalsPage').then((m) => ({ default: m.RevenueSignalsPage })))
const SettingsPage = lazy(() => import('@/pages/SettingsPage').then((m) => ({ default: m.SettingsPage })))
const ComponentsPage = lazy(() => import('@/pages/dev/ComponentsPage').then((m) => ({ default: m.ComponentsPage })))
const DataPage = lazy(() => import('@/pages/dev/DataPage').then((m) => ({ default: m.DataPage })))
const TokensPage = lazy(() => import('@/pages/dev/TokensPage').then((m) => ({ default: m.TokensPage })))
const UsersPage = lazy(() => import('@/pages/UsersPage').then((m) => ({ default: m.UsersPage })))

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<Suspense fallback={<PageFallback />}><HomePage /></Suspense>} />
      <Route path="login" element={<Suspense fallback={<PageFallback />}><LoginPage /></Suspense>} />

      <Route element={<RequireAuth />}>
        <Route element={<AppShell />}>
          <Route path="dashboard" element={<OverviewPage />} />
          <Route path="payments" element={<PaymentsPage />} />
          <Route path="recovery" element={<Navigate to="/command-center" replace />} />
          <Route path="command-center" element={<RecoveryCommandPage />} />
          <Route path="cash-flow" element={<CashFlowPage />} />
          <Route path="cohorts" element={<CustomerCohortsPage />} />
          <Route path="revenue-analytics" element={<Navigate to="/cohorts" replace />} />
          <Route path="signals" element={<RevenueSignalsPage />} />
          <Route path="ai-cost" element={<AiCostPage />} />
          <Route path="ai-workspace" element={<AiWorkspacePage />} />
          <Route element={<RequireAdmin />}>
            <Route path="users" element={<UsersPage />} />
          </Route>
          <Route path="settings" element={<SettingsPage />} />
          <Route path="*" element={<NotFoundPage />} />
        </Route>
      </Route>

      {/* Internal developer pages, outside the app shell. */}
      <Route path="design-system" element={<Suspense fallback={<PageFallback />}><ComponentsPage /></Suspense>} />
      <Route path="design-system/tokens" element={<Suspense fallback={<PageFallback />}><TokensPage /></Suspense>} />
      <Route path="design-system/data" element={<Suspense fallback={<PageFallback />}><DataPage /></Suspense>} />
    </Routes>
  )
}
