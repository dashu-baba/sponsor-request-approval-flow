import { Outlet } from 'react-router-dom'

import { AppFooter } from '@/app/layout/AppFooter'
import { Sidebar } from '@/app/layout/Sidebar'
import { ErrorBoundary } from '@/components/ErrorBoundary'

export function AppShell() {
  return (
    <div className="flex min-h-screen flex-col bg-page">
      <Sidebar />
      <main className="ml-[var(--sidebar-width)] flex-1 px-7 py-7">
        <ErrorBoundary>
          <Outlet />
        </ErrorBoundary>
      </main>
      <AppFooter />
    </div>
  )
}
