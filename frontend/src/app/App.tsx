import { BrowserRouter } from 'react-router'
import { ErrorBoundary } from './ErrorBoundary'
import { AppRoutes } from './router'

export function App() {
  return (
    <BrowserRouter>
      <ErrorBoundary>
        <AppRoutes />
      </ErrorBoundary>
    </BrowserRouter>
  )
}
