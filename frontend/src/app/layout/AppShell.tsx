import { Outlet } from 'react-router-dom'

import { ErrorBoundary } from '@/components/ErrorBoundary'
import { AppFooter } from '@/app/layout/AppFooter'
import { Sidebar } from '@/app/layout/Sidebar'
import { Topbar } from '@/app/layout/Topbar'

export function AppShell() {
  return (
    <div className="min-h-screen bg-page">
      <Topbar />
      <Sidebar />
      <main className="ml-[var(--sidebar-width)] min-h-[calc(100vh-var(--topbar-height))] px-7 py-7">
        <ErrorBoundary>
          <Outlet />
        </ErrorBoundary>
      </main>
      <AppFooter />
    </div>
  )
}
