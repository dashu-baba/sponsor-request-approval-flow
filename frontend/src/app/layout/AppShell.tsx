import { Outlet } from 'react-router-dom'

import { AppFooter } from '@/app/layout/AppFooter'
import { Sidebar } from '@/app/layout/Sidebar'
import { ErrorBoundary } from '@/components/ErrorBoundary'

export function AppShell() {
  return (
    <div className="min-h-screen bg-page">
      <Sidebar />
      <main className="ml-[var(--sidebar-width)] min-h-screen px-7 py-7">
        <ErrorBoundary>
          <Outlet />
        </ErrorBoundary>
      </main>
      <AppFooter />
    </div>
  )
}
