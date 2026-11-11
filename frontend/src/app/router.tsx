import { lazy, Suspense } from 'react'
import { Route, Routes } from 'react-router'
import { PageFallback } from './PageFallback'
import { RequireAuth } from '@/features/auth/RequireAuth'
import { AppShell } from '@/components/layout/AppShell'
import { NotFoundPage } from '@/pages/NotFoundPage'
const AiCostPage = lazy(() => import('@/pages/AiCostPage').then((m) => ({ default: m.AiCostPage })))
const AiWorkspacePage = lazy(() => import('@/pages/AiWorkspacePage').then((m) => ({ default: m.AiWorkspacePage })))
const HomePage = lazy(() => import('@/pages/HomePage').then((m) => ({ default: m.HomePage })))
const LoginPage = lazy(() => import('@/pages/LoginPage').then((m) => ({ default: m.LoginPage })))
const OverviewPage = lazy(() => import('@/pages/OverviewPage').then((m) => ({ default: m.OverviewPage })))
const PaymentsPage = lazy(() => import('@/pages/PaymentsPage').then((m) => ({ default: m.PaymentsPage })))
const RecoveryPage = lazy(() => import('@/pages/RecoveryPage').then((m) => ({ default: m.RecoveryPage })))
const SettingsPage = lazy(() => import('@/pages/SettingsPage').then((m) => ({ default: m.SettingsPage })))
const ComponentsPage = lazy(() => import('@/pages/dev/ComponentsPage').then((m) => ({ default: m.ComponentsPage })))
const DataPage = lazy(() => import('@/pages/dev/DataPage').then((m) => ({ default: m.DataPage })))
const TokensPage = lazy(() => import('@/pages/dev/TokensPage').then((m) => ({ default: m.TokensPage })))

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<Suspense fallback={<PageFallback />}><HomePage /></Suspense>} />
      <Route path="login" element={<Suspense fallback={<PageFallback />}><LoginPage /></Suspense>} />

      <Route element={<RequireAuth />}>
        <Route element={<AppShell />}>
          <Route path="dashboard" element={<OverviewPage />} />
          <Route path="payments" element={<PaymentsPage />} />
          <Route path="recovery" element={<RecoveryPage />} />
          <Route path="ai-cost" element={<AiCostPage />} />
          <Route path="ai-workspace" element={<AiWorkspacePage />} />
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
